using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Status { get; set; }

        public string? ShippingAddress { get; set; }

        public string? PaymentStatus { get; set; }

        public string? DeliveryMethod { get; set; }

        public decimal? ShippingFee { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ReturnReason { get; set; }

        // Nhân viên đã xử lý đơn (gán khi đơn chuyển sang "Delivered") — dùng để thống kê
        // doanh thu theo nhân viên.
        public int? ProcessedByStaffId { get; set; }

        // Voucher đã áp dụng cho đơn này (audit trail) — tối đa 1 voucher/loại.
        public int? ProductVoucherId { get; set; }
        public int? ShippingVoucherId { get; set; }

        public User? User { get; set; }

        public User? ProcessedByStaff { get; set; }

        public Voucher? ProductVoucher { get; set; }
        public Voucher? ShippingVoucher { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }

        public ICollection<Payment>? Payments { get; set; }
    }
}
