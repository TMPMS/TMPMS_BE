using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class DiagnosisService : IDiagnosisService
    {
        private readonly IDiagnosisRepository _repo;
        public DiagnosisService(IDiagnosisRepository repo) => _repo = repo;

        public async Task<DiagnosisResponseDTO> Create(DiagnosisCreateDTO dto)
        {
            var entity = new Diagnosis
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId > 0 ? dto.DoctorId : 1,
                Symptoms = dto.Symptoms,
                ClinicalExamination = dto.ClinicalExamination,
                DiagnosisResult = dto.DiagnosisResult,
                Note = dto.Note,
                DiagnosisDate = dto.DiagnosisDate == default ? DateTime.Now : dto.DiagnosisDate,
                CreatedAt = DateTime.Now
            };
            var created = await _repo.Create(entity);
            var full = await _repo.GetById(created.Id);
            return Map(full);
        }

        public async Task<DiagnosisResponseDTO> GetById(int id)
        {
            var entity = await _repo.GetById(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<List<DiagnosisResponseDTO>> GetByPatient(int patientId)
        {
            var list = await _repo.GetByPatient(patientId);
            return list.Select(Map).ToList();
        }

        public async Task<List<DiagnosisResponseDTO>> GetByDoctor(int doctorId)
        {
            var list = await _repo.GetByDoctor(doctorId);
            return list.Select(Map).ToList();
        }

        public async Task<List<DiagnosisResponseDTO>> GetAll()
        {
            var list = await _repo.GetAll();
            return list.Select(Map).ToList();
        }

        public async Task<DiagnosisResponseDTO> Update(int id, DiagnosisUpdateDTO dto)
        {
            var entity = await _repo.GetById(id);
            if (entity == null) return null;

            entity.Symptoms = dto.Symptoms ?? entity.Symptoms;
            entity.ClinicalExamination = dto.ClinicalExamination ?? entity.ClinicalExamination;
            entity.DiagnosisResult = dto.DiagnosisResult ?? entity.DiagnosisResult;
            entity.Note = dto.Note ?? entity.Note;

            var updated = await _repo.Update(entity);
            return Map(updated);
        }

        public async Task<bool> Delete(int id) => await _repo.Delete(id);

        private DiagnosisResponseDTO Map(Diagnosis d)
        {
            return new DiagnosisResponseDTO
            {
                Id = d.Id,
                PatientId = d.PatientId,
                PatientName = d.Patient?.UserName,
                DoctorId = d.DoctorId,
                DoctorName = d.Doctor?.UserName,
                Symptoms = d.Symptoms,
                ClinicalExamination = d.ClinicalExamination,
                DiagnosisResult = d.DiagnosisResult,
                Note = d.Note,
                DiagnosisDate = d.DiagnosisDate,
                CreatedAt = d.CreatedAt,
                PrescriptionCount = d.Prescriptions?.Count ?? 0
            };
        }
    }
}
