using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentBookingResult> BookAppointment(int userId, AppointmentCreateDTO dto);
        Task<List<AppointmentDTO>> GetAppointments(int userId);
        Task<List<AppointmentDTO>> GetAllAppointments();
        Task<bool> UpdateAppointment(int id, AppointmentUpdateDTO dto);
        Task<bool> DeleteAppointment(int id);
        Task<bool> CancelAppointment(int id, int currentUserId, bool isStaff);
        Task<bool> ApproveAppointment(int id, int staffId);
        Task<bool> RejectAppointment(int id, int staffId, string reason);
        Task<bool> CompleteAppointment(int id);
        Task ExpireOverdueAppointmentsAsync();
    }
}
