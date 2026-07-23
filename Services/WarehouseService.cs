using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _repo;
        public WarehouseService(IWarehouseRepository repo) => _repo = repo;

        public async Task<WarehouseResponseDTO> Create(WarehouseCreateDTO dto)
        {
            var entity = new Warehouse { Name = dto.Name, Address = dto.Address };
            var created = await _repo.Create(entity);
            return Map(created);
        }

        public async Task<WarehouseResponseDTO> GetById(int id)
        {
            var entity = await _repo.GetById(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<List<WarehouseResponseDTO>> GetAll()
        {
            var list = await _repo.GetAll();
            return list.Select(Map).ToList();
        }

        public async Task<WarehouseResponseDTO> Update(int id, WarehouseUpdateDTO dto)
        {
            var entity = await _repo.GetById(id);
            if (entity == null) return null;
            entity.Name = dto.Name ?? entity.Name;
            entity.Address = dto.Address ?? entity.Address;
            var updated = await _repo.Update(entity);
            return Map(updated);
        }

        public async Task<bool> Delete(int id) => await _repo.Delete(id);

        private WarehouseResponseDTO Map(Warehouse w)
        {
            return new WarehouseResponseDTO
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                TotalMedicineTypes = w.InventoryStocks?.Select(s => s.MedicineId).Distinct().Count() ?? 0,
                TotalStockQuantity = w.InventoryStocks?.Sum(s => s.Quantity) ?? 0
            };
        }
    }
}
