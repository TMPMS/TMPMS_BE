using BusinessObjects;

namespace TMPMS.DTOs
{
    public class CartItemViewDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public int? AllowedQuantity { get; set; }
        public Medicine Medicine { get; set; } = null!;
    }

    // Kết quả 1 thao tác thêm/sửa/xóa CartItem — gói đủ thông tin để Controller quyết định mã HTTP
    // (200/201/400/403) mà không phải tự lặp lại logic nghiệp vụ Rx Allowance.
    public class CartItemActionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        // 403 nếu chưa có đơn thuốc hợp lệ; 400 nếu các lỗi nghiệp vụ khác (vượt số lượng, hết giá...).
        public bool RequiresPrescription { get; set; }
        public CartItem? Item { get; set; }
        public bool Created { get; set; }
    }
}
