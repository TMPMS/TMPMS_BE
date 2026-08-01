using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IHealthQuizService
    {
        Task<List<HealthQuizListDTO>> GetActiveQuizzesAsync();
        Task<HealthQuizDetailDTO?> GetQuizByCodeAsync(string code);
        Task<QuizSubmitResponseDTO> SubmitQuizAsync(string code, QuizSubmitRequestDTO dto, int? userId);
    }
}
