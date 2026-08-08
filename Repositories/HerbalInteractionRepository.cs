using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class HerbalInteractionRepository : IHerbalInteractionRepository
    {
        private readonly TMPMSDbContext _context;
        public HerbalInteractionRepository(TMPMSDbContext context) => _context = context;

        public async Task<List<HerbalInteraction>> GetConflictsAmong(List<int> medicineIds)
        {
            return await _context.HerbalInteractions
                .Include(hi => hi.HerbA)
                .Include(hi => hi.HerbB)
                .Include(hi => hi.SuggestedReplacementForA)
                .Include(hi => hi.SuggestedReplacementForB)
                .Where(hi => medicineIds.Contains(hi.HerbAId) && medicineIds.Contains(hi.HerbBId))
                .ToListAsync();
        }
    }
}
