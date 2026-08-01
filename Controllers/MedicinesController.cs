using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("medicines")]
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
            [FromQuery(Name = "name")] string? nameStr)
        {
            var query = _context.Medicines.AsQueryable();

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
                // Decodes URL tags and formats like "ilike.*Canxi*" -> "Canxi"
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
                m.CreatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddMedicine([FromBody] Medicine medicine)
        {
            medicine.CreatedAt = DateTime.UtcNow;
            if (medicine.ManufactureDate == default) medicine.ManufactureDate = DateTime.UtcNow;
            if (medicine.ExpiryDate == default) medicine.ExpiryDate = DateTime.UtcNow.AddYears(1);

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            return StatusCode(201, medicine);
        }
    }
}
