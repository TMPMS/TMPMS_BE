namespace TMPMS.DTOs
{
    public class AppointmentUpdateDTO
    {
        public DateTime AppointmentDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Note { get; set; }
    }
}
