using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("patients")]
    public class PatientsApiController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientsApiController(TMPMSDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetPatients()
        {
            var users = await _context.Users.ToListAsync();
            var patients = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Patient") || roles.Contains("User")) // include standard users as patients too
                {
                    patients.Add(new {
                        id = user.Id,
                        name = user.FullName ?? user.UserName,
                        username = user.UserName,
                        email = user.Email,
                        gender = user.Gender ?? "Nam",
                        date_of_birth = user.DateOfBirth ?? DateTime.UtcNow.AddYears(-25),
                        phone = user.PhoneNumber,
                        address = user.Address ?? "",
                        created_at = user.CreatedAt
                    });
                }
            }

            return Ok(patients);
        }

        public class PatientInput
        {
            public string Name { get; set; } = "";
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Gender { get; set; } = "Nam";
            public DateTime? Date_Of_Birth { get; set; }
            public string Address { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] PatientInput input)
        {
            var user = new User
            {
                UserName = string.IsNullOrEmpty(input.Username) ? "patient_" + input.Phone : input.Username,
                Email = string.IsNullOrEmpty(input.Email) ? input.Phone + "@patient.com" : input.Email,
                PhoneNumber = input.Phone,
                FullName = input.Name,
                Gender = input.Gender,
                DateOfBirth = input.Date_Of_Birth ?? DateTime.UtcNow.AddYears(-25),
                Address = input.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, "Patient@123");
            if (!result.Succeeded)
            {
                return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, "Patient");

            return StatusCode(201, new {
                id = user.Id,
                name = user.FullName,
                username = user.UserName,
                email = user.Email,
                gender = user.Gender,
                date_of_birth = user.DateOfBirth,
                phone = user.PhoneNumber,
                address = user.Address,
                created_at = user.CreatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] PatientInput input)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("Patient not found");

            user.FullName = input.Name;
            if (!string.IsNullOrEmpty(input.Email)) user.Email = input.Email;
            user.PhoneNumber = input.Phone;
            user.Gender = input.Gender;
            if (input.Date_Of_Birth.HasValue) user.DateOfBirth = input.Date_Of_Birth.Value;
            user.Address = input.Address;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("Update patient failed");
            }

            return Ok(new {
                id = user.Id,
                name = user.FullName,
                username = user.UserName,
                email = user.Email,
                gender = user.Gender,
                date_of_birth = user.DateOfBirth,
                phone = user.PhoneNumber,
                address = user.Address,
                created_at = user.CreatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("Patient not found");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("Delete patient failed");
            }

            return Ok(new { message = "Deleted successfully" });
        }
    }
}
