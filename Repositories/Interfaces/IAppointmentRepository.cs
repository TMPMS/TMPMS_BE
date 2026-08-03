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
        Task<List<Appointment>> GetAll();
        Task<Appointment?> GetById(int id);
        Task<bool> Update(Appointment appointment);
        Task<bool> Delete(int id);
        Task<bool> IsAppointmentExist(int? staffId, DateTime appointmentDate, int appointmentId);
        Task<bool> HasRecentActiveAppointment(int userId, DateTime since);
        Task<int> ExpireOverdueAppointmentsAsync(DateTime now, DateTime utcNow);
        Task<Appointment?> GetActiveAppointmentByUserId(int userId);
    }
}
