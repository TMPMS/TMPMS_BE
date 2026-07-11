using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IPatientService
    {
        Task<List<PatientDto>> GetAllPatientsAsync();
        Task<bool> AddPatientAsync(PatientCreateDTO dto);
        Task<bool> UpdatePatientAsync(int id, UpdatePatientDto dto);
        Task<bool> DeletePatientAsync(int id);
        Task<List<PatientDto>> SearchPatientsAsync(string keyword);
        Task<PatientDetailDto?> GetPatientDetailAsync(int id);
        Task<List<DiagnosisDto>> GetDiagnosisHistoryAsync(int patientId);

        Task<List<PrescriptionDTO>> GetPrescriptionHistoryAsync(int patientId);
    }
}
