using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("categories")]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public CategoriesController(TMPMSDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new { c.Id, c.Name, c.Description, ProductCount = c.Medicines.Count(m => m.IsActive) })
                .ToListAsync();
            return Ok(categories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Tên danh mục không được để trống");

            var category = new Category { Name = dto.Name.Trim(), Description = dto.Description };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await this.LogAuditAsync(_auditLogService, "Category", "Create", category.Id.ToString(), $"Tạo danh mục '{category.Name}'");
            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, new { category.Id, category.Name, category.Description, ProductCount = 0 });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Tên danh mục không được để trống");

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Name = dto.Name.Trim();
            category.Description = dto.Description;
            await _context.SaveChangesAsync();

            await this.LogAuditAsync(_auditLogService, "Category", "Update", id.ToString(), $"Cập nhật danh mục #{id} ('{category.Name}')");

            var productCount = await _context.Medicines.CountAsync(m => m.CategoryId == id && m.IsActive);
            return Ok(new { category.Id, category.Name, category.Description, ProductCount = productCount });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            var inUse = await _context.Medicines.AnyAsync(m => m.CategoryId == id);
            if (inUse) return BadRequest("Không thể xóa danh mục đang có sản phẩm sử dụng");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            await this.LogAuditAsync(_auditLogService, "Category", "Delete", id.ToString(), $"Xóa danh mục #{id} ('{category.Name}')");
            return NoContent();
        }
    }

    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
