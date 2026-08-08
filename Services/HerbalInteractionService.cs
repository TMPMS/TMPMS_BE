using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class HerbalInteractionService : IHerbalInteractionService
    {
        private readonly IHerbalInteractionRepository _repo;
        public HerbalInteractionService(IHerbalInteractionRepository repo) => _repo = repo;

        public async Task<SafetyCheckResponseDTO> CheckSafety(SafetyCheckRequestDTO dto)
        {
            var distinctIds = dto.MedicineIds.Distinct().ToList();
            var conflicts = new List<InteractionConflictDTO>();

            if (distinctIds.Count >= 2)
            {
                var matches = await _repo.GetConflictsAmong(distinctIds);
                conflicts = matches.Select(hi => new InteractionConflictDTO
                {
                    HerbAId = hi.HerbAId,
                    HerbAName = hi.HerbA?.Name,
                    HerbBId = hi.HerbBId,
                    HerbBName = hi.HerbB?.Name,
                    InteractionType = hi.InteractionType,
                    Severity = hi.Severity,
                    MechanismDescription = hi.MechanismDescription,
                    ReplacementForAId = hi.SuggestedReplacementForAId,
                    ReplacementForAName = hi.SuggestedReplacementForA?.Name,
                    ReplacementForBId = hi.SuggestedReplacementForBId,
                    ReplacementForBName = hi.SuggestedReplacementForB?.Name
                }).ToList();
            }

            string maxSeverity = null;
            if (conflicts.Any(c => c.Severity == "Critical")) maxSeverity = "Critical";
            else if (conflicts.Any(c => c.Severity == "Warning")) maxSeverity = "Warning";
            else if (conflicts.Any(c => c.Severity == "Notice")) maxSeverity = "Notice";

            return new SafetyCheckResponseDTO
            {
                IsSafe = conflicts.Count == 0,
                MaxSeverity = maxSeverity,
                Conflicts = conflicts
            };
        }
    }
}
