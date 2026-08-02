using System;

namespace TMPMS.DTOs
{
    public class PaymentCreateDTO
    {
        public int OrderId { get; set; }
        public string Method { get; set; }  // Cash, BankTransfer, CreditCard, MoMo, VNPay...
        public decimal Amount { get; set; }
    }

    public class PaymentUpdateStatusDTO
    {
        public string Status { get; set; }         // Pending, Success, Failed, Refunded
        public string? TransactionCode { get; set; }
    }

    public class PaymentResponseDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Method { get; set; }
        public string TransactionCode { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
