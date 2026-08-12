using System;

namespace BusinessObjects
{
    public class LoyaltyPointTransaction
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // Dương = tích điểm (đơn hàng giao thành công), Âm = đổi điểm lấy voucher hoặc bị trừ khi hoàn hàng.
        public int Points { get; set; }

        public string Reason { get; set; } = string.Empty;

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        public int? VoucherId { get; set; }
        public Voucher? Voucher { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
