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
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<UserAddress?> GetByIdAsync(int addressId)
        {
            return await _context.UserAddresses.FindAsync(addressId);
        }

        private async Task UnsetOtherDefaultsAsync(int userId, int? exceptAddressId = null)
        {
            await _context.UserAddresses
                .Where(a => a.UserId == userId && a.IsDefault && (exceptAddressId == null || a.Id != exceptAddressId))
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
        }

        public async Task<bool> AddAddressAsync(int userId, AddressDto dto)
        {
            // Địa chỉ đầu tiên của user luôn tự động là mặc định, để không bao giờ có
            // trạng thái "0 địa chỉ mặc định".
            var isFirstAddress = !await _context.UserAddresses.AnyAsync(a => a.UserId == userId);
            var isDefault = dto.IsDefault || isFirstAddress;

            if (isDefault)
            {
                await UnsetOtherDefaultsAsync(userId);
            }

            var address = new UserAddress
            {
                UserId = userId,
                AddressLine = dto.AddressLine,
                City = dto.City,
                District = dto.District,
                Ward = dto.Ward,
                IsDefault = isDefault
            };

            _context.UserAddresses.Add(address);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAddressAsync(int addressId, AddressDto dto)
        {
            var address = await _context.UserAddresses.FindAsync(addressId);

            if (address == null)
                return false;

            if (dto.IsDefault && !address.IsDefault)
            {
                await UnsetOtherDefaultsAsync(address.UserId, addressId);
            }

            address.AddressLine = dto.AddressLine;
            address.City = dto.City;
            address.District = dto.District;
            address.Ward = dto.Ward;
            address.IsDefault = dto.IsDefault || address.IsDefault;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAddressAsync(int addressId)
        {
            var address = await _context.UserAddresses.FindAsync(addressId);

            if (address == null)
                return false;

            var userId = address.UserId;
            var wasDefault = address.IsDefault;

            _context.UserAddresses.Remove(address);
            var deleted = await _context.SaveChangesAsync() > 0;

            if (deleted && wasDefault)
            {
                // Sau khi xoá địa chỉ mặc định, tự động thăng địa chỉ gần nhất còn lại làm mặc định
                // để user luôn có 1 địa chỉ mặc định (nếu còn địa chỉ nào).
                var next = await _context.UserAddresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefaultAsync();
                if (next != null)
                {
                    next.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            return deleted;
        }

        public async Task<bool> SetDefaultAsync(int userId, int addressId)
        {
            var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
            if (address == null)
                return false;

            await UnsetOtherDefaultsAsync(userId, addressId);
            address.IsDefault = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
