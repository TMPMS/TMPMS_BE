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

        // Endpoint này được dùng cả cho trang quản trị (cần đủ Email/SĐT/địa chỉ/mã số thuế để sửa) lẫn
        // carousel "Thương hiệu" công khai trên trang chủ (Brands.jsx, không đăng nhập — chỉ cần tên +
        // số sản phẩm). Trước đây trả nguyên DTO đầy đủ cho mọi request, lộ thông tin liên hệ/mã số thuế
        // của toàn bộ nhà cung cấp ra công khai. Giờ chỉ Admin/Staff mới thấy đủ field.
        private bool CanViewSupplierDetails() => User.IsInRole("Admin") || User.IsInRole("Staff");

        private static SupplierDto ToPublicDto(SupplierDto s) => new SupplierDto
        {
            Id = s.Id,
            CompanyName = s.CompanyName,
            ProductCount = s.ProductCount,
            Status = s.Status
        };

        [HttpGet("~/suppliers")]
        [HttpGet("~/api/suppliers")]
        [HttpGet("api/[controller]")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.GetAllAsync();
            if (!CanViewSupplierDetails()) list = list.Select(ToPublicDto).ToList();
            return Ok(list);
        }

        [HttpGet("~/suppliers/{id}")]
        [HttpGet("~/api/suppliers/{id}")]
        [HttpGet("api/[controller]/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _service.GetByIdAsync(id);
            if (res == null) return NotFound();
            return Ok(CanViewSupplierDetails() ? res : ToPublicDto(res));
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
            try
            {
                var ok = await _service.DeleteAsync(id);
                if (!ok) return NotFound();
                await this.LogAuditAsync(_auditLogService, "Supplier", "Delete", id.ToString(), $"Xóa nhà cung cấp #{id}");
                return NoContent();
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
