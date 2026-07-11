using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/patient-address")]
    [Authorize(Roles = "Admin,Patient")]
    public class PatientAddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public PatientAddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        // GET: api/patient-address/user/1
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetAddresses(int userId)
        {
            var result = await _addressService.GetAddressesAsync(userId);
            return Ok(result);
        }

        // POST: api/patient-address/user/1
        [HttpPost("user/{userId}")]
        public async Task<IActionResult> AddAddress(int userId, AddressDto dto)
        {
            var result = await _addressService.AddAddressAsync(userId, dto);

            if (!result)
                return BadRequest();

            return Ok(new { message = "Address added successfully." });
        }

        // PUT: api/patient-address/5
        [HttpPut("{addressId}")]
        public async Task<IActionResult> UpdateAddress(int addressId, AddressDto dto)
        {
            var result = await _addressService.UpdateAddressAsync(addressId, dto);

            if (!result)
                return NotFound();

            return Ok(new { message = "Address updated successfully." });
        }

        // DELETE: api/patient-address/5
        [HttpDelete("{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var result = await _addressService.DeleteAddressAsync(addressId);

            if (!result)
                return NotFound();

            return Ok(new { message = "Address deleted successfully." });
        }
    }
}
