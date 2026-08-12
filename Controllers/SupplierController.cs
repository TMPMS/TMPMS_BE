using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _service;
        private readonly IAuditLogService _auditLogService;
        public SupplierController(ISupplierService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

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
            await this.LogAuditAsync(_auditLogService, "Supplier", "Create", res.Id.ToString(), $"Tạo nhà cung cấp '{res.CompanyName}'");
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpPut("~/Supplier/{id}")]
        [HttpPut("api/[controller]/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] SupplierCreateDto dto)
        {
            var res = await _service.UpdateAsync(id, dto);
            if (res == null) return NotFound();
            await this.LogAuditAsync(_auditLogService, "Supplier", "Update", id.ToString(), $"Cập nhật nhà cung cấp #{id} ('{res.CompanyName}')");
            return Ok(res);
        }

        [HttpDelete("~/Supplier/{id}")]
        [HttpDelete("api/[controller]/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            await this.LogAuditAsync(_auditLogService, "Supplier", "Delete", id.ToString(), $"Xóa nhà cung cấp #{id}");
            return NoContent();
        }
    }
}
