namespace TMPMS.DTOs
{
    public class RegisterRequestDTO
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Phone { get; set; }
        // Mặc định user tự đăng ký là Customer; Admin muốn tạo Doctor/Pharmacist... thì dùng AssignRole
        public string RoleName { get; set; } = "Customer";
    }

    public class LoginRequestDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RefreshTokenRequestDTO
    {
        public string RefreshToken { get; set; }
    }

    public class RevokeTokenRequestDTO
    {
        public string RefreshToken { get; set; }
    }

    public class AssignRoleRequestDTO
    {
        public int UserId { get; set; }
        public string RoleName { get; set; }
    }

    public class ChangePasswordRequestDTO
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class AuthResponseDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; }
    }

    public class UserProfileDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
