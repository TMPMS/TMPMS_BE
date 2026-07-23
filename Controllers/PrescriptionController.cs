using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PrescriptionController : ControllerBase
    {
        private readonly IPrescriptionService _service;
        public PrescriptionController(IPrescriptionService service) => _service = service;

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] PrescriptionCreateDTO dto)
        {
            try
            {
                var result = await _service.Create(dto);
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

        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetByUser(int userId) => Ok(await _service.GetByUser(userId));

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Pharmacist,Admin")]
        public async Task<ActionResult> GetByStatus(string status) => Ok(await _service.GetByStatus(status));

        [HttpGet]
        [Authorize(Roles = "Pharmacist,Admin")]
        public async Task<ActionResult> GetAll() => Ok(await _service.GetAll());

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Pharmacist,Admin")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] PrescriptionStatusUpdateDTO dto)
        {
            try
            {
                var result = await _service.UpdateStatus(id, dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await _service.Delete(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
