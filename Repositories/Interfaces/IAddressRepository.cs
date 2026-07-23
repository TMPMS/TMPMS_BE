using BusinessObjects;
using TMPMS.DTOs;

namespace TMPMS.Repositories.Interfaces
{
    public interface IAddressRepository
    {
        Task<List<UserAddress>> GetByUserIdAsync(int userId);

        Task<bool> AddAddressAsync(int userId, AddressDto dto);

        Task<bool> UpdateAddressAsync(int addressId, AddressDto dto);

        Task<bool> DeleteAddressAsync(int addressId);
    }
}
