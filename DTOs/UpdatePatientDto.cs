namespace TMPMS.DTOs
{
    public class UpdatePatientDto
    {
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Phone { get; set; } // Alias
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
