using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _service;
        public WarehouseController(IWarehouseService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult> GetAll() => Ok(await _service.GetAll());

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var result = await _service.GetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<ActionResult> Create([FromBody] WarehouseCreateDTO dto)
        {
            var result = await _service.Create(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<ActionResult> Update(int id, [FromBody] WarehouseUpdateDTO dto)
        {
            var result = await _service.Update(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await _service.Delete(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
