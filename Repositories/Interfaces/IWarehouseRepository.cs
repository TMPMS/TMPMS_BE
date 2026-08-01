using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IWarehouseRepository
    {
        Task<Warehouse> Create(Warehouse warehouse);
        Task<Warehouse> GetById(int id);
        Task<List<Warehouse>> GetAll();
        Task<Warehouse> Update(Warehouse warehouse);
        Task<bool> Delete(int id);
    }
}
