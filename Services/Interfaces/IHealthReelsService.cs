using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IHealthReelsService
    {
        Task<HealthReelsResponseDto> GetHealthReelsAsync();
    }
}
