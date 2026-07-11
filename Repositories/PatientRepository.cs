using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly TMPMSDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientRepository(
            TMPMSDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<PatientDto>> GetAllPatientsAsync()
        {
            var users = await _context.Users.ToListAsync();

            var patients = new List<PatientDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Customer"))
                {
                    patients.Add(new PatientDto
                    {
                        Id = user.Id,
                        Username = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        Role = roles.FirstOrDefault()
                    });
                }
            }

            return patients;
        }

        public async Task<bool> AddPatientAsync(PatientCreateDTO dto)
        {
            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return false;

            // Gán Role Patient (hoặc Customer)
            await _userManager.AddToRoleAsync(user, "Patient");

            return true;
        }

        public async Task<bool> UpdatePatientAsync(int id, UpdatePatientDto dto)
        {
            var patient = await _userManager.FindByIdAsync(id.ToString());

            if (patient == null)
                return false;

            patient.UserName = dto.Username;
            patient.Email = dto.Email;
            patient.PhoneNumber = dto.PhoneNumber;
            patient.IsActive = dto.IsActive;

            var result = await _userManager.UpdateAsync(patient);

            return result.Succeeded;
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            var patient = await _userManager.FindByIdAsync(id.ToString());

            if (patient == null)
                return false;

            var result = await _userManager.DeleteAsync(patient);

            return result.Succeeded;
        }

        public async Task<List<PatientDto>> SearchPatientsAsync(string keyword)
        {
            var users = await _context.Users
                .Where(x =>
                    x.UserName.Contains(keyword) ||
    (x.Email != null && x.Email.Contains(keyword)) ||
    (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword)))
                .ToListAsync();

            var patients = new List<PatientDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Patient"))   // hoặc Customer nếu project bạn dùng Customer
                {
                    patients.Add(new PatientDto
                    {
                        Id = user.Id,
                        Username = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        Role = roles.FirstOrDefault()
                    });
                }
            }

            return patients;
        }
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
