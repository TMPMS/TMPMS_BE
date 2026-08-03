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

        // PendingConfirmation (Chờ xác nhận) | Confirmed (Đã xác nhận) |
        // Completed (Hoàn thành) | Cancelled (Đã hủy) | Rejected (Từ chối) | Expired (Quá hạn)
        public string Status { get; set; } = "PendingConfirmation";

        // Thời gian tạo (UTC)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Hạn chót Pharmacy phải xác nhận — quá hạn mà chưa xác nhận => Expired (giải phóng khung giờ)
        public DateTime ConfirmationDeadline { get; set; }

        // Thời điểm Pharmacy xác nhận lịch hẹn
        public DateTime? ConfirmedAt { get; set; }

        // Staff/Admin đã xác nhận lịch hẹn (FK -> AspNetUsers)
        public int? ConfirmedByStaffId { get; set; }

        // Lý do từ chối (khi Status = Rejected)
        public string? RejectionReason { get; set; }

        // Navigation Property
        public User User { get; set; }

        public User? Staff { get; set; }

        public User? ConfirmedBy { get; set; }
    }
}
