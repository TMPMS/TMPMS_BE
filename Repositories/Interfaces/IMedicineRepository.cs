using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Repositories.Interfaces
{
    public interface IMedicineRepository
    {
        Task<(List<Medicine> Items, int TotalCount)> SearchAsync(MedicineSearchFilterDto filter);
        Task<Dictionary<int, (double AvgRating, int Count)>> GetReviewStatsAsync(List<int> medicineIds);
        Task<(List<string> PartUsed, List<string> Effects)> GetHerbalFilterOptionsAsync();
        Task<Medicine?> GetByIdAsync(int id);
        Task<Medicine?> GetByBarcodeAsync(string barcode);
        Task<Medicine> CreateAsync(Medicine medicine);
        Task SaveChangesAsync();
        Task<bool> HasLinksAsync(int medicineId);
        Task DeactivateAsync(Medicine medicine);
        Task DeleteAsync(Medicine medicine);
    }
}
