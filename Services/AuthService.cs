using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Models;

namespace TMPMS.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IAuthRepository authRepo,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authRepo = authRepo;
            _configuration = configuration;
        }

        // ---------- REGISTER ----------
        public async Task<AuthResponseDTO> Register(RegisterRequestDTO dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                throw new ArgumentException("Mật khẩu xác nhận không khớp.");

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new ArgumentException("Email đã được sử dụng.");

            if (!await _roleManager.RoleExistsAsync(dto.RoleName))
                throw new ArgumentException($"Role '{dto.RoleName}' chưa tồn tại. Vui lòng seed role trước.");

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.Phone,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new ArgumentException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, dto.RoleName);

            return await BuildAuthResponse(user, ipAddress: null, includeRefreshToken: true);
        }

        // ---------- LOGIN ----------
        public async Task<AuthResponseDTO> Login(LoginRequestDTO dto, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                return null;

            if (await _userManager.IsLockedOutAsync(user))
                throw new InvalidOperationException("Tài khoản đang bị tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau.");

            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!validPassword)
            {
                await _userManager.AccessFailedAsync(user);
                return null;
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            return await BuildAuthResponse(user, ipAddress, includeRefreshToken: true);
        }

        // ---------- REFRESH TOKEN (rotation) ----------
        public async Task<AuthResponseDTO> RefreshToken(string refreshToken, string ipAddress)
        {
            var existingToken = await _authRepo.GetRefreshToken(refreshToken);
            if (existingToken == null || !existingToken.IsActive)
                throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");

            var user = existingToken.User ?? await _userManager.FindByIdAsync(existingToken.UserId.ToString());
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Người dùng không hợp lệ.");

            // Thu hồi token cũ, cấp token mới (rotation)
            var newRefreshTokenEntity = GenerateRefreshTokenEntity(user.Id, ipAddress);
            existingToken.RevokedAt = DateTime.Now;
            existingToken.RevokedByIp = ipAddress;
            existingToken.ReplacedByToken = newRefreshTokenEntity.Token;
            await _authRepo.UpdateRefreshToken(existingToken);
            await _authRepo.SaveRefreshToken(newRefreshTokenEntity);

            var roles = await _userManager.GetRolesAsync(user);
            var (accessToken, expiresAt) = GenerateAccessToken(user, roles);

            return new AuthResponseDTO
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles.ToList(),
                AccessToken = accessToken,
                AccessTokenExpiresAt = expiresAt,
                RefreshToken = newRefreshTokenEntity.Token
            };
        }

        // ---------- REVOKE (logout) ----------
        public async Task<bool> RevokeToken(string refreshToken, string ipAddress)
        {
            var existingToken = await _authRepo.GetRefreshToken(refreshToken);
            if (existingToken == null || !existingToken.IsActive) return false;

            existingToken.RevokedAt = DateTime.Now;
            existingToken.RevokedByIp = ipAddress;
            await _authRepo.UpdateRefreshToken(existingToken);
            return true;
        }

        // ---------- ASSIGN ROLE ----------
        public async Task<bool> AssignRole(AssignRoleRequestDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (user == null) return false;

            if (!await _roleManager.RoleExistsAsync(dto.RoleName))
                await _roleManager.CreateAsync(new Role(dto.RoleName));

            if (await _userManager.IsInRoleAsync(user, dto.RoleName)) return true;

            var result = await _userManager.AddToRoleAsync(user, dto.RoleName);
            return result.Succeeded;
        }

        // ---------- PROFILE ----------
        public async Task<UserProfileDTO> GetProfile(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };
        }

        // ---------- CHANGE PASSWORD ----------
        public async Task<bool> ChangePassword(int userId, ChangePasswordRequestDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new ArgumentException(string.Join("; ", result.Errors.Select(e => e.Description)));

            return true;
        }

        // ---------- HELPERS ----------
        private async Task<AuthResponseDTO> BuildAuthResponse(User user, string ipAddress, bool includeRefreshToken)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var (accessToken, expiresAt) = GenerateAccessToken(user, roles);

            string refreshTokenValue = null;
            if (includeRefreshToken)
            {
                var refreshTokenEntity = GenerateRefreshTokenEntity(user.Id, ipAddress);
                await _authRepo.SaveRefreshToken(refreshTokenEntity);
                refreshTokenValue = refreshTokenEntity.Token;
            }

            return new AuthResponseDTO
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles.ToList(),
                AccessToken = accessToken,
                AccessTokenExpiresAt = expiresAt,
                RefreshToken = refreshTokenValue
            };
        }

        private (string token, DateTime expiresAt) GenerateAccessToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
            var signCredential = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            var accessTokenMinutes = int.TryParse(_configuration["JWT:AccessTokenExpiryMinutes"], out var m) ? m : 30;
            var expiresAt = DateTime.Now.AddMinutes(accessTokenMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: signCredential);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        private RefreshToken GenerateRefreshTokenEntity(int userId, string ipAddress)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var refreshTokenDays = int.TryParse(_configuration["JWT:RefreshTokenExpiryDays"], out var d) ? d : 7;

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                UserId = userId,
                ExpiresAt = DateTime.Now.AddDays(refreshTokenDays),
                CreatedAt = DateTime.Now,
                CreatedByIp = ipAddress
            };
        }
    }
}
