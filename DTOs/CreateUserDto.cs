namespace TMPMS.DTOs
{
        namespace TMPMS_BE.DTOs
    {
        public class CreateUserDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string RoleName { get; set; }
        }
    }
}

