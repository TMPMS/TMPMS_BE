using BusinessObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Services.Interfaces;
using System.IO;
using System.Text.RegularExpressions;
using TMPMS.Data;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repo;
        private readonly TMPMSDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IPrescriptionOcrService _ocrService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        public PrescriptionService(
            IPrescriptionRepository repo,
            TMPMSDbContext context,
            IInventoryService inventoryService,
            IPrescriptionOcrService ocrService,
            IWebHostEnvironment environment,
            IEmailService emailService)
        {
            _repo = repo;
            _context = context;
            _inventoryService = inventoryService;
            _ocrService = ocrService;
            _environment = environment;
            _emailService = emailService;
        }

        // Gửi email "đã khám xong & kê đơn" tới bệnh nhân thực sự (Patient nếu người khác gửi hộ, không thì User).
        private async Task NotifyPrescriptionCreated(BusinessObjects.Prescription full)
        {
            var recipientEmail = full.Patient?.Email ?? full.User?.Email;
            if (string.IsNullOrWhiteSpace(recipientEmail)) return;

            var recipientName = full.Patient?.FullName ?? full.Patient?.UserName ?? full.User?.FullName ?? full.User?.UserName;
            var itemCount = full.PrescriptionItems?.Count ?? 0;

            await _emailService.SendEmailAsync(
                recipientEmail,
                "Kết quả khám và đơn thuốc - TMPMS",
                $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(recipientName)},</p>" +
                $"<p>Bạn đã hoàn tất buổi khám ngày <b>{full.PrescriptionDate:dd/MM/yyyy}</b>.</p>" +
                $"<p>Chẩn đoán: {System.Net.WebUtility.HtmlEncode(full.DiagnosisNote)}</p>" +
                $"<p>Bác sĩ {System.Net.WebUtility.HtmlEncode(full.DoctorName)} đã kê đơn gồm {itemCount} loại thuốc. Vui lòng đến nhà thuốc để nhận thuốc.</p>");
        }

        public async Task<PrescriptionResponseDTO> Create(PrescriptionCreateDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Giải quyết targetUserId an toàn từ dto.UserId hoặc dto.PatientId và kiểm tra tồn tại trong AspNetUsers
                int targetUserId = dto.UserId > 0 ? dto.UserId : (dto.PatientId ?? 0);
                if (targetUserId <= 0 || !await _context.Users.AnyAsync(u => u.Id == targetUserId))
                {
                    var defaultUser = await _context.Users.FirstOrDefaultAsync();
                    targetUserId = defaultUser?.Id ?? 1;
                }

                // 1. Kiểm tra tồn kho cho từng vị thuốc được kê
                foreach (var item in dto.Items)
                {
                    var med = await _context.Medicines.FindAsync(item.MedicineId);
                    if (med == null)
                    {
                        throw new InvalidOperationException($"Không tìm thấy vị thuốc/dược liệu có mã ID {item.MedicineId}.");
                    }
                    if (med.StockQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException($"Vị thuốc {med.Name} chỉ còn {med.StockQuantity}g trong kho, không đủ để kê {item.Quantity}g");
                    }
                }

                // 2. Tạo đơn thuốc
                var entity = new Prescription
                {
                    UserId = targetUserId,
                    DiagnosisId = dto.DiagnosisId,
                    DiagnosisNote = string.IsNullOrWhiteSpace(dto.DiagnosisNote) ? "Chẩn đoán Đông Y" : dto.DiagnosisNote,
                    DoctorId = dto.DoctorId,
                    DoctorName = string.IsNullOrWhiteSpace(dto.DoctorName) ? "Bác sĩ Đông Y" : dto.DoctorName,
                    Hospital = string.IsNullOrWhiteSpace(dto.Hospital) ? "Phòng khám Đông Y TMPMS" : dto.Hospital,
                    PrescriptionDate = dto.PrescriptionDate == default ? DateTime.Now : dto.PrescriptionDate,
                    ImageUrl = dto.ImageUrl ?? "",
                    AppointmentId = dto.AppointmentId,
                    PatientId = dto.PatientId,
                    Status = dto.Items != null && dto.Items.Any() ? "Approved" : "Pending",
                    PrescriptionItems = dto.Items.Select(i => new PrescriptionItem
                    {
                        MedicineId = i.MedicineId,
                        Quantity = i.Quantity,
                        Instructions = i.Instructions
                    }).ToList()
                };

                _context.Prescriptions.Add(entity);
                await _context.SaveChangesAsync();

                // 2.1 Đồng bộ trạng thái lịch hẹn: lịch hẹn liên kết được hoàn tất việc kê đơn
                if (dto.AppointmentId.HasValue)
                {
                    var appointment = await _context.Appointments.FindAsync(dto.AppointmentId.Value);
                    if (appointment != null && appointment.Status != "Cancelled")
                    {
                        appointment.Status = "Completed";
                        _context.Appointments.Update(appointment);
                    }
                }

                // 3. Trừ kho tự động & ghi nhận giao dịch xuất kho qua InventoryService
                var defaultWarehouse = await _context.Warehouses.FirstOrDefaultAsync();
                int warehouseId = defaultWarehouse?.Id ?? 1;

                foreach (var item in dto.Items)
                {
                    // Xuất kho theo FEFO: lô hết hạn sớm nhất được trừ trước
                    await _inventoryService.DeductStockFEFO(item.MedicineId, warehouseId, item.Quantity, $"RX-{entity.Id}");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var full = await _repo.GetById(entity.Id);
                await NotifyPrescriptionCreated(full);
                return Map(full);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PrescriptionResponseDTO> GetById(int id)
        {
            var entity = await _repo.GetById(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<List<PrescriptionResponseDTO>> GetByUser(int userId)
        {
            var list = await _repo.GetByUser(userId);
            return list.Select(Map).ToList();
        }

        public async Task<List<PrescriptionResponseDTO>> GetByStatus(string status)
        {
            var list = await _repo.GetByStatus(status);
            return list.Select(Map).ToList();
        }

        public async Task<List<PrescriptionResponseDTO>> GetAll()
        {
            var list = await _repo.GetAll();
            return list.Select(Map).ToList();
        }

        // Duyệt / Từ chối đơn thuốc. Chỉ những thuốc RequiresPrescription mới cần đơn hợp lệ để mua.
        public async Task<PrescriptionResponseDTO> UpdateStatus(int id, PrescriptionStatusUpdateDTO dto)
        {
            var entity = await _repo.GetById(id);
            if (entity == null) return null;

            var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Fulfilled" };
            if (!allowedStatuses.Contains(dto.Status))
                throw new ArgumentException("Trạng thái không hợp lệ.");

            entity.Status = dto.Status;
            var updated = await _repo.Update(entity);
            return Map(updated);
        }

        public async Task<bool> Delete(int id) => await _repo.Delete(id);

        // Đọc ảnh toa thuốc bằng AI (Gemini Vision) và gợi ý khớp với danh mục dược phẩm hiện có.
        // Chỉ trả về gợi ý — không tạo PrescriptionItem hay trừ kho, Dược sĩ phải xem lại rồi mới Finalize.
        public async Task<PrescriptionOcrResultDto> ScanImage(int id)
        {
            var entity = await _repo.GetById(id);
            if (entity == null) throw new InvalidOperationException("Không tìm thấy đơn thuốc.");
            if (string.IsNullOrWhiteSpace(entity.ImageUrl))
                throw new InvalidOperationException("Đơn thuốc này chưa có ảnh toa thuốc để quét.");

            var root = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var relativePath = entity.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(root, relativePath);
            if (!File.Exists(fullPath))
                throw new InvalidOperationException("Không tìm thấy tệp ảnh toa thuốc trên máy chủ.");

            var bytes = await File.ReadAllBytesAsync(fullPath);
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            var raw = await _ocrService.ExtractAsync(bytes, mimeType);
            var result = new PrescriptionOcrResultDto();
            if (raw == null)
            {
                result.IsAiGenerated = false;
                result.Disclaimer = "Không thể phân tích ảnh bằng AI vào lúc này. Vui lòng xem ảnh và nhập thuốc thủ công.";
                return result;
            }

            result.PatientName = raw.PatientName;
            result.DoctorName = raw.DoctorName;
            result.Hospital = raw.Hospital;
            result.Diagnosis = raw.Diagnosis;
            result.IsAiGenerated = true;

            var catalog = await _context.Medicines
                .Where(m => m.IsActive)
                .Select(m => new { m.Id, m.Name })
                .ToListAsync();

            foreach (var line in raw.MedicationLines)
            {
                var match = MatchMedicine(line, catalog.Select(m => (m.Id, m.Name)));
                result.Items.Add(new PrescriptionOcrItemDto
                {
                    RawText = line,
                    MatchedMedicineId = match?.Id,
                    MatchedMedicineName = match?.Name,
                    SuggestedQuantity = ExtractQuantity(line)
                });
            }

            if (result.Items.Count == 0)
                result.Disclaimer = "AI không đọc được danh sách thuốc trong ảnh. Vui lòng kiểm tra ảnh và nhập thuốc thủ công.";

            return result;
        }

        // Khớp gần đúng: chuẩn hóa chữ thường rồi tìm tên thuốc trong danh mục xuất hiện trong dòng
        // chữ AI đọc được. Ưu tiên tên khớp dài nhất để giảm khớp nhầm do trùng từ ngắn (vd "B1").
        private static (int Id, string Name)? MatchMedicine(string line, IEnumerable<(int Id, string Name)> catalog)
        {
            var normalizedLine = Normalize(line);
            var best = catalog
                .Select(m => new { m.Id, m.Name, Norm = Normalize(m.Name) })
                .Where(m => m.Norm.Length >= 3 && normalizedLine.Contains(m.Norm))
                .OrderByDescending(m => m.Norm.Length)
                .FirstOrDefault();
            return best == null ? null : (best.Id, best.Name);
        }

        private static string Normalize(string? s) => (s ?? "").ToLowerInvariant().Trim();

        // Toa thuốc Việt Nam thường ghi liều dùng trước ("sáng 1 gói - chiều 1 gói") rồi mới đến
        // tổng số lượng cấp phát ở cuối dòng ("- 10 gói") — lấy số cuối cùng thay vì số đầu tiên
        // để tránh nhầm liều dùng mỗi lần với tổng số lượng cần kê.
        private static int ExtractQuantity(string line)
        {
            var matches = Regex.Matches(line, @"\d+");
            if (matches.Count == 0) return 1;
            var last = matches[matches.Count - 1];
            if (int.TryParse(last.Value, out var qty) && qty > 0 && qty < 1000) return qty;
            return 1;
        }

        // Dược sĩ hoàn thiện (kê đơn) một đơn thuốc "Pending" gửi kèm ảnh: gắn danh sách thuốc cuối
        // cùng đã xác nhận, trừ kho theo FEFO và chuyển trạng thái sang Approved trong 1 giao dịch.
        public async Task<PrescriptionResponseDTO> Finalize(int id, PrescriptionFinalizeDTO dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Vui lòng thêm ít nhất một loại thuốc trước khi duyệt đơn.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.Prescriptions
                    .Include(p => p.PrescriptionItems)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (entity == null) throw new InvalidOperationException("Không tìm thấy đơn thuốc.");
                if (entity.PrescriptionItems != null && entity.PrescriptionItems.Any())
                    throw new InvalidOperationException("Đơn thuốc này đã được kê thuốc trước đó.");
                if (entity.Status != "Pending")
                    throw new InvalidOperationException("Chỉ có thể hoàn thiện đơn thuốc đang ở trạng thái Chờ duyệt.");

                foreach (var item in dto.Items)
                {
                    var med = await _context.Medicines.FindAsync(item.MedicineId);
                    if (med == null)
                        throw new InvalidOperationException($"Không tìm thấy vị thuốc/dược liệu có mã ID {item.MedicineId}.");
                    if (med.StockQuantity < item.Quantity)
                        throw new InvalidOperationException($"Vị thuốc {med.Name} chỉ còn {med.StockQuantity} trong kho, không đủ để kê {item.Quantity}.");
                }

                if (!string.IsNullOrWhiteSpace(dto.DoctorName)) entity.DoctorName = dto.DoctorName;
                if (!string.IsNullOrWhiteSpace(dto.Hospital)) entity.Hospital = dto.Hospital;
                if (!string.IsNullOrWhiteSpace(dto.DiagnosisNote)) entity.DiagnosisNote = dto.DiagnosisNote;
                if (dto.PatientId.HasValue) entity.PatientId = dto.PatientId;
                entity.Status = "Approved";
                entity.PrescriptionItems = dto.Items.Select(i => new PrescriptionItem
                {
                    MedicineId = i.MedicineId,
                    Quantity = i.Quantity,
                    Instructions = i.Instructions
                }).ToList();

                _context.Prescriptions.Update(entity);
                await _context.SaveChangesAsync();

                var defaultWarehouse = await _context.Warehouses.FirstOrDefaultAsync();
                int warehouseId = defaultWarehouse?.Id ?? 1;
                foreach (var item in dto.Items)
                {
                    await _inventoryService.DeductStockFEFO(item.MedicineId, warehouseId, item.Quantity, $"RX-{entity.Id}");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var full = await _repo.GetById(entity.Id);
                await NotifyPrescriptionCreated(full);
                return Map(full);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string DisplayName(User? u, int fallbackId) =>
            !string.IsNullOrEmpty(u?.FullName) ? u.FullName
            : (!string.IsNullOrEmpty(u?.UserName) ? u.UserName : $"Bệnh nhân #{fallbackId}");

        private PrescriptionResponseDTO Map(Prescription p)
        {
            string submitterDisplayName = DisplayName(p.User, p.UserId);
            int effectivePatientId = p.PatientId ?? p.UserId;
            bool isSubmittedForOther = p.PatientId.HasValue && p.PatientId.Value != p.UserId;
            string patientDisplayName = isSubmittedForOther ? DisplayName(p.Patient, effectivePatientId) : submitterDisplayName;

            return new PrescriptionResponseDTO
            {
                Id = p.Id,
                UserId = p.UserId,
                UserName = submitterDisplayName,
                PatientId = effectivePatientId,
                PatientName = patientDisplayName,
                IsSubmittedForOther = isSubmittedForOther,
                DiagnosisId = p.DiagnosisId,
                DiagnosisNote = p.DiagnosisNote ?? "Chẩn đoán Đông Y",
                DoctorId = p.DoctorId,
                DoctorName = p.DoctorName ?? p.Doctor?.UserName,
                Hospital = p.Hospital,
                AppointmentId = p.AppointmentId,
                PrescriptionDate = p.PrescriptionDate,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                Items = p.PrescriptionItems?.Select(i => new PrescriptionItemResponseDTO
                {
                    Id = i.Id,
                    MedicineId = i.MedicineId,
                    MedicineName = i.Medicine?.Name,
                    Quantity = i.Quantity,
                    RequiresPrescription = i.Medicine?.RequiresPrescription ?? false,
                    Instructions = i.Instructions
                }).ToList() ?? new List<PrescriptionItemResponseDTO>()
            };
        }

    }
}
