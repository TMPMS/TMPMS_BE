using BusinessObjects;
using TMPMS.DTOs;

namespace Repositories.Interfaces
{
    public interface IDiagnosisRepository
    {
        Task<Diagnosis> Create(Diagnosis diagnosis);
        Task<Diagnosis> GetById(int id);
        Task<List<Diagnosis>> GetByPatient(int patientId);
        Task<List<Diagnosis>> GetByDoctor(int doctorId);
        Task<List<Diagnosis>> GetAll();
        Task<Diagnosis> Update(Diagnosis diagnosis);
        Task<bool> Delete(int id);
        Task<List<Diagnosis>> GetByPatientIdAsync(int patientId);
        Task<List<DiagnosisDTOs>> GetDiagnosisHistoryAsync(int patientId);
    }
}

