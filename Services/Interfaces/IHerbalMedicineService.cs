using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IHerbalMedicineService
    {
        Task<HerbalMedicineResponseDTO> Create(HerbalMedicineCreateDTO dto);
        Task<HerbalMedicineResponseDTO> GetByMedicineId(int medicineId);
        Task<List<HerbalMedicineResponseDTO>> GetAll();
        Task<HerbalMedicineResponseDTO> Update(int medicineId, HerbalMedicineUpdateDTO dto);
        Task<bool> Delete(int medicineId);
    }
}
