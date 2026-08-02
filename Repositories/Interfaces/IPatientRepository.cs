using BusinessObjects;
using TMPMS.DTOs;

namespace TMPMS.Repositories.Interfaces
{
    public interface IPatientRepository
    {
        Task<List<PatientDto>> GetAllPatientsAsync();
        Task<PatientOperationResult> AddPatientAsync(PatientCreateDTO dto);
        Task<PatientOperationResult> UpdatePatientAsync(int id, UpdatePatientDto dto);
        Task<bool> DeletePatientAsync(int id);
        Task<List<PatientDto>> SearchPatientsAsync(string keyword);
        Task<User?> GetByIdAsync(int id);
    }
}
