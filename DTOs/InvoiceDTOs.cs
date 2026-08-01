using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class InvoiceItemDTO
    {
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal => Quantity * Price;
    }

    public class InvoiceResponseDTO
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; }
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime IssuedAt { get; set; }
        public List<InvoiceItemDTO> Items { get; set; } = new();
    }
}
