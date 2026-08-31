using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Medicine
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public int SupplierId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal? Price { get; set; }

        public int StockQuantity { get; set; }

        public DateTime ManufactureDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool RequiresPrescription { get; set; }

        public string ImageUrl { get; set; }

        public string? Unit { get; set; }

        public string? Origin { get; set; }

        public string? Packaging { get; set; }

        public string? Barcode { get; set; }

        public decimal? OldPrice { get; set; }

        public int? Discount { get; set; }

        /// <summary>
        /// Id của StockBatch đang "quyết định" Price hiện tại qua cơ chế đồng bộ tự động (khi lô đó có
        /// SellPrice riêng) — chỉ dùng để nhận biết "lô đầu hàng đợi FEFO đã đổi hay chưa" giữa 2 lần
        /// đồng bộ, tránh ghi đè lại Price liên tục mỗi khi có sự kiện kho không liên quan (điều đó sẽ
        /// xoá mất giá Admin/Dược sĩ vừa sửa tay). Null = giá hiện tại không do lô nào quyết định (Admin
        /// tự đặt, hoặc chưa có lô nào có SellPrice).
        /// </summary>
        public int? PricedFromBatchId { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Soft-delete flag: false = ẩn khỏi cửa hàng nhưng giữ lịch sử đơn hàng</summary>
        public bool IsActive { get; set; } = true;

        public Category? Category { get; set; }

        public Supplier? Supplier { get; set; }

        public ICollection<CartItem>? CartItems { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }

        public ICollection<Review>? Reviews { get; set; }

        public ICollection<MedicineImage>? Images { get; set; }

        public ICollection<PrescriptionItem>? PrescriptionItems { get; set; }

        public ICollection<InventoryStock>? InventoryStocks { get; set; }

        public ICollection<StockBatch>? StockBatches { get; set; }
    }
}
