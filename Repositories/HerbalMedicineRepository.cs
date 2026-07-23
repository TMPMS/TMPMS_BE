using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class HerbalMedicineRepository : IHerbalMedicineRepository
    {
        private readonly TMPMSDbContext _context;
        public HerbalMedicineRepository(TMPMSDbContext context) => _context = context;

        public async Task<Medicine> CreateMedicine(Medicine medicine)
        {
            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();
            return medicine;
        }

        public async Task<HerbalMedicineInfo> CreateHerbalInfo(HerbalMedicineInfo info)
        {
            _context.HerbalMedicineInfos.Add(info);
            await _context.SaveChangesAsync();
            return info;
        }

        public async Task<HerbalMedicineInfo> GetHerbalInfoByMedicineId(int medicineId)
        {
            return await _context.HerbalMedicineInfos
                .Include(h => h.Medicine).ThenInclude(m => m.Category)
                .FirstOrDefaultAsync(h => h.MedicineId == medicineId);
        }

        public async Task<Medicine> GetMedicineById(int medicineId)
        {
            return await _context.Medicines.Include(m => m.Category).FirstOrDefaultAsync(m => m.Id == medicineId);
        }

        public async Task<List<HerbalMedicineInfo>> GetAllHerbal()
        {
            return await _context.HerbalMedicineInfos
                .Include(h => h.Medicine).ThenInclude(m => m.Category)
                .ToListAsync();
        }

        public async Task<HerbalMedicineInfo> Update(HerbalMedicineInfo info)
        {
            _context.HerbalMedicineInfos.Update(info);
            await _context.SaveChangesAsync();
            return info;
        }

        public async Task<bool> Delete(int medicineId)
        {
            var entity = await _context.HerbalMedicineInfos.FirstOrDefaultAsync(h => h.MedicineId == medicineId);
            if (entity == null) return false;
            _context.HerbalMedicineInfos.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
