using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class SafetyCheckRequestDTO
    {
        public List<int> MedicineIds { get; set; } = new();
    }

    public class InteractionConflictDTO
    {
        public int HerbAId { get; set; }
        public string HerbAName { get; set; }
        public int HerbBId { get; set; }
        public string HerbBName { get; set; }
        public string InteractionType { get; set; }
        public string Severity { get; set; }
        public string MechanismDescription { get; set; }

        public int? ReplacementForAId { get; set; }
        public string ReplacementForAName { get; set; }
        public int? ReplacementForBId { get; set; }
        public string ReplacementForBName { get; set; }
    }

    public class SafetyCheckResponseDTO
    {
        public bool IsSafe { get; set; }
        public string MaxSeverity { get; set; } // "Critical" / "Warning" / null
        public List<InteractionConflictDTO> Conflicts { get; set; } = new();
    }

    public class HerbalInteractionSeedDTO
    {
        public string HerbAName { get; set; }
        public string HerbBName { get; set; }
        public string InteractionType { get; set; }
        public string Severity { get; set; }
        public string MechanismDescription { get; set; }
        public string ReplacementForAName { get; set; }
        public string ReplacementForBName { get; set; }
    }
}
