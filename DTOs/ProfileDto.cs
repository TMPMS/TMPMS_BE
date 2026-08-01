namespace TMPMS.DTOs
{
    public class ProfileDto
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public bool IsActive { get; set; }

        public string Address { get; set; }

        public string? FullName { get; set; }

        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }
    }
}
