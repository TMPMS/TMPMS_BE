using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/patient-address")]
    [Route("patient-address")]
    [Authorize]
    public class PatientAddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public PatientAddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        private bool IsSelfOrAdmin(int ownerUserId)
        {
            if (User.IsInRole("Admin")) return true;
            var currentUserId = GetUserId();
            return currentUserId != null && currentUserId.Value == ownerUserId;
        }

        // GET: api/patient-address/user/1
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetAddresses(int userId)
        {
            if (!IsSelfOrAdmin(userId)) return Forbid();
            var result = await _addressService.GetAddressesAsync(userId);
            return Ok(result);
        }

        // POST: api/patient-address/user/1
        [HttpPost("user/{userId}")]
        public async Task<IActionResult> AddAddress(int userId, AddressDto dto)
        {
            if (!IsSelfOrAdmin(userId)) return Forbid();

            var result = await _addressService.AddAddressAsync(userId, dto);

            if (!result)
                return BadRequest();

            return Ok(new { message = "Address added successfully." });
        }

        // PUT: api/patient-address/5
        [HttpPut("{addressId}")]
        public async Task<IActionResult> UpdateAddress(int addressId, AddressDto dto)
        {
            var existing = await _addressService.GetByIdAsync(addressId);
            if (existing == null) return NotFound();
            if (!IsSelfOrAdmin(existing.UserId)) return Forbid();

            var result = await _addressService.UpdateAddressAsync(addressId, dto);

            if (!result)
                return NotFound();

            return Ok(new { message = "Address updated successfully." });
        }

        // PUT: api/patient-address/5/set-default
        [HttpPut("{addressId}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var existing = await _addressService.GetByIdAsync(addressId);
            if (existing == null) return NotFound();
            if (!IsSelfOrAdmin(existing.UserId)) return Forbid();

            var result = await _addressService.SetDefaultAsync(existing.UserId, addressId);
            if (!result)
                return NotFound();

            return Ok(new { message = "Default address updated successfully." });
        }

        // DELETE: api/patient-address/5
        [HttpDelete("{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var existing = await _addressService.GetByIdAsync(addressId);
            if (existing == null) return NotFound();
            if (!IsSelfOrAdmin(existing.UserId)) return Forbid();

            var result = await _addressService.DeleteAddressAsync(addressId);

            if (!result)
                return NotFound();

            return Ok(new { message = "Address deleted successfully." });
        }
    }
}
