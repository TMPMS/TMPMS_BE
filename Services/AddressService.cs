using BusinessObjects;
using TMPMS.DTOs;
using TMPMS.Repositories;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class AddressService : IAddressService
    {

        private readonly IAddressRepository _addressRepository;

        public AddressService(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }
        public async Task<List<UserAddress>> GetAddressesAsync(int userId)
        {
            return await _addressRepository.GetByUserIdAsync(userId);
        }

        public async Task<UserAddress?> GetByIdAsync(int addressId)
        {
            return await _addressRepository.GetByIdAsync(addressId);
        }

        public async Task<bool> AddAddressAsync(int userId, AddressDto dto)
        {
            return await _addressRepository.AddAddressAsync(userId, dto);
        }

        public async Task<bool> UpdateAddressAsync(int addressId, AddressDto dto)
        {
            return await _addressRepository.UpdateAddressAsync(addressId, dto);
        }

        public async Task<bool> DeleteAddressAsync(int addressId)
        {
            return await _addressRepository.DeleteAddressAsync(addressId);
        }

        public async Task<bool> SetDefaultAsync(int userId, int addressId)
        {
            return await _addressRepository.SetDefaultAsync(userId, addressId);
        }
    }
}
