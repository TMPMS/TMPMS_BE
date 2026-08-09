using System.ComponentModel.DataAnnotations;

namespace TMPMS.DTOs
{
    public class RegisterRequestDTO
    {
        [Required, StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
        [Phone]
        public string Phone { get; set; }
        // Mặc định user tự đăng ký là Customer; Admin muốn tạo Doctor/Pharmacist... thì dùng AssignRole
        public string RoleName { get; set; } = "Customer";
    }

    public class LoginRequestDTO
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class OtpLoginRequestDTO
    {
        public string Phone { get; set; }
        public string Code { get; set; }
    }

    public class GoogleLoginRequestDTO
    {
        public string IdToken { get; set; }
    }

    public class SendOtpRequestDTO
    {
        public string Phone { get; set; }
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
        [Required]
        public string CurrentPassword { get; set; }
        [Required, StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }

    public class ForgotPasswordRequestDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordRequestDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Code { get; set; }
        [Required, StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
        [Required, Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }
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
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
    }
}
