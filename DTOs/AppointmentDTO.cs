namespace TMPMS.DTOs
{
    public class AppointmentDTO
    {
        public int Id { get; set; }

        public string PatientName { get; set; }

        public string? StaffName { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; }

    }
}
