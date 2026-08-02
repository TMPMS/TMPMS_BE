namespace TMPMS.DTOs
{
    public class AppointmentBookingResult
    {
        public bool Success { get; set; }

        // Lịch hẹn đang hoạt động (Pending/Confirmed chưa quá hạn) đang chặn user đặt lịch mới.
        public AppointmentDTO? BlockingAppointment { get; set; }
    }
}
