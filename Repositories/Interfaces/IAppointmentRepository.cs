using BusinessObjects;
using TMPMS.Models;

namespace TMPMS.Repositories.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<bool> Add(Appointment appointment);
        // Kiểm tra + thêm lịch hẹn nguyên tử trong 1 transaction Serializable — tránh race condition giữa
        // IsAppointmentExist (đọc) và Add (ghi) khi 2 request đặt cùng bác sĩ/thời điểm chạy đồng thời,
        // cả hai đều thấy "chưa có lịch" rồi cùng chèn (double-booking). Trả về false nếu slot đã bị
        // chiếm (không insert), true nếu đặt thành công.
        Task<bool> TryAddIfSlotFreeAsync(Appointment appointment);
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
