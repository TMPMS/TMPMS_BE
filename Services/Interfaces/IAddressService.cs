using BusinessObjects;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IAddressService
    {
        Task<List<UserAddress>> GetAddressesAsync(int userId);

        Task<UserAddress?> GetByIdAsync(int addressId);

        Task<bool> AddAddressAsync(int userId, AddressDto dto);

        Task<bool> UpdateAddressAsync(int addressId, AddressDto dto);

        Task<bool> DeleteAddressAsync(int addressId);

        Task<bool> SetDefaultAsync(int userId, int addressId);
    }
}
