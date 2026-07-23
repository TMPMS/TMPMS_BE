using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class PrescriptionItemRepository : IPrescriptionItemRepository
    {
        private readonly TMPMSDbContext _context;
        public PrescriptionItemRepository(TMPMSDbContext context)
        {
            _context = context;
        }

        public async Task<List<PrescriptionItem>> GetPrescriptionItemsByPrescriptionIdAsync(int prescriptionId)
        {
            return await _context.PrescriptionItems
                                 .Include(pi => pi.Medicine)
                                 .Where(pi => pi.PrescriptionId == prescriptionId)
                                 .ToListAsync();
        }
    }
}
