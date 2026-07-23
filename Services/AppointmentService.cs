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
            // kiểm tra bệnh nhân
            var user = await _appointmentRepository.GetUserById(userId);

            if (user == null || !user.IsActive)
                throw new Exception("User does not exist or has been disabled.");

            // kiểm tra nhân viên
            if (dto.StaffId != null)
            {
                var staff = await _appointmentRepository.GetStaffById(dto.StaffId.Value);

                if (staff == null || !staff.IsActive)
                    throw new Exception("Staff does not exist.");
            }

            // Không cho đặt quá khứ
            if (dto.AppointmentDate < DateTime.Now)
                throw new Exception("Appointment date cannot be in the past.");

            // Không cho đặt quá 7 ngày
            if (dto.AppointmentDate > DateTime.Now.AddDays(7))
                throw new Exception("Appointment can only be booked within 7 days.");

            // Kiểm tra trùng lịch
            if (dto.StaffId != null)
            {
                bool exist = await _appointmentRepository.IsAppointmentExist(
                    dto.StaffId.Value,
                    dto.AppointmentDate);

                if (exist)
                    throw new Exception("This time slot has already been booked.");
            }

            Appointment appointment = new Appointment
            {
                UserId = userId,
                StaffId = dto.StaffId,
                AppointmentDate = dto.AppointmentDate,
                Reason = dto.Reason,
                Note = dto.Note,

                Status = "Pending",

                CreatedAt = DateTime.Now
            };

            return await _appointmentRepository.Add(appointment);
        }

        public async Task<List<AppointmentDTO>> GetAppointments(int userId)
        {
            var appointments = await _appointmentRepository.GetByUserId(userId);

            return appointments.Select(a => new AppointmentDTO
            {
                Id = a.Id,
                PatientName = a.User.UserName,
                StaffName = a.Staff != null ? a.Staff.UserName : null,
                AppointmentDate = a.AppointmentDate,
                Reason = a.Reason,
                Status = a.Status
            }).ToList();
        }

        public async Task<bool> UpdateAppointment(int id,
                                           AppointmentUpdateDTO dto)
        {
            var appointment = await _appointmentRepository.GetById(id);

            if (appointment == null)
                throw new Exception("Appointment not found.");

            if (appointment.Status == "Completed")
                throw new Exception("Completed appointment cannot be updated.");

            if (appointment.Status == "Cancelled")
                throw new Exception("Cancelled appointment cannot be updated.");

            if (dto.AppointmentDate < DateTime.Now)
                throw new Exception("Appointment date must be greater than current date.");

            if (dto.AppointmentDate > DateTime.Now.AddDays(7))
                throw new Exception("Appointment can only be updated within 7 days.");

            bool exist = await _appointmentRepository.IsAppointmentExist(
                appointment.StaffId,
                dto.AppointmentDate,
                appointment.Id);

            if (exist)
                throw new Exception("The selected time slot has already been booked.");

            appointment.AppointmentDate = dto.AppointmentDate;
            appointment.Reason = dto.Reason;
            appointment.Note = dto.Note;

            return await _appointmentRepository.Update(appointment);
        }

        public async Task<bool> CancelAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);

            if (appointment == null)
                throw new Exception("Appointment not found.");

            // Không cho hủy nếu đã hoàn thành
            if (appointment.Status == "Completed")
                throw new Exception("Completed appointment cannot be cancelled.");

            // Không cho hủy nếu đã hủy
            if (appointment.Status == "Cancelled")
                throw new Exception("Appointment has already been cancelled.");

            // Không cho hủy nếu đã quá giờ hẹn
            if (appointment.AppointmentDate <= DateTime.Now)
                throw new Exception("Appointment cannot be cancelled because it has already started.");

            // Cập nhật trạng thái
            appointment.Status = "Cancelled";

            return await _appointmentRepository.Update(appointment);
        }

        public async Task<bool> ApproveAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);

            if (appointment == null)
                throw new Exception("Appointment not found.");

            // Không thể duyệt nếu đã hủy
            if (appointment.Status == "Cancelled")
                throw new Exception("Cancelled appointment cannot be approved.");

            // Không thể duyệt nếu đã hoàn thành
            if (appointment.Status == "Completed")
                throw new Exception("Completed appointment cannot be approved.");

            // Đã duyệt rồi
            if (appointment.Status == "Confirmed")
                throw new Exception("Appointment has already been approved.");

            // Không duyệt lịch đã qua
            if (appointment.AppointmentDate < DateTime.Now)
                throw new Exception("Past appointments cannot be approved.");

            appointment.Status = "Confirmed";

            return await _appointmentRepository.Update(appointment);
        }

        public async Task<bool> CompleteAppointment(int id)
        {
            var appointment = await _appointmentRepository.GetById(id);

            if (appointment == null)
                throw new Exception("Appointment not found.");

            // Chỉ được hoàn thành khi đã được xác nhận
            if (appointment.Status != "Confirmed")
                throw new Exception("Only confirmed appointments can be completed.");

            // Không thể hoàn thành nếu đã hủy
            if (appointment.Status == "Cancelled")
                throw new Exception("Cancelled appointment cannot be completed.");

            // Không thể hoàn thành nếu đã hoàn thành
            if (appointment.Status == "Completed")
                throw new Exception("Appointment has already been completed.");

            // Chỉ hoàn thành khi đã đến hoặc qua thời gian hẹn
            if (appointment.AppointmentDate > DateTime.Now)
                throw new Exception("Appointment has not started yet.");

            appointment.Status = "Completed";

            return await _appointmentRepository.Update(appointment);
        }
    }
}
