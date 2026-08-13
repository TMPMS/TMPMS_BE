using System;

using System.ComponentModel.DataAnnotations;

namespace TMPMS.DTOs
{
    public class MedicineCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int SupplierId { get; set; }
        public decimal? Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int? Discount { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Origin { get; set; }
        public string? Packaging { get; set; }
        public string? Barcode { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

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
        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }
        public bool? RequiresPrescription { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
    }

    // Bộ lọc tìm kiếm dược phẩm — gom toàn bộ query string của GET /medicines thành 1 DTO,
    // truyền xuống Repository để build LINQ query (thay vì Controller tự build IQueryable).
    public class MedicineSearchFilterDto
    {
        public string? CategoryIdStr { get; set; }
        public string? SupplierIdStr { get; set; }
        public string? Origin { get; set; }
        public string? Unit { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Name { get; set; }
        public bool IncludeRx { get; set; }
        public bool? InStock { get; set; }
        public bool? HasDiscount { get; set; }
        public string? PartUsed { get; set; }
        public string? Effects { get; set; }
        public bool? HerbalOnly { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }

        public bool IsPaged => Page.HasValue && PageSize.HasValue && PageSize.Value > 0;
    }

    public class MedicineListItemDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string PriceStatus { get; set; } = "available";
        public int StockQuantity { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Origin { get; set; }
        public string? Packaging { get; set; }
        public string? Barcode { get; set; }
        public decimal? OldPrice { get; set; }
        public int? Discount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class MedicineDetailDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string PriceStatus { get; set; } = "available";
        public int StockQuantity { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Origin { get; set; }
        public string? Packaging { get; set; }
        public string? Barcode { get; set; }
        public decimal? OldPrice { get; set; }
        public int? Discount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MedicineBarcodeDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string PriceStatus { get; set; } = "available";
        public int StockQuantity { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Origin { get; set; }
        public string? Packaging { get; set; }
        public string? Barcode { get; set; }
        public decimal? OldPrice { get; set; }
        public int? Discount { get; set; }
        public bool IsActive { get; set; }
    }

    public class MedicineUpdateResponseDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int StockQuantity { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Origin { get; set; }
        public string? Packaging { get; set; }
        public string? Barcode { get; set; }
        public decimal? OldPrice { get; set; }
        public bool IsActive { get; set; }
    }

    public class HerbalFilterOptionsDto
    {
        public System.Collections.Generic.List<string> PartUsed { get; set; } = new();
        public System.Collections.Generic.List<string> Effects { get; set; } = new();
    }
}
