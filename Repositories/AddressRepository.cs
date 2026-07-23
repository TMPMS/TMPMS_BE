using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly TMPMSDbContext _context;

        public AddressRepository(TMPMSDbContext context)
        {
            _context = context;
        }
        public async Task<List<UserAddress>> GetByUserIdAsync(int userId)
        {

      
            return await _context.UserAddresses
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> AddAddressAsync(int userId, AddressDto dto)
        {
            var address = new UserAddress
            {
                UserId = userId,
                AddressLine = dto.AddressLine,
                City = dto.City,
                District = dto.District,
                Ward = dto.Ward,
                IsDefault = dto.IsDefault
            };

            _context.UserAddresses.Add(address);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAddressAsync(int addressId, AddressDto dto)
        {
            var address = await _context.UserAddresses.FindAsync(addressId);

            if (address == null)
                return false;

            address.AddressLine = dto.AddressLine;
            address.City = dto.City;
            address.District = dto.District;
            address.Ward = dto.Ward;
            address.IsDefault = dto.IsDefault;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAddressAsync(int addressId)
        {
            var address = await _context.UserAddresses.FindAsync(addressId);

            if (address == null)
                return false;

            _context.UserAddresses.Remove(address);

            return await _context.SaveChangesAsync() > 0;
        }
    }
}
