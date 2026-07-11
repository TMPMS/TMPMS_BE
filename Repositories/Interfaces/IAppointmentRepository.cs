using BusinessObjects;
using TMPMS.Models;

namespace TMPMS.Repositories.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<bool> Add(Appointment appointment);

        Task<bool> IsAppointmentExist(int staffId, DateTime appointmentDate);

        Task<User?> GetUserById(int userId);

        Task<User?> GetStaffById(int staffId);

        Task<List<Appointment>> GetByUserId(int userId);

        Task<Appointment?> GetById(int id);

        Task<bool> Update(Appointment appointment);

        Task<bool> IsAppointmentExist(int? staffId, DateTime appointmentDate, int appointmentId);

    }
}
