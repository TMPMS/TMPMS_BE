using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly TMPMSDbContext _context;
        public WarehouseRepository(TMPMSDbContext context) => _context = context;

        public async Task<Warehouse> Create(Warehouse warehouse)
        {
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();
            return warehouse;
        }

        public async Task<Warehouse> GetById(int id)
        {
            return await _context.Warehouses
                .Include(w => w.InventoryStocks).ThenInclude(s => s.Medicine)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<List<Warehouse>> GetAll()
        {
            return await _context.Warehouses
                .Include(w => w.InventoryStocks)
                .ToListAsync();
        }

        public async Task<Warehouse> Update(Warehouse warehouse)
        {
            _context.Warehouses.Update(warehouse);
            await _context.SaveChangesAsync();
            return warehouse;
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _context.Warehouses.FindAsync(id);
            if (entity == null) return false;
            _context.Warehouses.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
