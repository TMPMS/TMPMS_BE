using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Services.Interfaces
{
    public class WheelPrizeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string DiscountType { get; set; } = "";
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
    }

    public class WheelStatusResult
    {
        public bool CanSpinToday { get; set; }
        public DateTime? NextResetAtUtc { get; set; }
        public bool Blocked { get; set; }
        public DateTime? LastSpinDate { get; set; }
        public Voucher? LastSpinVoucher { get; set; }
    }

    public class WheelSpinResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        // true = đã quay hôm nay rồi (409 Conflict); false = lỗi khác (400 BadRequest).
        public bool AlreadySpunToday { get; set; }
        public Voucher? Voucher { get; set; }
        public int PrizeIndex { get; set; }
    }

    public interface IWheelService
    {
        Task<List<WheelPrizeDto>> GetPrizesAsync();
        Task<WheelStatusResult> GetStatusAsync(int userId, bool isBlockedRole);
        Task<WheelSpinResult> SpinAsync(int userId, bool isBlockedRole);
    }
}
