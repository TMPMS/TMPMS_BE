using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<WarehouseResponseDTO> Create(WarehouseCreateDTO dto);
        Task<WarehouseResponseDTO> GetById(int id);
        Task<List<WarehouseResponseDTO>> GetAll();
        Task<WarehouseResponseDTO> Update(int id, WarehouseUpdateDTO dto);
        Task<bool> Delete(int id);
    }
}
