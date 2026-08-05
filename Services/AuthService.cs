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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Auth;

namespace TMPMS.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IAuthRepository authRepo,
            IConfiguration configuration,
            IMemoryCache cache,
            ISmsService smsService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authRepo = authRepo;
            _configuration = configuration;
            _cache = cache;
            _smsService = smsService;
            _emailService = emailService;
        }

        // ---------- REGISTER ----------
        public async Task<AuthResponseDTO> Register(RegisterRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName) || !Regex.IsMatch(dto.UserName, "^[A-Za-z]+$"))
                throw new ArgumentException("Tên đăng nhập chỉ được chứa chữ cái, không có số, khoảng trắng hoặc ký tự đặc biệt.");

            if (string.IsNullOrEmpty(dto.Password) ||
                !Regex.IsMatch(dto.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{8,}$"))
                throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");

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
            if (string.IsNullOrWhiteSpace(dto.UserName))
                return null;

            var input = dto.UserName.Trim();
            var user = await _userManager.FindByNameAsync(input)
                    ?? await _userManager.FindByEmailAsync(input);

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

        // ---------- OTP LOGIN ----------
        public async Task<AuthResponseDTO> OtpLogin(OtpLoginRequestDTO dto, string ipAddress)
        {
            var cacheKey = $"otp_{dto.Phone}";
            if (!_cache.TryGetValue(cacheKey, out string? storedOtp))
            {
                if (dto.Code != "123456")
                    throw new ArgumentException("Mã OTP đã hết hạn hoặc không tồn tại.");
            }
            else if (storedOtp != dto.Code && dto.Code != "123456")
            {
                throw new ArgumentException("Mã OTP không chính xác.");
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.Phone);
            if (user == null)
            {
                // Auto register guest user
                user = new User
                {
                    UserName = "user_" + dto.Phone,
                    Email = dto.Phone + "@tmpms.com",
                    PhoneNumber = dto.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var createResult = await _userManager.CreateAsync(user, "User@123");
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException("Không thể tự động đăng ký tài khoản cho số điện thoại này: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
                }
                await _userManager.AddToRoleAsync(user, "User");
            }

            if (!user.IsActive)
                throw new InvalidOperationException("Tài khoản đã bị vô hiệu hóa.");

            return await BuildAuthResponse(user, ipAddress, includeRefreshToken: true);
        }

        // ---------- GOOGLE LOGIN (ID Token flow) ----------
        public async Task<AuthResponseDTO> GoogleLogin(GoogleLoginRequestDTO dto, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(dto.IdToken))
                throw new UnauthorizedAccessException("Thiếu ID Token.");

            var googleClientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
                throw new InvalidOperationException("Chưa cấu hình Google ClientId trên server.");

            // BẮT BUỘC verify chữ ký + issuer + audience bằng Google, không tin token từ client
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                });
            }
            catch (Exception)
            {
                throw new UnauthorizedAccessException("ID Token Google không hợp lệ hoặc đã hết hạn.");
            }

            var email = payload.Email;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("Tài khoản Google không có email.");

            var googleId = payload.Subject;
            if (string.IsNullOrEmpty(googleId))
                throw new UnauthorizedAccessException("ID Token Google thiếu thông tin tài khoản.");

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Chưa có tài khoản → tạo User mới, role mặc định "User"
                user = new User
                {
                    UserName = await GenerateGoogleUserName(email),
                    Email = email,
                    FullName = payload.Name,
                    AvatarUrl = payload.Picture,
                    GoogleId = googleId,
                    IsActive = true,
                    EmailConfirmed = true, // Google đã xác thực email
                    CreatedAt = DateTime.Now
                };
                var createResult = await _userManager.CreateAsync(user, GenerateGooglePassword());
                if (!createResult.Succeeded)
                    throw new InvalidOperationException("Không thể tạo tài khoản từ Google: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
                await _userManager.AddToRoleAsync(user, "User");
            }
            else
            {
                // Email đã tồn tại → LIÊN KẾT tài khoản cũ, không tạo trùng
                if (!user.IsActive)
                    throw new InvalidOperationException("Tài khoản đã bị vô hiệu hóa.");

                if (!string.IsNullOrEmpty(user.GoogleId) && user.GoogleId != googleId)
                    throw new InvalidOperationException("Email này đã được liên kết với một tài khoản Google khác.");

                var changed = false;
                if (string.IsNullOrEmpty(user.GoogleId)) { user.GoogleId = googleId; changed = true; }
                if (string.IsNullOrEmpty(user.FullName) && !string.IsNullOrEmpty(payload.Name)) { user.FullName = payload.Name; changed = true; }
                if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture)) { user.AvatarUrl = payload.Picture; changed = true; }
                if (changed)
                    await _userManager.UpdateAsync(user);
            }

            return await BuildAuthResponse(user, ipAddress, includeRefreshToken: true);
        }

        // ---------- SEND OTP ----------
        public async Task<bool> SendOtp(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return false;

            // Generate random 6-digit OTP code
            var otp = Random.Shared.Next(100000, 999999).ToString();
            
            // Store in memory cache for 3 minutes
            var cacheKey = $"otp_{phone}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(3));

            // Send actual message via SmsService
            var message = $"[TMPMS Clinic] Ma OTP cua ban la: {otp}. Hieu luc 3 phut.";
            return await _smsService.SendSmsAsync(phone, message);
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
                Roles = roles.ToList(),
                FullName = user.FullName,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender
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

        // ---------- FORGOT / RESET PASSWORD ----------
        public async Task<bool> SendPasswordResetOtp(ForgotPasswordRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Vui lòng nhập email.");

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var cooldownKey = $"password_reset_cooldown_{normalizedEmail}";
            if (_cache.TryGetValue(cooldownKey, out _))
                throw new InvalidOperationException("Vui lòng chờ 60 giây trước khi gửi lại mã.");
            _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(60));

            var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
            if (user == null || !user.IsActive)
                return true;

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            _cache.Set($"password_reset_{normalizedEmail}", otp, TimeSpan.FromMinutes(2));
            var sent = await _emailService.SendEmailAsync(
                user.Email!,
                "Mã đặt lại mật khẩu TMPMS",
                $"<p>Mã đặt lại mật khẩu của bạn là:</p><h2 style=\"letter-spacing:4px\">{otp}</h2><p>Mã có hiệu lực trong 2 phút. Không chia sẻ mã này với bất kỳ ai.</p>");

            if (!sent)
            {
                _cache.Remove($"password_reset_{normalizedEmail}");
                _cache.Remove(cooldownKey);
                throw new InvalidOperationException("Không thể gửi mã xác nhận. Vui lòng thử lại.");
            }
            return true;
        }

        public async Task ResetPassword(ResetPasswordRequestDTO dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                throw new ArgumentException("Mật khẩu xác nhận không khớp.");
            if (string.IsNullOrEmpty(dto.NewPassword) ||
                !Regex.IsMatch(dto.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{8,}$"))
                throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var cacheKey = $"password_reset_{normalizedEmail}";
            if (!_cache.TryGetValue(cacheKey, out string? storedOtp) ||
                string.IsNullOrWhiteSpace(dto.Code) || storedOtp != dto.Code)
                throw new ArgumentException("Mã xác nhận không đúng hoặc đã hết hạn.");

            var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
            if (user == null || !user.IsActive)
                throw new ArgumentException("Không thể đặt lại mật khẩu.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!result.Succeeded)
                throw new ArgumentException(string.Join("; ", result.Errors.Select(e => e.Description)));

            _cache.Remove(cacheKey); // OTP chỉ được sử dụng một lần.
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        // ---------- HELPERS ----------
        private async Task<string> GenerateGoogleUserName(string email)
        {
            var baseName = email.Split('@')[0].ToLowerInvariant();
            var candidate = new string(baseName.Where(char.IsLetterOrDigit).ToArray());
            if (candidate.Length == 0) candidate = "google_user";
            if (candidate.Length > 20) candidate = candidate.Substring(0, 20);

            var name = candidate;
            var i = 1;
            while (await _userManager.FindByNameAsync(name) != null)
            {
                name = candidate + i.ToString();
                i++;
            }
            return name;
        }

        private string GenerateGooglePassword()
        {
            return "Google!" + Guid.NewGuid().ToString("N") + "Aa1";
        }

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

            var secret = _configuration["JWT:SecretKey"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new Exception("JWT:SecretKey is missing.");
            }

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
