using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly INewsArticleService _service;
        public NewsController(INewsArticleService service) => _service = service;

        [HttpGet("~/news")]
        [HttpGet("api/[controller]")]
        public async Task<IActionResult> GetAll([FromQuery] string? tag) => Ok(await _service.GetAllAsync(tag));

        [HttpGet("~/news/{id}")]
        [HttpGet("api/[controller]/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _service.GetByIdAsync(id);
            if (res == null) return NotFound();
            return Ok(res);
        }

        [HttpPost("api/[controller]")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create([FromBody] NewsArticleCreateDto dto)
        {
            var res = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpPut("api/[controller]/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] NewsArticleCreateDto dto)
        {
            var res = await _service.UpdateAsync(id, dto);
            if (res == null) return NotFound();
            return Ok(res);
        }

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
