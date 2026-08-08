using BusinessObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _repo;
        public SupplierService(ISupplierRepository repo) => _repo = repo;

        public async Task<List<SupplierDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            var counts = await _repo.GetProductCountsAsync();
            return list.Select(s => new SupplierDto
            {
                Id = s.Id,
                CompanyName = s.CompanyName,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                TaxCode = s.TaxCode,
                Status = s.Status ?? "Active",
                ProductCount = counts.TryGetValue(s.Id, out var count) ? count : 0
            }).ToList();
        }

        public async Task<SupplierDto?> GetByIdAsync(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return null;
            return new SupplierDto
            {
                Id = s.Id,
                CompanyName = s.CompanyName,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                TaxCode = s.TaxCode,
                Status = s.Status ?? "Active"
            };
        }

        public async Task<SupplierDto> CreateAsync(SupplierCreateDto dto)
        {
            var entity = new Supplier
            {
                CompanyName = dto.CompanyName,
                ContactPerson = dto.ContactPerson,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                TaxCode = dto.TaxCode,
                Status = dto.Status ?? "Active"
            };
            var created = await _repo.CreateAsync(entity);
            return (await GetByIdAsync(created.Id))!;
        }

        public async Task<SupplierDto?> UpdateAsync(int id, SupplierCreateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.CompanyName = dto.CompanyName;
            existing.ContactPerson = dto.ContactPerson;
            existing.Email = dto.Email;
            existing.Phone = dto.Phone;
            existing.Address = dto.Address;
            existing.TaxCode = dto.TaxCode;
            existing.Status = dto.Status ?? existing.Status;

            await _repo.UpdateAsync(existing);
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);
    }
}
