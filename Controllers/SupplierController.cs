using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _service;
        public SupplierController(ISupplierService service) => _service = service;

        [HttpGet("~/suppliers")]
        [HttpGet("~/api/suppliers")]
        [HttpGet("api/[controller]")]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("~/suppliers/{id}")]
        [HttpGet("~/api/suppliers/{id}")]
        [HttpGet("api/[controller]/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _service.GetByIdAsync(id);
            if (res == null) return NotFound();
            return Ok(res);
        }

        [HttpPost("~/Supplier")]
        [HttpPost("api/[controller]")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create([FromBody] SupplierCreateDto dto)
        {
            var res = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpPut("~/Supplier/{id}")]
        [HttpPut("api/[controller]/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] SupplierCreateDto dto)
        {
            var res = await _service.UpdateAsync(id, dto);
            if (res == null) return NotFound();
            return Ok(res);
        }

        [HttpDelete("~/Supplier/{id}")]
        [HttpDelete("api/[controller]/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
