using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] AccountRequestDTO loginDTO)
        {
            try
            {
                var account = await _authService.Login(loginDTO.Email, loginDTO.Password);
                if (account == null)
                {
                    return Unauthorized("Invalid email or password.");
                }

                var role = account.Role.Name;

                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, true).Build();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, account.Email),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("AccountId", account.Id.ToString()),
                };

                var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]));
                var signCredential = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

                var preparedToken = new JwtSecurityToken(
                    issuer: configuration["JWT:Issuer"],
                    audience: configuration["JWT:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(16),
                    signingCredentials: signCredential);

                var generatedToken = new JwtSecurityTokenHandler().WriteToken(preparedToken);
                var accountId = account.Id.ToString();

                return Ok(new AccountResponseDTO
                {
                    Role = role,
                    Token = generatedToken,
                    AccountId = accountId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
