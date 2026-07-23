using BusinessObjects;

namespace TMPMS.Repositories.Interfaces
{
    public interface IPrescriptionItemRepository
    {
        Task<List<PrescriptionItem>> GetPrescriptionItemsByPrescriptionIdAsync(int prescriptionId);
    }
}
