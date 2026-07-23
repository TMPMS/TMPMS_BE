namespace TMPMS.DTOs
{
    public class AppointmentCreateDTO
    {
        public int? StaffId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string Reason { get; set; }

        public string? Note { get; set; }
    }
}
