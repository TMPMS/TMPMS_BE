using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Repositories.Interfaces
{
    public interface ISupplierRepository
    {
        Task<List<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(int id);
        Task<Supplier> CreateAsync(Supplier supplier);
        Task<Supplier?> UpdateAsync(Supplier supplier);
        Task<bool> DeleteAsync(int id);
        Task<Dictionary<int, int>> GetProductCountsAsync();
    }
}
