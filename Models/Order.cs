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

        public string Status { get; set; }

        public string ShippingAddress { get; set; }

        public string PaymentStatus { get; set; }

        public string? DeliveryMethod { get; set; }

        public decimal? ShippingFee { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ReturnReason { get; set; }

        public User User { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }

        public ICollection<Payment> Payments { get; set; }
    }
}
