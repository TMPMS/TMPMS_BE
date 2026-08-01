using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.Data;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repo;
        private readonly TMPMSDbContext _context;
        private readonly IInventoryService _inventoryService;

        public PrescriptionService(
            IPrescriptionRepository repo,
            TMPMSDbContext context,
            IInventoryService inventoryService)
        {
            _repo = repo;
            _context = context;
            _inventoryService = inventoryService;
        }

        public async Task<PrescriptionResponseDTO> Create(PrescriptionCreateDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                    UserId = dto.UserId,
                    DiagnosisId = dto.DiagnosisId,
                    DoctorId = dto.DoctorId,
                    DoctorName = dto.DoctorName,
                    Hospital = dto.Hospital,
                    PrescriptionDate = dto.PrescriptionDate == default ? DateTime.Now : dto.PrescriptionDate,
                    ImageUrl = dto.ImageUrl,
                    Status = "Approved",
                    PrescriptionItems = dto.Items.Select(i => new PrescriptionItem
                    {
                        MedicineId = i.MedicineId,
                        Quantity = i.Quantity
                    }).ToList()
                };

                _context.Prescriptions.Add(entity);
                await _context.SaveChangesAsync();

                // 3. Trừ kho tự động & ghi nhận giao dịch xuất kho qua InventoryService
                var defaultWarehouse = await _context.Warehouses.FirstOrDefaultAsync();
                int warehouseId = defaultWarehouse?.Id ?? 1;

                foreach (var item in dto.Items)
                {
                    var med = await _context.Medicines.FindAsync(item.MedicineId);
                    if (med != null)
                    {
                        med.StockQuantity -= item.Quantity;
                    }

                    await _inventoryService.CreateTransaction(new StockTransactionCreateDTO
                    {
                        MedicineId = item.MedicineId,
                        WarehouseId = warehouseId,
                        Type = "Export",
                        Quantity = item.Quantity,
                        ReferenceId = $"RX-{entity.Id}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var full = await _repo.GetById(entity.Id);
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

        private PrescriptionResponseDTO Map(Prescription p)
        {
            return new PrescriptionResponseDTO
            {
                Id = p.Id,
                UserId = p.UserId,
                UserName = p.User?.UserName,
                DiagnosisId = p.DiagnosisId,
                DoctorId = p.DoctorId,
                DoctorName = p.DoctorName ?? p.Doctor?.UserName,
                Hospital = p.Hospital,
                PrescriptionDate = p.PrescriptionDate,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                Items = p.PrescriptionItems?.Select(i => new PrescriptionItemResponseDTO
                {
                    Id = i.Id,
                    MedicineId = i.MedicineId,
                    MedicineName = i.Medicine?.Name,
                    Quantity = i.Quantity,
                    RequiresPrescription = i.Medicine?.RequiresPrescription ?? false
                }).ToList() ?? new List<PrescriptionItemResponseDTO>()
            };
        }

    }
}
