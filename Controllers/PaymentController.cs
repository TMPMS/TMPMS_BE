using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _service;
        public PaymentController(IPaymentService service) => _service = service;

        [HttpPost]
        public async Task<ActionResult> CreatePayment([FromBody] PaymentCreateDTO dto)
        {
            try
            {
                var result = await _service.CreatePayment(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var result = await _service.GetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult> GetByOrder(int orderId) => Ok(await _service.GetByOrder(orderId));

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Accountant,Pharmacy")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] PaymentUpdateStatusDTO dto)
        {
            try
            {
                var result = await _service.UpdateStatus(id, dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
