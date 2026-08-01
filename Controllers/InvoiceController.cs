using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _service;
        public InvoiceController(IInvoiceService service) => _service = service;

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("id")?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }

        private bool IsAdminOrStaff()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Pharmacy");
        }

        [HttpPost("generate/{orderId}")]
        [Authorize]
        public async Task<ActionResult> Generate(int orderId)
        {
            try
            {
                int userId = GetUserId();
                bool isAdminOrStaff = IsAdminOrStaff();
                var result = await _service.GenerateInvoice(orderId, userId, isAdminOrStaff);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<ActionResult> GetByOrderId(int orderId)
        {
            try
            {
                int userId = GetUserId();
                bool isAdminOrStaff = IsAdminOrStaff();
                var result = await _service.GetByOrderId(orderId, userId, isAdminOrStaff);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
