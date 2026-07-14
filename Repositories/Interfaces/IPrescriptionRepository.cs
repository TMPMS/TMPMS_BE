using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IPrescriptionRepository
    {
        Task<Prescription> Create(Prescription prescription);
        Task<Prescription> GetById(int id);
        Task<List<Prescription>> GetByUser(int userId);
        Task<List<Prescription>> GetByStatus(string status);
        Task<List<Prescription>> GetAll();
        Task<Prescription> Update(Prescription prescription);
        Task<bool> Delete(int id);
        Task<Medicine> GetMedicineById(int medicineId);
        Task<List<Prescription>> GetPrescriptionsByPatientIdAsync(int userId);
    }
}
