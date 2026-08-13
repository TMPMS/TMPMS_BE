using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly TMPMSDbContext _context;
        public MedicineRepository(TMPMSDbContext context) => _context = context;

        public async Task<(List<Medicine> Items, int TotalCount)> SearchAsync(MedicineSearchFilterDto filter)
        {
            var query = _context.Medicines.Where(m => m.IsActive).AsQueryable();

            if (!filter.IncludeRx)
            {
                query = query.Where(m => !m.RequiresPrescription);
            }

            if (!string.IsNullOrEmpty(filter.CategoryIdStr))
            {
                var cleanId = filter.CategoryIdStr.Replace("eq.", "");
                if (int.TryParse(cleanId, out int catId))
                {
                    query = query.Where(m => m.CategoryId == catId);
                }
            }

            if (!string.IsNullOrEmpty(filter.SupplierIdStr))
            {
                var cleanId = filter.SupplierIdStr.Replace("eq.", "");
                if (int.TryParse(cleanId, out int supId))
                {
                    query = query.Where(m => m.SupplierId == supId);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Origin))
            {
                query = query.Where(m => m.Origin != null && m.Origin.Contains(filter.Origin.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(filter.Unit))
            {
                query = query.Where(m => m.Unit != null && m.Unit.Contains(filter.Unit.Trim()));
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(m => m.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(m => m.Price <= filter.MaxPrice.Value);
            }

            if (!string.IsNullOrEmpty(filter.Name))
            {
                var searchTerm = Uri.UnescapeDataString(filter.Name)
                    .Replace("ilike.*", "")
                    .Replace("*", "")
                    .Trim();

                // Khớp theo từng từ thay vì cả cụm nguyên văn: tên do AI đọc từ ảnh (hoặc gõ tay)
                // thường khác thứ tự/thêm bớt từ so với tên lưu trong danh mục (vd "Mật Ong Rừng
                // Tây Nguyên" khi tên thật là "Mật ong hoa rừng nguyên chất Tây Nguyên"), nên yêu
                // cầu khớp nguyên cụm sẽ bỏ sót sản phẩm dù đọc đúng nhãn hàng.
                var words = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    foreach (var word in words)
                    {
                        var w = word;
                        query = query.Where(m => m.Name.Contains(w) || (m.Description != null && m.Description.Contains(w)));
                    }
                }
                else
                {
                    query = query.Where(m => m.Name.Contains(searchTerm) || (m.Description != null && m.Description.Contains(searchTerm)));
                }
            }

            if (filter.InStock == true)
            {
                query = query.Where(m => m.StockQuantity > 0);
            }

            if (filter.HasDiscount == true)
            {
                query = query.Where(m => m.Discount != null && m.Discount > 0);
            }

            var partUsed = string.IsNullOrWhiteSpace(filter.PartUsed) ? null : filter.PartUsed.Trim();
            var effects = string.IsNullOrWhiteSpace(filter.Effects) ? null : filter.Effects.Trim();
            if (filter.HerbalOnly == true || partUsed != null || effects != null)
            {
                query = query.Where(m => _context.HerbalMedicineInfos.Any(h => h.MedicineId == m.Id
                    && (partUsed == null || h.PartUsed.Contains(partUsed))
                    && (effects == null || h.Effects.Contains(effects))));
            }

            // Đếm tổng số kết quả TRƯỚC khi phân trang, để FE hiển thị "còn bao nhiêu" / nút tải thêm.
            var totalCount = await query.CountAsync();

            query = filter.Sort switch
            {
                "priceAsc" => query.OrderBy(m => m.Price),
                "priceDesc" => query.OrderByDescending(m => m.Price),
                "nameAsc" => query.OrderBy(m => m.Name),
                "nameDesc" => query.OrderByDescending(m => m.Name),
                "newest" => query.OrderByDescending(m => m.CreatedAt),
                _ => query.OrderBy(m => m.Id)
            };

            // Phân trang là OPT-IN: chỉ áp dụng khi FE gửi page/page_size, để không phá các nơi
            // (vd. trang Admin) vẫn cần tải toàn bộ danh sách thuốc trong 1 lần gọi.
            if (filter.IsPaged)
            {
                query = query.Skip((filter.Page!.Value - 1) * filter.PageSize!.Value).Take(filter.PageSize.Value);
            }

            var items = await query.ToListAsync();
            return (items, totalCount);
        }

        public async Task<Dictionary<int, (double AvgRating, int Count)>> GetReviewStatsAsync(List<int> medicineIds)
        {
            var stats = await _context.Reviews
                .Where(r => medicineIds.Contains(r.MedicineId))
                .GroupBy(r => r.MedicineId)
                .Select(g => new { MedicineId = g.Key, AvgRating = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToListAsync();

            return stats.ToDictionary(x => x.MedicineId, x => (x.AvgRating, x.Count));
        }

        public async Task<(List<string> PartUsed, List<string> Effects)> GetHerbalFilterOptionsAsync()
        {
            var partUsed = await _context.HerbalMedicineInfos
                .Where(h => h.PartUsed != null && h.PartUsed != "")
                .Select(h => h.PartUsed)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var effects = await _context.HerbalMedicineInfos
                .Where(h => h.Effects != null && h.Effects != "")
                .Select(h => h.Effects)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return (partUsed, effects);
        }

        public async Task<Medicine?> GetByIdAsync(int id) => await _context.Medicines.FindAsync(id);

        public async Task<Medicine?> GetByBarcodeAsync(string barcode) =>
            await _context.Medicines.FirstOrDefaultAsync(x => x.IsActive && x.Barcode == barcode);

        public async Task<Medicine> CreateAsync(Medicine medicine)
        {
            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();
            return medicine;
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<bool> HasLinksAsync(int medicineId)
        {
            return await _context.OrderItems.AnyAsync(oi => oi.MedicineId == medicineId) ||
                   await _context.CartItems.AnyAsync(ci => ci.MedicineId == medicineId) ||
                   await _context.PrescriptionItems.AnyAsync(pi => pi.MedicineId == medicineId) ||
                   await _context.StockBatches.AnyAsync(b => b.MedicineId == medicineId);
        }

        public async Task DeactivateAsync(Medicine medicine)
        {
            medicine.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Medicine medicine)
        {
            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();
        }
    }
}
