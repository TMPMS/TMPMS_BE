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
            var activePatientUserIds = new HashSet<int>(appointmentUserIds.Concat(diagnosisPatientIds));
            var rolesByUser = await GetRolesByUserIdAsync();

            var patients = new List<PatientDto>();

            foreach (var user in users)
            {
                var roles = rolesByUser.TryGetValue(user.Id, out var userRoles) ? userRoles : new List<string>();
                bool isStaffOrAdmin = roles.Contains("Admin") || roles.Contains("Doctor") || roles.Contains("Pharmacy") || roles.Contains("Staff");

                // STRICT BUSINESS RULE: A user is ONLY listed as a Patient if they have an active Appointment or Diagnosis record (or explicit Patient role)
                if (!isStaffOrAdmin && (activePatientUserIds.Contains(user.Id) || roles.Contains("Patient")))
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
                        MedicalHistory = user.MedicalHistory,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        Role = roles.FirstOrDefault() ?? "Patient"
                    });
                }
            }

            return patients;
        }

        // Batch-resolves roles for every user in one query instead of one UserManager call per user (fixes N+1).
        private async Task<Dictionary<int, List<string>>> GetRolesByUserIdAsync()
        {
            var userRoleNames = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                select new { ur.UserId, r.Name }
            ).ToListAsync();

            return userRoleNames
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name ?? "").ToList());
        }

        public async Task<PatientOperationResult> AddPatientAsync(PatientCreateDTO dto)
        {
            string phoneNum = (dto.PhoneNumber ?? dto.Phone ?? "").Trim();
            string uname = string.IsNullOrEmpty(dto.Username) ? "patient_" + (string.IsNullOrEmpty(phoneNum) ? DateTime.Now.Ticks.ToString() : phoneNum) : dto.Username.Trim();
            string email = string.IsNullOrEmpty(dto.Email) ? (string.IsNullOrEmpty(phoneNum) ? uname : phoneNum) + "@patient.com" : dto.Email.Trim();

            // STRICT ANTI-OVERWRITE RULE: Check if SĐT, Email, or Username is already linked to another User
            User? existingUser = null;
            if (!string.IsNullOrEmpty(phoneNum))
            {
                existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNum);
            }
            if (existingUser == null && !string.IsNullOrEmpty(email))
            {
                existingUser = await _userManager.FindByEmailAsync(email);
            }
            if (existingUser == null && !string.IsNullOrEmpty(uname))
            {
                existingUser = await _userManager.FindByNameAsync(uname);
            }

            if (existingUser != null)
            {
                string existingName = !string.IsNullOrEmpty(existingUser.FullName) ? existingUser.FullName : existingUser.UserName ?? "Chưa đặt tên";
                return new PatientOperationResult
                {
                    Success = false,
                    IsConflict = true,
                    ConflictUserId = existingUser.Id,
                    ConflictUserName = existingName,
                    Message = $"SĐT/Email này đã gắn với hồ sơ bệnh nhân khác (Mã: {existingUser.Id}, Tên: {existingName}). Vui lòng kiểm tra lại hoặc liên hệ Admin nếu đây là cùng 1 người cần gộp hồ sơ thủ công."
                };
            }

            var targetUser = new User
            {
                UserName = uname,
                FullName = dto.Name ?? uname,
                Email = email,
                PhoneNumber = phoneNum,
                Gender = dto.Gender ?? "Nam",
                DateOfBirth = dto.DateOfBirth ?? DateTime.UtcNow.AddYears(-25),
                Address = dto.Address ?? "",
                MedicalHistory = dto.MedicalHistory,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            string pwd = string.IsNullOrEmpty(dto.Password) ? "Patient@123" : dto.Password;
            var result = await _userManager.CreateAsync(targetUser, pwd);
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return new PatientOperationResult
                {
                    Success = false,
                    IsConflict = false,
                    Message = $"Tạo tài khoản thất bại: {errors}"
                };
            }

            try
            {
                await _userManager.AddToRoleAsync(targetUser, "User");
            }
            catch { }

            // Ensure an appointment record exists for this patient
            bool hasAppointment = await _context.Appointments.AnyAsync(a => a.UserId == targetUser.Id);
            if (!hasAppointment)
            {
                var appointment = new Appointment
                {
                    UserId = targetUser.Id,
                    AppointmentDate = DateTime.Now,
                    Reason = "Đăng ký khám bệnh trực tiếp tại nhà thuốc",
                    Status = "Confirmed",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();
            }

            return new PatientOperationResult
            {
                Success = true,
                IsConflict = false,
                Message = "Tạo hồ sơ bệnh nhân thành công."
            };
        }

        public async Task<PatientOperationResult> UpdatePatientAsync(int id, UpdatePatientDto dto)
        {
            var patient = await _userManager.FindByIdAsync(id.ToString());

            if (patient == null)
            {
                return new PatientOperationResult
                {
                    Success = false,
                    IsConflict = false,
                    Message = "Không tìm thấy hồ sơ bệnh nhân."
                };
            }

            string phoneNum = (dto.PhoneNumber ?? dto.Phone ?? "").Trim();
            if (!string.IsNullOrEmpty(phoneNum) && phoneNum != patient.PhoneNumber)
            {
                var existingPhoneUser = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNum && u.Id != id);
                if (existingPhoneUser != null)
                {
                    string existingName = !string.IsNullOrEmpty(existingPhoneUser.FullName) ? existingPhoneUser.FullName : existingPhoneUser.UserName ?? "Chưa đặt tên";
                    return new PatientOperationResult
                    {
                        Success = false,
                        IsConflict = true,
                        ConflictUserId = existingPhoneUser.Id,
                        ConflictUserName = existingName,
                        Message = $"SĐT/Email này đã gắn với hồ sơ bệnh nhân khác (Mã: {existingPhoneUser.Id}, Tên: {existingName}). Vui lòng kiểm tra lại hoặc liên hệ Admin nếu đây là cùng 1 người cần gộp hồ sơ thủ công."
                    };
                }
                patient.PhoneNumber = phoneNum;
            }

            string email = (dto.Email ?? "").Trim();
            if (!string.IsNullOrEmpty(email) && email != patient.Email)
            {
                var existingEmailUser = await _userManager.FindByEmailAsync(email);
                if (existingEmailUser != null && existingEmailUser.Id != id)
                {
                    string existingName = !string.IsNullOrEmpty(existingEmailUser.FullName) ? existingEmailUser.FullName : existingEmailUser.UserName ?? "Chưa đặt tên";
                    return new PatientOperationResult
                    {
                        Success = false,
                        IsConflict = true,
                        ConflictUserId = existingEmailUser.Id,
                        ConflictUserName = existingName,
                        Message = $"SĐT/Email này đã gắn với hồ sơ bệnh nhân khác (Mã: {existingEmailUser.Id}, Tên: {existingName}). Vui lòng kiểm tra lại hoặc liên hệ Admin nếu đây là cùng 1 người cần gộp hồ sơ thủ công."
                    };
                }
                patient.Email = email;
            }

            if (!string.IsNullOrEmpty(dto.Username)) patient.UserName = dto.Username;
            if (!string.IsNullOrEmpty(dto.Name)) patient.FullName = dto.Name;
            if (!string.IsNullOrEmpty(dto.Gender)) patient.Gender = dto.Gender;
            if (dto.DateOfBirth.HasValue) patient.DateOfBirth = dto.DateOfBirth.Value;
            if (dto.Address != null) patient.Address = dto.Address;
            if (dto.MedicalHistory != null) patient.MedicalHistory = dto.MedicalHistory;
            patient.IsActive = dto.IsActive;

            var result = await _userManager.UpdateAsync(patient);
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return new PatientOperationResult
                {
                    Success = false,
                    IsConflict = false,
                    Message = $"Cập nhật thất bại: {errors}"
                };
            }

            return new PatientOperationResult
            {
                Success = true,
                IsConflict = false,
                Message = "Cập nhật thông tin bệnh nhân thành công."
            };
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

            var rolesByUser = await GetRolesByUserIdAsync();
            var patients = new List<PatientDto>();

            foreach (var user in users)
            {
                var roles = rolesByUser.TryGetValue(user.Id, out var userRoles) ? userRoles : new List<string>();

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
                        MedicalHistory = user.MedicalHistory,
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
