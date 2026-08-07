namespace TMPMS.DTOs
{
    public class AppointmentDTO
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = "";
        public string? PatientPhone { get; set; }
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ConfirmationDeadline { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string SymptomDescription { get; set; } = "";
        public string? PrescriptionImageUrl { get; set; }
        public string Location { get; set; } = "";
        public decimal DepositAmount { get; set; }
        public string PaymentStatus { get; set; } = "";
        public string? PaymentMethod { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }
}
