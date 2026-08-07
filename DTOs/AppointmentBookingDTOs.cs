namespace TMPMS.DTOs
{
    public class SlotHoldRequestDTO
    {
        public DateTime AppointmentDate { get; set; }
        public string Location { get; set; } = "Nhà thuốc TMPMS";
    }

    public class AppointmentCheckoutDTO
    {
        public string HoldToken { get; set; } = string.Empty;
        public string SymptomDescription { get; set; } = string.Empty;
        public string? PrescriptionImageUrl { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "PayOS";
        public bool PolicyAccepted { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class AppointmentRescheduleCreateDTO
    {
        public DateTime AppointmentDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AppointmentProposalDTO
    {
        public DateTime AppointmentDate { get; set; }
        public string? Note { get; set; }
    }

    public class AppointmentDecisionDTO { public bool Accept { get; set; } }
}
