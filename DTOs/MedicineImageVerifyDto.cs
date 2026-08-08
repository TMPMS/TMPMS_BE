namespace TMPMS.DTOs
{
    public class MedicineImageVerifyRequestDto
    {
        public string? ImageUrl { get; set; }
        public string? ImageBase64 { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
    }

    public class MedicineImageVerifyResponseDto
    {
        public bool IsMatch { get; set; }
        public bool IsMedicineImage { get; set; }
        public int ConfidenceScore { get; set; }
        public string DetectedName { get; set; } = "";
        public string WarningMessage { get; set; } = "";
    }
}
