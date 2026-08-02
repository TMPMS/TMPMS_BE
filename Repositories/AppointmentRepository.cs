using BusinessObjects;
using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> IsAppointmentExist(int staffId, DateTime appointmentDate)
        {
            return await _context.Appointments.AnyAsync(x =>
                x.StaffId == staffId &&
                x.AppointmentDate == appointmentDate &&
                x.Status != "Cancelled");
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

        public async Task<bool> IsAppointmentExist(int? staffId, DateTime appointmentDate, int appointmentId)
        {
            return await _context.Appointments.AnyAsync(x =>
                x.Id != appointmentId &&
                x.StaffId == staffId &&
                x.AppointmentDate == appointmentDate &&
                x.Status != "Cancelled");
        }
    }
}
