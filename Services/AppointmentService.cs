using BusinessObjects;
using TMPMS.DTOs;
using TMPMS.Models;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;

        public AppointmentService(IAppointmentRepository appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
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

            // Quy tắc nghiệp vụ: mỗi user chỉ được có tối đa 1 lịch hẹn đang hoạt động
            // (Status = PendingConfirmation | Confirmed và chưa quá hạn). Nếu có lịch đang chặn,
            // trả thông tin chi tiết lịch đó để FE hiển thị cụ thể.
            var active = await _appointmentRepository.GetActiveAppointmentByUserId(targetUserId);
            if (active != null)
            {
                return new AppointmentBookingResult
                {
                    Success = false,
                    BlockingAppointment = ToDTO(active)
                };
            }

            // Chống đặt trùng: không cho 2 lịch hẹn cùng bác sĩ tại cùng thời điểm (trừ lịch đã hủy/từ chối/hoàn thành)
            if (targetStaffId != null && await _appointmentRepository.IsAppointmentExist(targetStaffId.Value, appointmentDate))
                throw new Exception("Bác sĩ đã có lịch hẹn vào thời điểm này. Vui lòng chọn thời gian khác.");

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

            bool created = await _appointmentRepository.Add(appointment);
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

        public async Task<bool> CancelAppointment(int id, int currentUserId, bool isStaff)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            if (!isStaff && appointment.UserId != currentUserId)
                throw new Exception("Bạn không có quyền hủy lịch hẹn này.");
            appointment.Status = "Cancelled";
            return await _appointmentRepository.Update(appointment);
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
            return await _appointmentRepository.Update(appointment);
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
            return await _appointmentRepository.Update(appointment);
        }

        public async Task<bool> CompleteAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            appointment.Status = "Completed";
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
            };
        }
    }
}
