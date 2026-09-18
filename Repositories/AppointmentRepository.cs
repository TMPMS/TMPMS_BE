using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TMPMS.Data;
using TMPMS.Models;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly TMPMSDbContext _context;

        public AppointmentRepository(TMPMSDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Add(Appointment appointment)
        {
            await _context.Appointments.AddAsync(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> TryAddIfSlotFreeAsync(Appointment appointment)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Không chọn bác sĩ cụ thể (StaffId null) trước đây BỎ QUA HẲN kiểm tra trùng giờ — có
                // thể đặt vô hạn lịch hẹn vào cùng 1 khung giờ nếu không ai chọn bác sĩ. Rơi về kiểm tra
                // theo Location+AppointmentDate (so với các lịch cũng chưa gán bác sĩ) khi không có
                // StaffId cụ thể để so — không gộp chung với lịch đã có bác sĩ riêng vì đó là năng lực
                // phục vụ song song khác, không tính là trùng chỗ.
                var exists = appointment.StaffId != null
                    ? await _context.Appointments.AnyAsync(x =>
                        x.StaffId == appointment.StaffId &&
                        x.AppointmentDate == appointment.AppointmentDate &&
                        (x.Status == "PendingConfirmation" || x.Status == "Confirmed"))
                    : await _context.Appointments.AnyAsync(x =>
                        x.StaffId == null &&
                        x.Location == appointment.Location &&
                        x.AppointmentDate == appointment.AppointmentDate &&
                        (x.Status == "PendingConfirmation" || x.Status == "Confirmed"));

                if (exists)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<User?> GetUserById(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<User?> GetStaffById(int staffId)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == staffId);
        }

        public async Task<List<Appointment>> GetByUserId(int userId)
        {
            return await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Staff)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetAll()
        {
            return await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Staff)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<Appointment?> GetById(int id)
        {
            return await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> Update(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return false;
            _context.Appointments.Remove(appt);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsAppointmentExist(int? staffId, string location, DateTime appointmentDate, int appointmentId)
        {
            // Cùng lý do với TryAddIfSlotFreeAsync: không có StaffId thì so theo Location, không bỏ
            // qua kiểm tra hoàn toàn.
            return staffId != null
                ? await _context.Appointments.AnyAsync(x =>
                    x.Id != appointmentId &&
                    x.StaffId == staffId &&
                    x.AppointmentDate == appointmentDate &&
                    (x.Status == "PendingConfirmation" || x.Status == "Confirmed"))
                : await _context.Appointments.AnyAsync(x =>
                    x.Id != appointmentId &&
                    x.StaffId == null &&
                    x.Location == location &&
                    x.AppointmentDate == appointmentDate &&
                    (x.Status == "PendingConfirmation" || x.Status == "Confirmed"));
        }

        public async Task<bool> HasRecentActiveAppointment(int userId, DateTime since)
        {
            return await _context.Appointments.AnyAsync(x =>
                x.UserId == userId &&
                x.CreatedAt >= since &&
                (x.Status == "PendingConfirmation" || x.Status == "Pending" || x.Status == "Confirmed"));
        }

        public async Task<int> ExpireOverdueAppointmentsAsync(DateTime now, DateTime utcNow)
        {
            return await _context.Appointments
                .Where(x =>
                    (x.Status == "Pending" || x.Status == "Confirmed") && x.AppointmentDate < now ||
                    x.Status == "PendingConfirmation" && (x.ConfirmationDeadline < utcNow || x.AppointmentDate < now))
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, "Expired"));
        }

        public async Task<Appointment?> GetActiveAppointmentByUserId(int userId)
        {
            return await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Staff)
                .Where(a => a.UserId == userId && (a.Status == "PendingConfirmation" || a.Status == "Pending" || a.Status == "Confirmed"))
                .OrderBy(a => a.AppointmentDate)
                .FirstOrDefaultAsync();
        }
    }
}
