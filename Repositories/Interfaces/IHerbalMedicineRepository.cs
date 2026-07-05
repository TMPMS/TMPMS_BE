using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IHerbalMedicineRepository
    {
        Task<Medicine> CreateMedicine(Medicine medicine);
        Task<HerbalMedicineInfo> CreateHerbalInfo(HerbalMedicineInfo info);
        Task<HerbalMedicineInfo> GetHerbalInfoByMedicineId(int medicineId);
        Task<Medicine> GetMedicineById(int medicineId);
        Task<List<HerbalMedicineInfo>> GetAllHerbal();
        Task<HerbalMedicineInfo> Update(HerbalMedicineInfo info);
        Task<bool> Delete(int medicineId);
    }
}
