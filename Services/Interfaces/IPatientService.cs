using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IPatientService
    {
        Task<List<PatientDto>> GetAllPatientsAsync();
        Task<PatientOperationResult> AddPatientAsync(PatientCreateDTO dto);
        Task<PatientOperationResult> UpdatePatientAsync(int id, UpdatePatientDto dto);
        Task<bool> DeletePatientAsync(int id);
        Task<List<PatientDto>> SearchPatientsAsync(string keyword);
        Task<PatientDetailDto?> GetPatientDetailAsync(int id);
        Task<List<DiagnosisDTOs>> GetDiagnosisHistoryAsync(int patientId);

        Task<List<PrescriptionResponseDTO>> GetPrescriptionHistoryAsync(int patientId);
    }
}
