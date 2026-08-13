using System;

namespace TMPMS.DTOs
{
    public class VoucherCreateInputDto
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string DiscountType { get; set; } = "percent";
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int UsageLimit { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public string Type { get; set; } = "product";
        public bool IsWheelPrize { get; set; } = false;
        public int Weight { get; set; } = 0;
    }

    public class VoucherUpdateInputDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public bool? IsActive { get; set; }
        public string? Type { get; set; }
        public bool? IsWheelPrize { get; set; }
        public int? Weight { get; set; }
    }

    public class ValidateVoucherRequestDto
    {
        public string Code { get; set; } = "";
        public decimal Order_Total { get; set; }
        public string Type { get; set; } = "product";
        // Chỉ dùng khi Type == "shipping", để cap số tiền giảm không vượt phí ship thực tế.
        public decimal? ShippingFee { get; set; }
    }
}
