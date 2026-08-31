using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Data;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;
using TMPMS.Models;
using BusinessObjects;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;
using Services.Interfaces;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    // [Route("[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly TMPMSDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppointmentController> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailService _emailService;
        private const decimal DefaultDeposit = 100000m;

        public AppointmentController(IAppointmentService appointmentService, TMPMSDbContext context, IWebHostEnvironment environment, IConfiguration configuration, ILogger<AppointmentController> logger, IAuditLogService auditLogService, IEmailService emailService)
        {
            _appointmentService = appointmentService;
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
            _auditLogService = auditLogService;
            _emailService = emailService;
        }

        [HttpGet("availability")]
        [Authorize]
        public async Task<IActionResult> GetAvailability([FromQuery] DateTime date, [FromQuery] string? location)
        {
            var day = date.Date;
            if (day < DateTime.Today || day > DateTime.Today.AddDays(14)) return BadRequest(new { message = "Ngày khám phải nằm trong 14 ngày tới." });
            var place = string.IsNullOrWhiteSpace(location) ? "Nhà thuốc TMPMS" : location.Trim();
            var now = DateTime.Now;
            var occupied = await _context.Appointments
                .Where(a => a.Location == place && a.AppointmentDate >= day && a.AppointmentDate < day.AddDays(1)
                    && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed" }.Contains(a.Status))
                .Select(a => a.AppointmentDate).ToListAsync();
            var held = await _context.AppointmentSlotHolds
                .Where(h => h.Location == place && !h.IsConsumed && h.ExpiresAt > DateTime.UtcNow
                    && h.AppointmentDate >= day && h.AppointmentDate < day.AddDays(1))
                .Select(h => h.AppointmentDate).ToListAsync();
            var slots = new List<object>();
            for (var time = day.AddHours(8); time <= day.AddHours(17.5); time = time.AddMinutes(30))
                slots.Add(new { appointmentDate = time, available = time > now && !occupied.Contains(time) && !held.Contains(time) });
            return Ok(slots);
        }

        [HttpPost("hold")]
        [Authorize]
        public async Task<IActionResult> HoldSlot(SlotHoldRequestDTO dto)
        {
            var userId = GetCurrentUserId();
            if (dto.AppointmentDate <= DateTime.Now || dto.AppointmentDate > DateTime.Now.AddDays(14)) return BadRequest(new { message = "Khung giờ không hợp lệ." });
            if (dto.AppointmentDate.Minute is not (0 or 30) || dto.AppointmentDate.Hour < 8 || dto.AppointmentDate.TimeOfDay > TimeSpan.FromHours(17.5))
                return BadRequest(new { message = "Khung giờ nằm ngoài giờ làm việc." });
            var activeCount = await _context.Appointments.CountAsync(a => a.UserId == userId && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed", "RescheduleRequested" }.Contains(a.Status));
            if (activeCount >= 3) return Conflict(new { message = "Bạn đã có đủ 3 lịch đang hoạt động. Vui lòng hoàn thành hoặc hủy một lịch trước." });

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var place = string.IsNullOrWhiteSpace(dto.Location) ? "Nhà thuốc TMPMS" : dto.Location.Trim();
            await _context.AppointmentSlotHolds.Where(h => !h.IsConsumed && h.ExpiresAt <= DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.IsConsumed, true));
            // Giải phóng các khung giờ khách đã giữ trước đó nhưng chưa thanh toán/checkout — nếu
            // không, khi khách đổi ý chọn giờ khác, khung giờ cũ vẫn bị coi là "đang giữ" tới hết 15
            // phút dù không còn ai chọn nó nữa, khiến khách quay lại không chọn được giờ cũ đó. Chỉ
            // giải phóng hold chưa gắn AppointmentPaymentIntent nào — hold đã vào luồng thanh toán thì
            // giữ nguyên để không phá vé đang chờ thanh toán ở tab/phiên khác.
            await _context.AppointmentSlotHolds
                .Where(h => h.UserId == userId && !h.IsConsumed && h.ExpiresAt > DateTime.UtcNow
                    && !_context.AppointmentPaymentIntents.Any(pi => pi.SlotHoldId == h.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.IsConsumed, true));
            var unavailable = await _context.Appointments.AnyAsync(a => a.Location == place && a.AppointmentDate == dto.AppointmentDate
                && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed" }.Contains(a.Status))
                || await _context.AppointmentSlotHolds.AnyAsync(h => h.Location == place && h.AppointmentDate == dto.AppointmentDate && !h.IsConsumed && h.ExpiresAt > DateTime.UtcNow);
            if (unavailable) return Conflict(new { message = "Khung giờ vừa được người khác chọn. Vui lòng chọn giờ khác." });
            var hold = new AppointmentSlotHold { UserId = userId, AppointmentDate = dto.AppointmentDate, Location = place, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(15) };
            _context.AppointmentSlotHolds.Add(hold);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { hold.Token, hold.ExpiresAt, depositAmount = DefaultDeposit });
        }

        [HttpPost("upload-prescription")]
        [Authorize]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadPrescription(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "Vui lòng chọn ảnh." });
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest(new { message = "Chỉ hỗ trợ JPG, PNG hoặc WEBP." });
            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();
                // Kiểm tra nội dung file thật (magic bytes) — trước đây chỉ tin đuôi file client tự khai.
                if (!TMPMS.Utils.ImageMagicBytes.LooksLikeImage(bytes))
                    return BadRequest(new { message = "Tệp không phải ảnh hợp lệ." });

                var root = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var folder = Path.Combine(root, "uploads", "appointments");
                Directory.CreateDirectory(folder);
                var name = $"{Guid.NewGuid():N}{ext}";
                await System.IO.File.WriteAllBytesAsync(Path.Combine(folder, name), bytes);
                return Ok(new { url = $"/api/uploads/appointments/{name}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể lưu ảnh đơn thuốc cho lịch hẹn.");
                return StatusCode(500, new { message = "Không thể lưu ảnh, vui lòng thử lại." });
            }
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout(AppointmentCheckoutDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SymptomDescription)) return BadRequest(new { message = "Mô tả triệu chứng không được để trống." });
            if (!dto.PolicyAccepted) return BadRequest(new { message = "Bạn cần đồng ý chính sách đặt lịch." });
            if (!Uri.TryCreate(dto.ReturnUrl, UriKind.Absolute, out _) || !Uri.TryCreate(dto.CancelUrl, UriKind.Absolute, out _))
                return BadRequest(new { message = "ReturnUrl hoặc CancelUrl không hợp lệ." });
            var userId = GetCurrentUserId();
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var hold = await _context.AppointmentSlotHolds.FirstOrDefaultAsync(h => h.Token == dto.HoldToken && h.UserId == userId && !h.IsConsumed);
            if (hold == null || hold.ExpiresAt <= DateTime.UtcNow) return Conflict(new { message = "Thời gian giữ chỗ đã hết. Vui lòng chọn lại khung giờ." });
            var activeCount = await _context.Appointments.CountAsync(a => a.UserId == userId && new[] { "PendingConfirmation", "Confirmed", "CheckedIn", "AlternativeProposed", "RescheduleRequested" }.Contains(a.Status));
            if (activeCount >= 3) return Conflict(new { message = "Bạn đã có đủ 3 lịch đang hoạt động." });
            var intent = new AppointmentPaymentIntent { UserId = userId, SlotHoldId = hold.Id, SymptomDescription = dto.SymptomDescription.Trim(), PrescriptionImageUrl = dto.PrescriptionImageUrl, Note = dto.Note, Amount = DefaultDeposit, CreatedAt = DateTime.UtcNow, ExpiresAt = hold.ExpiresAt };
            _context.AppointmentPaymentIntents.Add(intent);
            await _context.SaveChangesAsync();
            intent.OrderCode = 800000000000L + intent.Id;
            await _context.SaveChangesAsync();
            var clientId = _configuration["PayOS:ClientId"]; var apiKey = _configuration["PayOS:ApiKey"]; var checksumKey = _configuration["PayOS:ChecksumKey"];
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(checksumKey))
                return BadRequest(new { message = "PayOS chưa được cấu hình." });
            try
            {
                var client = new PayOSClient(clientId, apiKey, checksumKey);
                var request = new CreatePaymentLinkRequest { OrderCode = intent.OrderCode, Amount = decimal.ToInt32(DefaultDeposit), Description = $"COC LICH {intent.Id}", ReturnUrl = dto.ReturnUrl, CancelUrl = dto.CancelUrl };
                var link = await client.PaymentRequests.CreateAsync(request);
                intent.PaymentLinkId = link.PaymentLinkId; await _context.SaveChangesAsync(); await tx.CommitAsync();
                return Ok(new { checkoutUrl = link.CheckoutUrl, paymentLinkId = link.PaymentLinkId, orderCode = intent.OrderCode, expiresAt = hold.ExpiresAt });
            }
            catch (Exception ex) { return BadRequest(new { message = $"Không thể tạo thanh toán PayOS: {ex.Message}" }); }
        }

        [HttpPost("book")]
        [Authorize]
        public async Task<IActionResult> BookAppointment(AppointmentCreateDTO dto)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (!CanProxy()) dto.PatientId = null;
                var result = await _appointmentService.BookAppointment(userId, dto);

                if (result.BlockingAppointment != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn đang có lịch hẹn chưa xử lý xong. Vui lòng chờ xác nhận, quá hạn hoặc hủy lịch hẹn này trước khi đặt lịch mới.",
                        blockingAppointment = result.BlockingAppointment
                    });
                }

                return Ok(new { success = result.Success, message = "Book appointment successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("my-appointments")]
        [Authorize]
        public async Task<IActionResult> GetAppointments()
        {
            int userId = GetCurrentUserId();
            var appointments = await _appointmentService.GetAppointments(userId);
            return Ok(appointments);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> GetAllAppointments()
        {
            var appointments = await _appointmentService.GetAllAppointments();
            return Ok(appointments);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> UpdateAppointment(int id, AppointmentUpdateDTO dto)
        {
            try
            {
                bool result = await _appointmentService.UpdateAppointment(id, dto);
                if (!result) return BadRequest(new { success = false, message = "Update appointment failed." });
                await this.LogAuditAsync(_auditLogService, "Appointment", "Update", id.ToString(), $"Cập nhật lịch hẹn #{id}");
                return Ok(new { success = true, message = "Appointment updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.DeleteAppointment(id);
                if (!result) return NotFound(new { success = false, message = "Appointment not found." });
                await this.LogAuditAsync(_auditLogService, "Appointment", "Delete", id.ToString(), $"Xóa lịch hẹn #{id}");
                return Ok(new { success = true, message = "Appointment deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}/cancellation-quote")]
        [Authorize]
        public async Task<IActionResult> GetCancellationQuote(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();
            if (!CanProxy() && appt.UserId != GetCurrentUserId()) return Forbid();
            if (!new[] { "PendingConfirmation", "Confirmed", "AlternativeProposed", "RescheduleRequested" }.Contains(appt.Status))
                return BadRequest(new { message = "Lịch này không thể hủy." });
            var hours = (appt.AppointmentDate - DateTime.Now).TotalHours;
            var percent = hours >= 24 ? 100 : hours > 0 ? 50 : 0;
            return Ok(new { refundPercentage = percent, refundAmount = appt.DepositAmount * percent / 100m });
        }

        [HttpPost("{id}/cancel-with-refund")]
        [Authorize]
        public async Task<IActionResult> CancelWithRefund(int id)
        {
            var appt = await _context.Appointments.Include(a => a.AppointmentPayments).Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null) return NotFound();
            if (!CanProxy() && appt.UserId != GetCurrentUserId()) return Forbid();
            if (!new[] { "PendingConfirmation", "Confirmed", "AlternativeProposed", "RescheduleRequested" }.Contains(appt.Status))
                return BadRequest(new { message = "Lịch này không thể hủy." });
            var percent = CanProxy() || (appt.AppointmentDate - DateTime.Now).TotalHours >= 24 ? 100 : (appt.AppointmentDate > DateTime.Now ? 50 : 0);
            appt.Status = "Cancelled";
            appt.CancelledAt = DateTime.UtcNow;
            appt.RefundAmount = appt.DepositAmount * percent / 100m;
            var payment = appt.AppointmentPayments.OrderByDescending(p => p.Id).FirstOrDefault();
            if (payment != null) { payment.RefundAmount = appt.RefundAmount; payment.RefundStatus = appt.RefundAmount > 0 ? "ContactCustomerForManualRefund" : "NonRefundable"; }
            await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Appointment", "CancelWithRefund", id.ToString(), $"Hủy lịch hẹn #{id} — hoàn {percent}% cọc ({appt.RefundAmount:N0}đ)");
            if (!string.IsNullOrWhiteSpace(appt.User?.Email))
            {
                await _emailService.SendEmailAsync(
                    appt.User.Email,
                    "Lịch hẹn đã được hủy - TMPMS",
                    $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(appt.User.FullName ?? appt.User.UserName)},</p>" +
                    $"<p>Lịch hẹn khám vào lúc <b>{appt.AppointmentDate:HH:mm dd/MM/yyyy}</b> của bạn đã được <b>hủy</b> thành công.</p>" +
                    (appt.RefundAmount > 0
                        ? $"<p>Số tiền cọc được hoàn: <b>{appt.RefundAmount:N0}đ</b> ({percent}%). Chúng tôi sẽ liên hệ để hoàn tiền.</p>"
                        : "<p>Lịch hẹn này không được hoàn cọc theo chính sách hủy lịch.</p>") +
                    "<p>Nếu đây không phải yêu cầu của bạn, vui lòng liên hệ nhà thuốc để được hỗ trợ.</p>");
            }
            return Ok(new { success = true, refundPercentage = percent, refundAmount = appt.RefundAmount, refundStatus = payment?.RefundStatus });
        }

        [HttpPost("{id}/reschedule")]
        [Authorize]
        public async Task<IActionResult> RequestReschedule(int id, AppointmentRescheduleCreateDTO dto)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();
            if (appt.UserId != GetCurrentUserId()) return Forbid();
            if (!new[] { "PendingConfirmation", "Confirmed" }.Contains(appt.Status)) return BadRequest(new { message = "Lịch này không thể đổi giờ." });
            if (dto.AppointmentDate <= DateTime.Now || dto.AppointmentDate > DateTime.Now.AddDays(14)) return BadRequest(new { message = "Khung giờ mới không hợp lệ." });
            var occupied = await _context.Appointments.AnyAsync(a => a.Id != id && a.Location == appt.Location && a.AppointmentDate == dto.AppointmentDate && new[] { "PendingConfirmation", "Confirmed", "CheckedIn" }.Contains(a.Status));
            if (occupied) return Conflict(new { message = "Khung giờ mới đã được đặt." });
            if (appt.Status == "PendingConfirmation")
            {
                appt.AppointmentDate = dto.AppointmentDate;
                appt.ConfirmationDeadline = DateTime.UtcNow.AddHours(24);
            }
            else
            {
                _context.AppointmentRescheduleRequests.Add(new AppointmentRescheduleRequest { AppointmentId = id, OldAppointmentDate = appt.AppointmentDate, RequestedAppointmentDate = dto.AppointmentDate, Reason = dto.Reason, CreatedAt = DateTime.UtcNow });
                appt.Status = "RescheduleRequested";
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true, status = appt.Status });
        }

        [HttpPut("{id}/check-in")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> CheckIn(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();
            if (appt.Status != "Confirmed") return BadRequest(new { message = "Chỉ lịch đã xác nhận mới có thể check-in." });
            appt.Status = "CheckedIn"; appt.CheckedInAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Appointment", "CheckIn", id.ToString(), $"Check-in lịch hẹn #{id}");
            return Ok(new { success = true });
        }

        [HttpPost("{id}/propose-time")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> ProposeTime(int id, AppointmentProposalDTO dto)
        {
            var appt = await _context.Appointments.FindAsync(id); if (appt == null) return NotFound();
            if (appt.Status != "PendingConfirmation" || dto.AppointmentDate <= DateTime.Now) return BadRequest(new { message = "Không thể đề xuất giờ cho lịch này." });
            appt.ProposedAppointmentDateNote = $"{dto.AppointmentDate:O}|{dto.Note}"; appt.Status = "AlternativeProposed";
            await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Appointment", "ProposeTime", id.ToString(), $"Đề xuất giờ khám mới cho lịch hẹn #{id}: {dto.AppointmentDate:O}");
            return Ok(new { success = true });
        }

        [HttpPost("{id}/respond-proposal")]
        [Authorize]
        public async Task<IActionResult> RespondProposal(int id, AppointmentDecisionDTO dto)
        {
            var appt = await _context.Appointments.FindAsync(id); if (appt == null) return NotFound();
            if (appt.UserId != GetCurrentUserId()) return Forbid();
            if (appt.Status != "AlternativeProposed" || string.IsNullOrWhiteSpace(appt.ProposedAppointmentDateNote)) return BadRequest(new { message = "Không có đề xuất giờ đang chờ." });
            if (dto.Accept && DateTime.TryParse(appt.ProposedAppointmentDateNote.Split('|')[0], out var proposed)) { appt.AppointmentDate = proposed; appt.Status = "Confirmed"; appt.ConfirmedAt = DateTime.UtcNow; }
            else appt.Status = "PendingConfirmation";
            appt.ProposedAppointmentDateNote = null; await _context.SaveChangesAsync(); return Ok(new { success = true, appt.Status });
        }

        [HttpPost("reschedule-requests/{requestId}/decision")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> ResolveReschedule(int requestId, AppointmentDecisionDTO dto)
        {
            var request = await _context.AppointmentRescheduleRequests.Include(r => r.Appointment).FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending");
            if (request == null) return NotFound();
            request.Status = dto.Accept ? "Approved" : "Rejected"; request.ResolvedAt = DateTime.UtcNow; request.ResolvedByStaffId = GetCurrentUserId();
            if (dto.Accept) request.Appointment.AppointmentDate = request.RequestedAppointmentDate;
            request.Appointment.Status = "Confirmed"; await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Appointment", "ResolveReschedule", request.AppointmentId.ToString(), $"{(dto.Accept ? "Duyệt" : "Từ chối")} yêu cầu đổi lịch #{requestId} cho lịch hẹn #{request.AppointmentId}");
            return Ok(new { success = true });
        }

        [HttpPut("{id}/no-show")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> MarkNoShow(int id)
        {
            var appt = await _context.Appointments.Include(a => a.AppointmentPayments).FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null) return NotFound();
            if (appt.Status != "Confirmed" || DateTime.Now < appt.AppointmentDate.AddMinutes(15)) return BadRequest(new { message = "Chỉ đánh dấu vắng sau giờ hẹn 15 phút." });
            appt.Status = "NoShow"; appt.RefundAmount = 0;
            var payment = appt.AppointmentPayments.FirstOrDefault(); if (payment != null) payment.RefundStatus = "NonRefundable";
            await _context.SaveChangesAsync();
            await this.LogAuditAsync(_auditLogService, "Appointment", "NoShow", id.ToString(), $"Đánh dấu vắng mặt lịch hẹn #{id} — không hoàn cọc");
            return Ok(new { success = true });
        }

        [HttpPut("approve/{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.ApproveAppointment(id, GetCurrentUserId());
                await this.LogAuditAsync(_auditLogService, "Appointment", "Approve", id.ToString(), $"Duyệt lịch hẹn #{id}");
                return Ok(new { Success = result, Message = "Appointment approved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("reject/{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> RejectAppointment(int id, AppointmentRejectDTO dto)
        {
            try
            {
                bool result = await _appointmentService.RejectAppointment(id, GetCurrentUserId(), dto?.Reason);
                var rejected = await _context.Appointments.Include(a => a.AppointmentPayments).FirstOrDefaultAsync(a => a.Id == id);
                if (rejected != null)
                {
                    rejected.RefundAmount = rejected.DepositAmount;
                    var payment = rejected.AppointmentPayments.OrderByDescending(p => p.Id).FirstOrDefault();
                    if (payment != null) { payment.RefundAmount = rejected.DepositAmount; payment.RefundStatus = "ContactCustomerForManualRefund"; }
                    await _context.SaveChangesAsync();
                }
                await this.LogAuditAsync(_auditLogService, "Appointment", "Reject", id.ToString(), $"Từ chối lịch hẹn #{id} — lý do: {dto?.Reason}");
                return Ok(new { Success = result, Message = "Appointment rejected successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("complete/{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            try
            {
                bool result = await _appointmentService.CompleteAppointment(id);
                await this.LogAuditAsync(_auditLogService, "Appointment", "Complete", id.ToString(), $"Hoàn thành lịch hẹn #{id}");
                return Ok(new { Success = result, Message = "Appointment completed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(claim, out int userId);
            return userId;
        }

        private bool CanProxy()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Pharmacy");
        }
    }
}
