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

        public async Task<bool> BookAppointment(int userId, AppointmentCreateDTO dto)
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

            if (appointmentDate < DateTime.Now.AddDays(-1))
                throw new Exception("Appointment date cannot be in the past.");

            // Chống đặt trùng: không cho 2 lịch hẹn cùng bác sĩ tại cùng thời điểm (trừ lịch đã hủy)
            if (targetStaffId != null && await _appointmentRepository.IsAppointmentExist(targetStaffId.Value, appointmentDate))
                throw new Exception("Bác sĩ đã có lịch hẹn vào thời điểm này. Vui lòng chọn thời gian khác.");

            // Chống spam: mỗi người chỉ được có 1 lịch hẹn đang chờ/đã xác nhận trong vòng 30 phút qua
            var recentWindow = DateTime.UtcNow.AddMinutes(-30);
            if (await _appointmentRepository.HasRecentActiveAppointment(targetUserId, recentWindow))
                throw new Exception("Bạn vừa đặt lịch hẹn gần đây. Vui lòng chờ xác nhận lịch hẹn hiện tại trước khi đặt lịch mới.");

            Appointment appointment = new Appointment
            {
                UserId = targetUserId,
                StaffId = targetStaffId,
                AppointmentDate = appointmentDate,
                Reason = dto.Reason ?? "",
                Note = dto.Note ?? dto.Notes,
                Status = string.IsNullOrEmpty(dto.Status) ? "Pending" : (dto.Status == "Scheduled" ? "Pending" : dto.Status),
                CreatedAt = DateTime.UtcNow
            };

            return await _appointmentRepository.Add(appointment);
        }

        public async Task<List<AppointmentDTO>> GetAppointments(int userId)
        {
            var appointments = await _appointmentRepository.GetByUserId(userId);
            return appointments.Select(a => new AppointmentDTO
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
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<List<AppointmentDTO>> GetAllAppointments()
        {
            var appointments = await _appointmentRepository.GetAll();
            return appointments.Select(a => new AppointmentDTO
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
                CreatedAt = a.CreatedAt
            }).ToList();
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

        public async Task<bool> CancelAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            appointment.Status = "Cancelled";
            return await _appointmentRepository.Update(appointment);
        }

        public async Task<bool> ApproveAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            appointment.Status = "Confirmed";
            return await _appointmentRepository.Update(appointment);
        }

        public async Task<bool> CompleteAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);
            if (appointment == null) throw new Exception("Appointment not found.");
            appointment.Status = "Completed";
            return await _appointmentRepository.Update(appointment);
        }
    }
}
