using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IHerbalInteractionService
    {
        Task<SafetyCheckResponseDTO> CheckSafety(SafetyCheckRequestDTO dto);
    }
}
