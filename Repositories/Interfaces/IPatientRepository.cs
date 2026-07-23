using BusinessObjects;
using TMPMS.DTOs;

namespace TMPMS.Repositories.Interfaces
{
    public interface IPatientRepository
    {
        Task<List<PatientDto>> GetAllPatientsAsync();
        Task<bool> AddPatientAsync(PatientCreateDTO dto);
        Task<bool> UpdatePatientAsync(int id, UpdatePatientDto dto);
        Task<bool> DeletePatientAsync(int id);
        Task<List<PatientDto>> SearchPatientsAsync(string keyword);
        Task<User?> GetByIdAsync(int id);
    }
}
