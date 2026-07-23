using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IPrescriptionService
    {
        Task<PrescriptionResponseDTO> Create(PrescriptionCreateDTO dto);
        Task<PrescriptionResponseDTO> GetById(int id);
        Task<List<PrescriptionResponseDTO>> GetByUser(int userId);
        Task<List<PrescriptionResponseDTO>> GetByStatus(string status);
        Task<List<PrescriptionResponseDTO>> GetAll();
        Task<PrescriptionResponseDTO> UpdateStatus(int id, PrescriptionStatusUpdateDTO dto);
        Task<bool> Delete(int id);
    }
}
