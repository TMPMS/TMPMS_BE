using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _service;
        public InvoiceController(IInvoiceService service) => _service = service;

        [HttpPost("generate/{orderId}")]
        [Authorize(Roles = "Admin,Accountant")]
        public async Task<ActionResult> Generate(int orderId)
        {
            try
            {
                var result = await _service.GenerateInvoice(orderId);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult> GetByOrderId(int orderId)
        {
            var result = await _service.GetByOrderId(orderId);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}
