using System.Threading.Tasks;
using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface ITongueAnalysisService
    {
        Task<TongueAnalysisResponseDto> AnalyzeAsync(byte[] imageBytes, string mimeType);
    }
}
