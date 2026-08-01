using System;

namespace BusinessObjects
{
    // Hóa đơn phát sinh từ đơn hàng đã thanh toán
    public class Invoice
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string InvoiceCode { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime IssuedAt { get; set; }

        public Order Order { get; set; }
    }
}
