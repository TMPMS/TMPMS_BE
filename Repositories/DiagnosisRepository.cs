using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;
using TMPMS.DTOs;

namespace TMPMS.Repositories
{
    public class DiagnosisRepository : IDiagnosisRepository
    {
        private readonly TMPMSDbContext _context;
        public DiagnosisRepository(TMPMSDbContext context) => _context = context;

        public async Task<Diagnosis> Create(Diagnosis diagnosis)
        {
            _context.Diagnoses.Add(diagnosis);
            await _context.SaveChangesAsync();
            return diagnosis;
        }

        public async Task<Diagnosis> GetById(int id)
        {
            return await _context.Diagnoses
                .Include(d => d.Patient)
                .Include(d => d.Doctor)
                .Include(d => d.Prescriptions)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Diagnosis>> GetByPatient(int patientId)
        {
            return await _context.Diagnoses
                .Include(d => d.Doctor)
                .Include(d => d.Prescriptions)
                .Where(d => d.PatientId == patientId)
                .OrderByDescending(d => d.DiagnosisDate)
                .ToListAsync();
        }

        public async Task<List<Diagnosis>> GetByDoctor(int doctorId)
        {
            return await _context.Diagnoses
                .Include(d => d.Patient)
                .Include(d => d.Prescriptions)
                .Where(d => d.DoctorId == doctorId)
                .OrderByDescending(d => d.DiagnosisDate)
                .ToListAsync();
        }

        public async Task<List<Diagnosis>> GetAll()
        {
            return await _context.Diagnoses
                .Include(d => d.Patient)
                .Include(d => d.Doctor)
                .Include(d => d.Prescriptions)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Diagnosis> Update(Diagnosis diagnosis)
        {
            _context.Diagnoses.Update(diagnosis);
            await _context.SaveChangesAsync();
            return diagnosis;
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _context.Diagnoses.FindAsync(id);
            if (entity == null) return false;
            _context.Diagnoses.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Diagnosis>> GetByPatientIdAsync(int patientId)
        {
            return await _context.Diagnoses
                .Where(d => d.PatientId == patientId)
                .ToListAsync();
        }

        public async Task<List<DiagnosisDTOs>> GetDiagnosisHistoryAsync(int patientId)
        {
            return await _context.Diagnoses
                .Where(d => d.PatientId == patientId)
                .Select(d => new DiagnosisDTOs
                {
                    Id = d.Id,
                    PatientId = d.PatientId,
                    PatientName = d.Patient.UserName ?? "",      // đổi theo entity của bạn
                    DoctorId = d.DoctorId,
                    DoctorName = d.Doctor.UserName ?? "",        // đổi theo entity của bạn
                    Symptoms = d.Symptoms,
                    ClinicalExamination = d.ClinicalExamination,
                    DiagnosisResult = d.DiagnosisResult,
                    Note = d.Note,
                    DiagnosisDate = d.DiagnosisDate,
                    CreatedAt = d.CreatedAt,
                    PrescriptionCount = d.Prescriptions.Count()
                })
                .ToListAsync();
        }
        public async Task<List<SymptomQuestion>> GetQuestionsWithAnswersAsync()
        {
            return await _context.SymptomQuestions
                .Include(q => q.AnswerOptions)
                .OrderBy(q => q.QuestionOrder)
                .ToListAsync();
        }

        public async Task<List<SyndromeType>> GetSyndromeTypesAsync()
        {
            return await _context.SyndromeTypes.ToListAsync();
        }

        public async Task<List<AnswerScoreMapping>> GetScoreMappingsAsync()
        {
            return await _context.AnswerScoreMappings.ToListAsync();
        }
    }
}
