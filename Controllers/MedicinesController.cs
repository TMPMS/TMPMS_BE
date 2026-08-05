using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    public class MedicineUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int? StockQuantity { get; set; }
        public string? Unit { get; set; }
        public string? Origin { get; set; }
        public string? Packaging { get; set; }
        public string? ImageUrl { get; set; }
        public bool? RequiresPrescription { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class MedicinesController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public MedicinesController(TMPMSDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicines(
            [FromQuery(Name = "category_id")] string? categoryIdStr, 
            [FromQuery(Name = "name")] string? nameStr,
            [FromQuery(Name = "include_rx")] bool includeRx = false)
        {
            var query = _context.Medicines.Where(m => m.IsActive).AsQueryable();

            if (!includeRx)
            {
                query = query.Where(m => !m.RequiresPrescription);
            }

            if (!string.IsNullOrEmpty(categoryIdStr))
            {
                var cleanId = categoryIdStr.Replace("eq.", "");
                if (int.TryParse(cleanId, out int catId))
                {
                    query = query.Where(m => m.CategoryId == catId);
                }
            }

            if (!string.IsNullOrEmpty(nameStr))
            {
                var searchTerm = Uri.UnescapeDataString(nameStr)
                    .Replace("ilike.*", "")
                    .Replace("*", "")
                    .Trim();
                
                query = query.Where(m => m.Name.Contains(searchTerm));
            }

            var medicines = await query
                .OrderBy(m => m.Id)
                .ToListAsync();

            var response = medicines.Select(m => new {
                m.Id,
                m.CategoryId,
                m.SupplierId,
                m.Name,
                m.Description,
                m.Price,
                PriceStatus = m.Price == null ? "contact" : "available",
                m.StockQuantity,
                m.ManufactureDate,
                m.ExpiryDate,
                m.RequiresPrescription,
                m.ImageUrl,
                m.Unit,
                m.Origin,
                m.Packaging,
                m.OldPrice,
                m.Discount,
                m.IsActive,
                m.CreatedAt
            });

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMedicineById(int id)
        {
            var m = await _context.Medicines.FindAsync(id);
            if (m == null) return NotFound();

            return Ok(new {
                m.Id,
                m.CategoryId,
                m.SupplierId,
                m.Name,
                m.Description,
                m.Price,
                PriceStatus = m.Price == null ? "contact" : "available",
                m.StockQuantity,
                m.ManufactureDate,
                m.ExpiryDate,
                m.RequiresPrescription,
                m.ImageUrl,
                m.Unit,
                m.Origin,
                m.Packaging,
                m.OldPrice,
                m.Discount,
                m.IsActive,
                m.CreatedAt
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> AddMedicine([FromBody] Medicine medicine)
        {
            medicine.CreatedAt = DateTime.UtcNow;
            medicine.IsActive = true;
            if (medicine.ManufactureDate == default) medicine.ManufactureDate = DateTime.UtcNow;
            if (medicine.ExpiryDate == default) medicine.ExpiryDate = DateTime.UtcNow.AddYears(1);

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            return StatusCode(201, medicine);
        }

        [HttpPut("{id}")]
        [HttpPatch("{id}")]
        [HttpPut]
        [HttpPatch]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> UpdateMedicine(
            [FromRoute] int? id,
            [FromQuery(Name = "id")] string? idQuery,
            [FromBody] MedicineUpdateDto dto)
        {
            int medId = id ?? 0;
            if (medId == 0 && !string.IsNullOrEmpty(idQuery))
            {
                var clean = idQuery.Replace("eq.", "");
                int.TryParse(clean, out medId);
            }

            var med = await _context.Medicines.FindAsync(medId);
            if (med == null) return NotFound(new { error = "Không tìm thấy dược phẩm" });

            if (!string.IsNullOrWhiteSpace(dto.Name)) med.Name = dto.Name;
            if (dto.Description != null) med.Description = dto.Description;
            if (dto.Price != null) med.Price = dto.Price;
            if (dto.OldPrice != null) med.OldPrice = dto.OldPrice;
            if (dto.StockQuantity != null) med.StockQuantity = dto.StockQuantity.Value;
            if (!string.IsNullOrWhiteSpace(dto.Unit)) med.Unit = dto.Unit;
            if (!string.IsNullOrWhiteSpace(dto.Origin)) med.Origin = dto.Origin;
            if (!string.IsNullOrWhiteSpace(dto.Packaging)) med.Packaging = dto.Packaging;
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) med.ImageUrl = dto.ImageUrl;
            if (dto.RequiresPrescription != null) med.RequiresPrescription = dto.RequiresPrescription.Value;
            if (dto.CategoryId != null && dto.CategoryId > 0) med.CategoryId = dto.CategoryId.Value;
            if (dto.SupplierId != null && dto.SupplierId > 0) med.SupplierId = dto.SupplierId.Value;

            await _context.SaveChangesAsync();

            return Ok(new {
                med.Id,
                med.CategoryId,
                med.SupplierId,
                med.Name,
                med.Description,
                med.Price,
                med.StockQuantity,
                med.RequiresPrescription,
                med.ImageUrl,
                med.Unit,
                med.Origin,
                med.Packaging,
                med.OldPrice,
                med.IsActive
            });
        }

        [HttpDelete("{id}")]
        [HttpDelete]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> DeleteMedicine(
            [FromRoute] int? id,
            [FromQuery(Name = "id")] string? idQuery)
        {
            int medId = id ?? 0;
            if (medId == 0 && !string.IsNullOrEmpty(idQuery))
            {
                var clean = idQuery.Replace("eq.", "");
                int.TryParse(clean, out medId);
            }

            var med = await _context.Medicines.FindAsync(medId);
            if (med == null) return NotFound(new { error = "Không tìm thấy dược phẩm" });

            bool hasLinks = await _context.OrderItems.AnyAsync(oi => oi.MedicineId == medId) ||
                            await _context.CartItems.AnyAsync(ci => ci.MedicineId == medId) ||
                            await _context.PrescriptionItems.AnyAsync(pi => pi.MedicineId == medId);

            if (hasLinks)
            {
                med.IsActive = false;
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Medicines.Remove(med);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã xóa hoặc ẩn dược phẩm thành công" });
        }
    }
}
