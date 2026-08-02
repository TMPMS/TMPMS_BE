namespace TMPMS.DTOs
{
    public class PatientDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Role { get; set; }
    }
}
