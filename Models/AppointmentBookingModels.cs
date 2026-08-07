using BusinessObjects;

namespace TMPMS.Models
{
    public class AppointmentSlotHold
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? StaffId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Location { get; set; } = "Nhà thuốc TMPMS";
        public string Token { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsConsumed { get; set; }
        public User User { get; set; } = null!;
    }

    public class AppointmentPayment
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid";
        public string TransactionCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal RefundAmount { get; set; }
        public string? RefundStatus { get; set; }
        public Appointment Appointment { get; set; } = null!;
    }

    public class AppointmentRescheduleRequest
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public DateTime OldAppointmentDate { get; set; }
        public DateTime RequestedAppointmentDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByStaffId { get; set; }
        public Appointment Appointment { get; set; } = null!;
    }

    public class AppointmentPaymentIntent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SlotHoldId { get; set; }
        public long OrderCode { get; set; }
        public string SymptomDescription { get; set; } = string.Empty;
        public string? PrescriptionImageUrl { get; set; }
        public string? Note { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public string? PaymentLinkId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public AppointmentSlotHold SlotHold { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
