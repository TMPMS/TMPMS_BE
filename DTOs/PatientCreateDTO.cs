namespace TMPMS.DTOs
{
    public class PatientCreateDTO
    {
        public string? Name { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Phone { get; set; } // Alias for PhoneNumber
        public string? Password { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? MedicalHistory { get; set; }
    }
}
