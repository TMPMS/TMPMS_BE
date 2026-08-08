using System.Threading.Tasks;
using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IMeridianAnalysisService
    {
        Task<MeridianAnalysisResponseDto> GetAsync(int medicineId);
    }
}
