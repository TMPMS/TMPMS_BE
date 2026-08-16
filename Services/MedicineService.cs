using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly IMedicineRepository _repo;
        public MedicineService(IMedicineRepository repo) => _repo = repo;

        // Discount % luôn suy ra từ Price/OldPrice tại thời điểm đọc/ghi — không bao giờ tin
        // vào một con số Discount nhập tay/rời rạc có thể lệch khỏi giá thật đang hiển thị.
        private static int? ComputeDiscount(decimal? price, decimal? oldPrice)
        {
            if (price == null || oldPrice == null || oldPrice <= 0 || oldPrice <= price) return null;
            return (int)Math.Round((1 - price.Value / oldPrice.Value) * 100);
        }

        public async Task<(List<MedicineListItemDto> Items, int TotalCount, bool IsPaged)> SearchAsync(MedicineSearchFilterDto filter)
        {
            var (medicines, totalCount) = await _repo.SearchAsync(filter);
            var medIds = medicines.Select(m => m.Id).ToList();
            var reviewStats = await _repo.GetReviewStatsAsync(medIds);

            var items = medicines.Select(m =>
            {
                var hasStat = reviewStats.TryGetValue(m.Id, out var stat) && stat.Count > 0;
                var rating = hasStat ? Math.Round(stat.AvgRating, 1) : Math.Round(4.3 + (m.Id % 8) / 10.0, 1);

                return new MedicineListItemDto
                {
                    Id = m.Id,
                    CategoryId = m.CategoryId,
                    SupplierId = m.SupplierId,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    PriceStatus = m.Price == null ? "contact" : "available",
                    StockQuantity = m.StockQuantity,
                    ManufactureDate = m.ManufactureDate,
                    ExpiryDate = m.ExpiryDate,
                    RequiresPrescription = m.RequiresPrescription,
                    ImageUrl = m.ImageUrl,
                    Unit = m.Unit,
                    Origin = m.Origin,
                    Packaging = m.Packaging,
                    Barcode = m.Barcode,
                    OldPrice = m.OldPrice,
                    Discount = ComputeDiscount(m.Price, m.OldPrice),
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    Rating = rating,
                    ReviewCount = reviewStats.TryGetValue(m.Id, out var st) ? st.Count : 0
                };
            }).ToList();

            return (items, totalCount, filter.IsPaged);
        }

        public async Task<HerbalFilterOptionsDto> GetHerbalFilterOptionsAsync()
        {
            var (partUsed, effects) = await _repo.GetHerbalFilterOptionsAsync();
            return new HerbalFilterOptionsDto { PartUsed = partUsed, Effects = effects };
        }

        public async Task<MedicineDetailDto?> GetByIdAsync(int id)
        {
            var m = await _repo.GetByIdAsync(id);
            if (m == null) return null;
            return new MedicineDetailDto
            {
                Id = m.Id,
                CategoryId = m.CategoryId,
                SupplierId = m.SupplierId,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                PriceStatus = m.Price == null ? "contact" : "available",
                StockQuantity = m.StockQuantity,
                ManufactureDate = m.ManufactureDate,
                ExpiryDate = m.ExpiryDate,
                RequiresPrescription = m.RequiresPrescription,
                ImageUrl = m.ImageUrl,
                Unit = m.Unit,
                Origin = m.Origin,
                Packaging = m.Packaging,
                Barcode = m.Barcode,
                OldPrice = m.OldPrice,
                Discount = ComputeDiscount(m.Price, m.OldPrice),
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt
            };
        }

        public async Task<MedicineBarcodeDto?> GetByBarcodeAsync(string barcode)
        {
            var m = await _repo.GetByBarcodeAsync(barcode);
            if (m == null) return null;
            return new MedicineBarcodeDto
            {
                Id = m.Id,
                CategoryId = m.CategoryId,
                SupplierId = m.SupplierId,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                PriceStatus = m.Price == null ? "contact" : "available",
                StockQuantity = m.StockQuantity,
                RequiresPrescription = m.RequiresPrescription,
                ImageUrl = m.ImageUrl,
                Unit = m.Unit,
                Origin = m.Origin,
                Packaging = m.Packaging,
                Barcode = m.Barcode,
                OldPrice = m.OldPrice,
                Discount = ComputeDiscount(m.Price, m.OldPrice),
                IsActive = m.IsActive
            };
        }

        public async Task<Medicine> CreateAsync(MedicineCreateDto dto)
        {
            var medicine = new Medicine
            {
                Name = dto.Name,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                SupplierId = dto.SupplierId,
                Price = dto.Price,
                OldPrice = dto.OldPrice,
                Discount = ComputeDiscount(dto.Price, dto.OldPrice),
                RequiresPrescription = dto.RequiresPrescription,
                ImageUrl = dto.ImageUrl,
                Unit = dto.Unit,
                Origin = dto.Origin,
                Packaging = dto.Packaging,
                Barcode = dto.Barcode,
                ManufactureDate = dto.ManufactureDate ?? DateTime.UtcNow,
                ExpiryDate = dto.ExpiryDate ?? DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                // Tồn kho chỉ được cộng qua nhập lô (StockBatch) — sản phẩm mới luôn bắt đầu từ 0
                // và cần nhập lô đầu tiên (số lô, NSX, HSD thật) trong tab Nhập kho.
                StockQuantity = 0
            };

            return await _repo.CreateAsync(medicine);
        }

        public async Task<MedicineUpdateResponseDto?> UpdateAsync(int id, MedicineUpdateDto dto)
        {
            var med = await _repo.GetByIdAsync(id);
            if (med == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Name)) med.Name = dto.Name;
            if (dto.Description != null) med.Description = dto.Description;
            if (dto.Price != null) med.Price = dto.Price;
            if (dto.OldPrice != null) med.OldPrice = dto.OldPrice;
            // Số lượng tồn kho không còn được sửa trực tiếp ở đây — nguồn sự thật là StockBatches,
            // chỉnh qua API /api/inventory/batches (nhập lô mới / hủy / kiểm kê điều chỉnh).
            if (!string.IsNullOrWhiteSpace(dto.Unit)) med.Unit = dto.Unit;
            if (!string.IsNullOrWhiteSpace(dto.Origin)) med.Origin = dto.Origin;
            if (!string.IsNullOrWhiteSpace(dto.Packaging)) med.Packaging = dto.Packaging;
            if (dto.Barcode != null) med.Barcode = dto.Barcode;
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) med.ImageUrl = dto.ImageUrl;
            if (dto.RequiresPrescription != null) med.RequiresPrescription = dto.RequiresPrescription.Value;
            if (dto.CategoryId != null && dto.CategoryId > 0) med.CategoryId = dto.CategoryId.Value;
            if (dto.SupplierId != null && dto.SupplierId > 0) med.SupplierId = dto.SupplierId.Value;

            // Giá hoặc giá cũ vừa đổi thì % giảm phải đổi theo ngay, không để lại số cũ sai lệch.
            if (dto.Price != null || dto.OldPrice != null) med.Discount = ComputeDiscount(med.Price, med.OldPrice);

            await _repo.SaveChangesAsync();

            return new MedicineUpdateResponseDto
            {
                Id = med.Id,
                CategoryId = med.CategoryId,
                SupplierId = med.SupplierId,
                Name = med.Name,
                Description = med.Description,
                Price = med.Price,
                StockQuantity = med.StockQuantity,
                RequiresPrescription = med.RequiresPrescription,
                ImageUrl = med.ImageUrl,
                Unit = med.Unit,
                Origin = med.Origin,
                Packaging = med.Packaging,
                Barcode = med.Barcode,
                OldPrice = med.OldPrice,
                Discount = med.Discount,
                IsActive = med.IsActive
            };
        }

        public async Task<(bool Found, bool Deactivated, string? Name)> DeleteAsync(int id)
        {
            var med = await _repo.GetByIdAsync(id);
            if (med == null) return (false, false, null);

            var name = med.Name;
            var hasLinks = await _repo.HasLinksAsync(id);
            if (hasLinks)
            {
                await _repo.DeactivateAsync(med);
                return (true, true, name);
            }

            await _repo.DeleteAsync(med);
            return (true, false, name);
        }
    }
}
