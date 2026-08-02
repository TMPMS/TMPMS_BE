namespace TMPMS.DTOs
{
    public class PatientOperationResult
    {
        public bool Success { get; set; }
        public bool IsConflict { get; set; }
        public string Message { get; set; } = "";
        public int? ConflictUserId { get; set; }
        public string? ConflictUserName { get; set; }
    }
}
