namespace BusinessObjects
{
    public class Appointment
    {
        public int Id { get; set; }

        // Người đặt lịch
        public int UserId { get; set; }

        // Dược sĩ/Nhân viên phụ trách
        public int? StaffId { get; set; }

        // Ngày giờ hẹn
        public DateTime AppointmentDate { get; set; }

        // Lý do đặt lịch
        public string Reason { get; set; }

        // Ghi chú
        public string? Note { get; set; }

        // Pending | Confirmed | Completed | Cancelled | Expired
        public string Status { get; set; } = "Pending";

        // Thời gian tạo
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        public User User { get; set; }

        public User? Staff { get; set; }
    }
}
