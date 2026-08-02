using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<bool> BookAppointment(int userId, AppointmentCreateDTO dto);
        Task<List<AppointmentDTO>> GetAppointments(int userId);
        Task<List<AppointmentDTO>> GetAllAppointments();
        Task<bool> UpdateAppointment(int id, AppointmentUpdateDTO dto);
        Task<bool> DeleteAppointment(int id);
        Task<bool> CancelAppointment(int id);
        Task<bool> ApproveAppointment(int id);
        Task<bool> CompleteAppointment(int id);
    }
}
