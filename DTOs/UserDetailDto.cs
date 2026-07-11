namespace TMPMS.DTOs
{
    public class UserDetailDto
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public string RoleName { get; set; }
    }
}
