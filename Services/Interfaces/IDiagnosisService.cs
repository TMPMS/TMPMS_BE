using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IDiagnosisService
    {
        Task<DiagnosisResponseDTO> Create(DiagnosisCreateDTO dto);
        Task<DiagnosisResponseDTO> GetById(int id);
        Task<List<DiagnosisResponseDTO>> GetByPatient(int patientId);
        Task<List<DiagnosisResponseDTO>> GetByDoctor(int doctorId);
        Task<List<DiagnosisResponseDTO>> GetAll();
        Task<DiagnosisResponseDTO> Update(int id, DiagnosisUpdateDTO dto);
        Task<bool> Delete(int id);

        Task<List<SymptomQuestionDTO>> GetQuestionsAsync();
        Task<DiagnosisResultDTO> ClassifyAsync(DiagnosisClassifyRequestDTO dto, int? currentUserId);
        Task<NextQuestionResponseDTO> GetNextQuestionAsync(List<AnswerSubmissionDTO> answeredSoFar);
    }
}
