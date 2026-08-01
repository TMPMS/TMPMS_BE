namespace TMPMS.DTOs
{
    public class AppointmentCreateDTO
    {
        public int? PatientId { get; set; }
        public int? StaffId { get; set; }
        public int? DoctorId { get; set; } // Alias for StaffId
        public DateTime AppointmentDate { get; set; }
        public string Reason { get; set; } = "";
        public string? Note { get; set; }
        public string? Notes { get; set; } // Alias for Note
        public string? Status { get; set; }
    }
}
