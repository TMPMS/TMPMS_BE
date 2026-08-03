using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> Register(RegisterRequestDTO dto);
        Task<AuthResponseDTO> Login(LoginRequestDTO dto, string ipAddress);
        Task<AuthResponseDTO> OtpLogin(OtpLoginRequestDTO dto, string ipAddress);
        Task<AuthResponseDTO> GoogleLogin(GoogleLoginRequestDTO dto, string ipAddress);
        Task<bool> SendOtp(string phone);
        Task<AuthResponseDTO> RefreshToken(string refreshToken, string ipAddress);
        Task<bool> RevokeToken(string refreshToken, string ipAddress);
        Task<bool> AssignRole(AssignRoleRequestDTO dto);
        Task<UserProfileDTO> GetProfile(int userId);
        Task<bool> ChangePassword(int userId, ChangePasswordRequestDTO dto);
    }
}
