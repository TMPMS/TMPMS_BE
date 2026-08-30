using BusinessObjects;
using Services.Interfaces;
using TMPMS.DTOs;
using TMPMS.Models;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IEmailService _emailService;

        public AppointmentService(IAppointmentRepository appointmentRepository, IEmailService emailService)
        {
            _appointmentRepository = appointmentRepository;
            _emailService = emailService;
        }

        public async Task ExpireOverdueAppointmentsAsync()
        {
            // AppointmentDate lưu theo giờ địa phương (wall-clock) => so với DateTime.Now.
            // ConfirmationDeadline/CreatedAt lưu theo UTC => so với DateTime.UtcNow.
            await _appointmentRepository.ExpireOverdueAppointmentsAsync(DateTime.Now, DateTime.UtcNow);
        }

        public async Task<AppointmentBookingResult> BookAppointment(int userId, AppointmentCreateDTO dto)
        {
            int targetUserId = (dto.PatientId.HasValue && dto.PatientId.Value > 0) ? dto.PatientId.Value : userId;
            if (targetUserId <= 0) targetUserId = 1;

            var user = await _appointmentRepository.GetUserById(targetUserId);
            if (user == null || !user.IsActive)
                throw new Exception("User does not exist or has been disabled.");

            int? targetStaffId = dto.StaffId ?? dto.DoctorId;
            if (targetStaffId != null && targetStaffId > 0)
            {
                var staff = await _appointmentRepository.GetStaffById(targetStaffId.Value);
                if (staff == null || !staff.IsActive)
                    targetStaffId = null; // fallback
            }

            // Chuẩn hoá múi giờ: toàn hệ thống lưu theo giờ địa phương (wall-clock), không chênh lệch UTC
            DateTime appointmentDate = dto.AppointmentDate == default ? DateTime.Now.AddDays(1) : dto.AppointmentDate;
            if (appointmentDate.Kind == DateTimeKind.Utc)
                appointmentDate = appointmentDate.ToLocalTime();
            appointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Local);

            if (appointmentDate < DateTime.Now)
                throw new Exception("Thời gian hẹn khám phải ở tương lai. Vui lòng chọn khung giờ chưa trôi qua.");
            if (appointmentDate > DateTime.Now.AddDays(14))
                throw new Exception("Chỉ được đặt lịch hẹn trước tối đa 14 ngày.");

            // Làm mới trạng thái trước khi kiểm tra rule "1 lịch hoạt động":
            // các lịch quá hạn (hết hạn chờ xác nhận 24h hoặc đã qua giờ hẹn) sẽ bị Expired.
            await ExpireOverdueAppointmentsAsync();

            // Quy tắc nghiệp vụ: mỗi user chỉ được có tối đa 3 lịch hẹn đang hoạt động
            // (Status = PendingConfirmation | Confirmed và chưa quá hạn). Nếu có lịch đang chặn,
            // trả thông tin chi tiết lịch đó để FE hiển thị cụ thể.
            var activeAppointments = (await _appointmentRepository.GetByUserId(targetUserId))
                .Where(a => a.Status is "PendingConfirmation" or "Pending" or "Confirmed" or "CheckedIn" or "AlternativeProposed" or "RescheduleRequested")
                .ToList();
            if (activeAppointments.Count >= 3)
            {
                return new AppointmentBookingResult
                {
                    Success = false,
                    BlockingAppointment = ToDTO(activeAppointments.OrderBy(a => a.AppointmentDate).First())
                };
            }

            Appointment appointment = new Appointment
            {
                UserId = targetUserId,
                StaffId = targetStaffId,
                AppointmentDate = appointmentDate,
                Reason = dto.Reason ?? "",
                Note = dto.Note ?? dto.Notes,
                // Lịch mới luôn ở trạng thái chờ Pharmacy xác nhận (thủ công, không auto-confirm)
                Status = "PendingConfirmation",
                CreatedAt = DateTime.UtcNow,
                ConfirmationDeadline = DateTime.UtcNow.AddHours(24)
            };

            // Kiểm tra "bác sĩ đã có lịch chưa" + thêm lịch mới nguyên tử trong 1 transaction — tách
            // riêng 2 bước (check rồi insert) trước đây tạo race condition: 2 request đặt cùng bác sĩ/
            // thời điểm chạy đồng thời có thể cả hai đều thấy "chưa có lịch" rồi cùng đặt thành công.
            bool created = await _appointmentRepository.TryAddIfSlotFreeAsync(appointment);
            if (!created)
                throw new Exception("Bác sĩ đã có lịch hẹn vào thời điểm này. Vui lòng chọn thời gian khác.");

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Đặt lịch khám thành công - TMPMS",
                    $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(user.FullName ?? user.UserName)},</p>" +
                    $"<p>Bạn đã đặt lịch khám vào lúc <b>{appointment.AppointmentDate:HH:mm dd/MM/yyyy}</b> tại {System.Net.WebUtility.HtmlEncode(appointment.Location)}.</p>" +
                    "<p>Lịch hẹn đang chờ nhà thuốc xác nhận. Chúng tôi sẽ gửi email ngay khi lịch được xác nhận.</p>");
            }
            return new AppointmentBookingResult { Success = created };
        }

        public async Task<List<AppointmentDTO>> GetAppointments(int userId)
        {
            await ExpireOverdueAppointmentsAsync();
            var appointments = await _appointmentRepository.GetByUserId(userId);
            return appointments.Select(ToDTO).ToList();
        }

        public async Task<List<AppointmentDTO>> GetAllAppointments()
        {
            await ExpireOverdueAppointmentsAsync();
            var appointments = await _appointmentRepository.GetAll();
            return appointments.Select(ToDTO).ToList();
        }

        public async Task<bool> UpdateAppointment(int id, AppointmentUpdateDTO dto)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null)
                throw new Exception("Appointment not found.");

            DateTime appointmentDate = dto.AppointmentDate == default
                ? appointment.AppointmentDate
                : dto.AppointmentDate;
            if (appointmentDate.Kind == DateTimeKind.Utc)
                appointmentDate = appointmentDate.ToLocalTime();
            appointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Local);
            appointment.AppointmentDate = appointmentDate;
            if (!string.IsNullOrEmpty(dto.Reason)) appointment.Reason = dto.Reason;
            if (dto.Note != null) appointment.Note = dto.Note;

            // Chống trùng lịch khi sửa: kiểm tra bác sĩ/thời gian mới có bị trùng lịch hẹn khác không
            if (appointment.StaffId != null && await _appointmentRepository.IsAppointmentExist(appointment.StaffId, appointment.AppointmentDate, id))
                throw new Exception("Bác sĩ đã có lịch hẹn vào thời điểm này. Vui lòng chọn thời gian khác.");

            return await _appointmentRepository.Update(appointment);
        }

        public async Task<bool> DeleteAppointment(int id)
        {
            return await _appointmentRepository.Delete(id);
        }

        public async Task<bool> ApproveAppointment(int id, int staffId)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            if (appointment.Status != "PendingConfirmation")
                throw new Exception("Chỉ lịch hẹn đang chờ xác nhận mới được xác nhận.");
            appointment.Status = "Confirmed";
            appointment.ConfirmedAt = DateTime.UtcNow;
            appointment.ConfirmedByStaffId = staffId;
            appointment.RejectionReason = null;
            var updated = await _appointmentRepository.Update(appointment);
            if (updated && !string.IsNullOrWhiteSpace(appointment.User?.Email))
            {
                await _emailService.SendEmailAsync(
                    appointment.User.Email,
                    "Lịch hẹn đã được xác nhận - TMPMS",
                    $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(appointment.User.FullName ?? appointment.User.UserName)},</p>" +
                    $"<p>Lịch hẹn khám vào lúc <b>{appointment.AppointmentDate:HH:mm dd/MM/yyyy}</b> của bạn đã được nhà thuốc <b>xác nhận</b>.</p>" +
                    $"<p>Vui lòng đến đúng giờ tại {System.Net.WebUtility.HtmlEncode(appointment.Location)}.</p>");
            }
            return updated;
        }

        public async Task<bool> RejectAppointment(int id, int staffId, string reason)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            if (appointment.Status != "PendingConfirmation")
                throw new Exception("Chỉ lịch hẹn đang chờ xác nhận mới được từ chối.");
            appointment.Status = "Rejected";
            appointment.ConfirmedAt = null;
            appointment.ConfirmedByStaffId = staffId;
            appointment.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Không cung cấp lý do" : reason.Trim();
            var updated = await _appointmentRepository.Update(appointment);
            if (updated && !string.IsNullOrWhiteSpace(appointment.User?.Email))
            {
                await _emailService.SendEmailAsync(
                    appointment.User.Email,
                    "Lịch hẹn không được xác nhận - TMPMS",
                    $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(appointment.User.FullName ?? appointment.User.UserName)},</p>" +
                    $"<p>Rất tiếc, lịch hẹn khám vào lúc <b>{appointment.AppointmentDate:HH:mm dd/MM/yyyy}</b> của bạn <b>không được xác nhận</b>.</p>" +
                    $"<p>Lý do: {System.Net.WebUtility.HtmlEncode(appointment.RejectionReason)}</p>" +
                    "<p>Vui lòng đặt lại lịch hẹn khác hoặc liên hệ nhà thuốc để được hỗ trợ.</p>");
            }
            return updated;
        }

        public async Task<bool> CompleteAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            if (appointment.Status != "CheckedIn") throw new Exception("Lịch hẹn phải được check-in trước khi hoàn thành.");
            appointment.Status = "Completed";
            appointment.CompletedAt = DateTime.UtcNow;
            return await _appointmentRepository.Update(appointment);
        }

        private static AppointmentDTO ToDTO(Appointment a)
        {
            return new AppointmentDTO
            {
                Id = a.Id,
                PatientId = a.UserId,
                PatientName = a.User?.FullName ?? a.User?.UserName ?? "Bệnh nhân",
                PatientPhone = a.User?.PhoneNumber,
                DoctorId = a.StaffId,
                DoctorName = a.Staff != null ? (a.Staff.FullName ?? a.Staff.UserName) : "Bác sĩ phụ trách",
                AppointmentDate = a.AppointmentDate,
                Reason = a.Reason,
                Status = a.Status == "Pending" ? "Scheduled" : a.Status,
                Notes = a.Note,
                CreatedAt = a.CreatedAt,
                ConfirmationDeadline = a.ConfirmationDeadline,
                ConfirmedAt = a.ConfirmedAt,
                RejectionReason = a.RejectionReason
                ,SymptomDescription = a.SymptomDescription
                ,PrescriptionImageUrl = a.PrescriptionImageUrl
                ,Location = a.Location
                ,DepositAmount = a.DepositAmount
                ,PaymentStatus = a.PaymentStatus
                ,PaymentMethod = a.PaymentMethod
                ,RefundAmount = a.RefundAmount
                ,CheckedInAt = a.CheckedInAt
            };
        }
    }
}
