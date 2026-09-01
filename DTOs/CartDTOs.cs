namespace TMPMS.DTOs
{
    // DTO thu hẹp cho Cart — KHÔNG trả thẳng entity Cart, vì navigation property Cart.User bị EF Core
    // "fixup" tự động từ user đang được track trong cùng DbContext của request (vd do middleware auth
    // đã load User trước đó), khiến PasswordHash/SecurityStamp bị serialize lộ ra response dù không có
    // .Include(c => c.User) nào trong query.
    public class CartViewDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
    }

    public class CartItemViewDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public int? AllowedQuantity { get; set; }
        public MedicineListItemDto Medicine { get; set; } = null!;
    }

    // Trả về sau khi thêm/sửa 1 CartItem — chỉ các field client thực sự cần, KHÔNG phải entity CartItem
    // (entity mang navigation Cart -> User, cùng lớp lỗi rò rỉ PasswordHash/SecurityStamp như CartViewDto).
    public class CartItemBriefDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
    }

    // Kết quả 1 thao tác thêm/sửa/xóa CartItem — gói đủ thông tin để Controller quyết định mã HTTP
    // (200/201/400/403) mà không phải tự lặp lại logic nghiệp vụ Rx Allowance.
    public class CartItemActionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        // 403 nếu chưa có đơn thuốc hợp lệ; 400 nếu các lỗi nghiệp vụ khác (vượt số lượng, hết giá...).
        public bool RequiresPrescription { get; set; }
        public CartItemBriefDto? Item { get; set; }
        public bool Created { get; set; }
    }
}
