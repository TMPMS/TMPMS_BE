using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class PrescriptionRepository : IPrescriptionRepository
    {
        private readonly TMPMSDbContext _context;
        public PrescriptionRepository(TMPMSDbContext context) => _context = context;

        public async Task<Prescription> Create(Prescription prescription)
        {
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            return prescription;
        }

        public async Task<Prescription> GetById(int id)
        {
            return await _context.Prescriptions
                .Include(p => p.User)
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionItems).ThenInclude(i => i.Medicine)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Prescription>> GetByUser(int userId)
        {
            // Bao gồm cả đơn bệnh nhân này tự gửi (UserId) lẫn đơn người khác gửi hộ
            // nhưng Dược sĩ đã xác nhận đúng bệnh nhân là hồ sơ này (PatientId).
            return await _context.Prescriptions
                .Include(p => p.PrescriptionItems).ThenInclude(i => i.Medicine)
                .Where(p => p.UserId == userId || p.PatientId == userId)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();
        }

        public async Task<List<Prescription>> GetByStatus(string status)
        {
            return await _context.Prescriptions
                .Include(p => p.User)
                .Include(p => p.Patient)
                .Include(p => p.PrescriptionItems).ThenInclude(i => i.Medicine)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();
        }

        public async Task<List<Prescription>> GetAll()
        {
            return await _context.Prescriptions
                .Include(p => p.User)
                .Include(p => p.Patient)
                .Include(p => p.PrescriptionItems).ThenInclude(i => i.Medicine)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();
        }

        public async Task<Prescription> Update(Prescription prescription)
        {
            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();
            return prescription;
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _context.Prescriptions.FindAsync(id);
            if (entity == null) return false;
            _context.Prescriptions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Medicine> GetMedicineById(int medicineId)
        {
            return await _context.Medicines.FindAsync(medicineId);
        }

        public async Task<List<Prescription>> GetPrescriptionsByPatientIdAsync(int patientId)
        {
            return await _context.Prescriptions
                .Include(p => p.User)
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.Diagnosis)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medicine)
                .Where(p => p.UserId == patientId || p.PatientId == patientId)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();
        }
    }
}
