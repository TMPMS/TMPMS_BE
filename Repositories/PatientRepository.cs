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
            var appointmentUserIds = await _context.Appointments.Select(a => a.UserId).Distinct().ToListAsync();
            var diagnosisPatientIds = await _context.Diagnoses.Select(d => d.PatientId).Distinct().ToListAsync();
            var activePatientIds = new HashSet<int>(appointmentUserIds.Concat(diagnosisPatientIds));

            var patients = new List<PatientDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                bool isStaffOrAdmin = roles.Contains("Admin") || roles.Contains("Doctor") || roles.Contains("Pharmacy") || roles.Contains("Staff");

                // Include user as a patient if they have Patient/User role OR if they booked an appointment or diagnosis (and are not purely staff/admin)
                if (roles.Contains("Patient") || roles.Contains("User") || activePatientIds.Contains(user.Id) || !isStaffOrAdmin)
                {
                    patients.Add(new PatientDto
                    {
                        Id = user.Id,
                        Name = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.UserName,
                        Username = user.UserName ?? "",
                        Email = user.Email ?? "",
                        PhoneNumber = user.PhoneNumber ?? "",
                        Gender = !string.IsNullOrEmpty(user.Gender) ? user.Gender : "Nam",
                        DateOfBirth = user.DateOfBirth,
                        Address = user.Address ?? "",
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        Role = roles.FirstOrDefault() ?? "User"
                    });
                }
            }

            return patients;
        }

        public async Task<bool> AddPatientAsync(PatientCreateDTO dto)
        {
            string phoneNum = dto.PhoneNumber ?? dto.Phone ?? "";
            string uname = string.IsNullOrEmpty(dto.Username) ? "patient_" + (string.IsNullOrEmpty(phoneNum) ? DateTime.Now.Ticks.ToString() : phoneNum) : dto.Username;
            string email = string.IsNullOrEmpty(dto.Email) ? (string.IsNullOrEmpty(phoneNum) ? uname : phoneNum) + "@patient.com" : dto.Email;

            // Check if patient with same email or username already exists
            var existingUser = await _userManager.FindByEmailAsync(email) ?? await _userManager.FindByNameAsync(uname);
            if (existingUser != null)
            {
                existingUser.FullName = !string.IsNullOrEmpty(dto.Name) ? dto.Name : existingUser.FullName;
                existingUser.PhoneNumber = !string.IsNullOrEmpty(phoneNum) ? phoneNum : existingUser.PhoneNumber;
                existingUser.Gender = !string.IsNullOrEmpty(dto.Gender) ? dto.Gender : existingUser.Gender;
                existingUser.Address = !string.IsNullOrEmpty(dto.Address) ? dto.Address : existingUser.Address;
                if (dto.DateOfBirth.HasValue) existingUser.DateOfBirth = dto.DateOfBirth.Value;

                var updateRes = await _userManager.UpdateAsync(existingUser);
                return updateRes.Succeeded;
            }

            var user = new User
            {
                UserName = uname,
                FullName = dto.Name ?? uname,
                Email = email,
                PhoneNumber = phoneNum,
                Gender = dto.Gender ?? "Nam",
                DateOfBirth = dto.DateOfBirth ?? DateTime.UtcNow.AddYears(-25),
                Address = dto.Address ?? "",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            string pwd = string.IsNullOrEmpty(dto.Password) ? "Patient@123" : dto.Password;
            var result = await _userManager.CreateAsync(user, pwd);

            if (!result.Succeeded)
                return false;

            try
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
            catch
            {
                // Fallback if role addition fails
            }

            return true;
        }

        public async Task<bool> UpdatePatientAsync(int id, UpdatePatientDto dto)
        {
            var patient = await _userManager.FindByIdAsync(id.ToString());

            if (patient == null)
                return false;

            if (!string.IsNullOrEmpty(dto.Username)) patient.UserName = dto.Username;
            if (!string.IsNullOrEmpty(dto.Name)) patient.FullName = dto.Name;
            if (!string.IsNullOrEmpty(dto.Email)) patient.Email = dto.Email;
            
            string phoneNum = dto.PhoneNumber ?? dto.Phone;
            if (!string.IsNullOrEmpty(phoneNum)) patient.PhoneNumber = phoneNum;
            if (!string.IsNullOrEmpty(dto.Gender)) patient.Gender = dto.Gender;
            if (dto.DateOfBirth.HasValue) patient.DateOfBirth = dto.DateOfBirth.Value;
            if (dto.Address != null) patient.Address = dto.Address;
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
                    (x.UserName != null && x.UserName.Contains(keyword)) ||
                    (x.FullName != null && x.FullName.Contains(keyword)) ||
                    (x.Email != null && x.Email.Contains(keyword)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword)))
                .ToListAsync();

            var patients = new List<PatientDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Patient") || roles.Contains("User"))
                {
                    patients.Add(new PatientDto
                    {
                        Id = user.Id,
                        Name = user.FullName ?? user.UserName,
                        Username = user.UserName ?? "",
                        Email = user.Email ?? "",
                        PhoneNumber = user.PhoneNumber ?? "",
                        Gender = user.Gender ?? "Nam",
                        DateOfBirth = user.DateOfBirth,
                        Address = user.Address ?? "",
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
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
