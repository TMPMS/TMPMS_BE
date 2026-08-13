using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class OrderItemInputDto
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class CheckoutRequestDto
    {
        public int UserId { get; set; }
        public string ShippingAddress { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public List<OrderItemInputDto> Items { get; set; } = new();
        public string DeliveryMethod { get; set; } = "Giao hàng hỏa tốc (Ship 2 Giờ)";
        public decimal ShippingFee { get; set; }
        // Tối đa 1 voucher/loại — 1 mã giảm giá sản phẩm + 1 mã giảm phí vận chuyển.
        public string? ProductVoucherCode { get; set; }
        public string? ShippingVoucherCode { get; set; }
    }

    public class OrderItemSummaryDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? MedicineName { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class OrderSummaryDto
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
        public string? Username { get; set; }
        public string? Email { get; set; }
        public int? PaymentId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatusDetail { get; set; }
        public List<OrderItemSummaryDto> Items { get; set; } = new();
    }

    public class ReturnRequestInputDto
    {
        public string Reason { get; set; } = "";
    }

    public class UpdateOrderStatusRequestDto
    {
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
