using System;

namespace BusinessObjects
{
    // Ghi nhận lượt quay vòng quay may mắn hàng ngày của mỗi user — dùng unique index
    // (UserId, SpinDate) để đảm bảo mỗi user chỉ quay được 1 lần/ngày.
    public class WheelSpin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime SpinDate { get; set; }
        public int? VoucherId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Voucher? Voucher { get; set; }
    }
}
