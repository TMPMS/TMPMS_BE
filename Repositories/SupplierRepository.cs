using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly TMPMSDbContext _context;
        public SupplierRepository(TMPMSDbContext context) => _context = context;

        public async Task<List<Supplier>> GetAllAsync() => await _context.Suppliers.ToListAsync();
        public async Task<Supplier?> GetByIdAsync(int id) => await _context.Suppliers.FindAsync(id);
        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }
        public async Task<Supplier?> UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var sup = await _context.Suppliers.FindAsync(id);
            if (sup == null) return false;
            _context.Suppliers.Remove(sup);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<int, int>> GetProductCountsAsync()
        {
            return await _context.Medicines
                .Where(m => m.IsActive)
                .GroupBy(m => m.SupplierId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }
    }
}
