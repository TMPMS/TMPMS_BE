using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }

        public int WarehouseId { get; set; }

        public string Type { get; set; }

        public int Quantity { get; set; }

        public string ReferenceId { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Lô hàng liên quan đến giao dịch này (null cho các giao dịch cũ trước khi có quản lý theo lô).</summary>
        public int? StockBatchId { get; set; }

        /// <summary>
        /// Giá vốn của lô TẠI THỜI ĐIỂM giao dịch này xảy ra (chỉ có ý nghĩa với Type=Export) — chốt cứng
        /// ngay lúc xuất kho, không đổi theo sau dù StockBatch.UnitCostPrice có bị cập nhật lại vì nhập
        /// thêm hàng giá khác. Null cho giao dịch cũ tạo trước khi có cột này, hoặc các Type khác Export.
        /// </summary>
        public decimal? UnitCostPrice { get; set; }

        public Medicine Medicine { get; set; }

        public Warehouse Warehouse { get; set; }

        public StockBatch StockBatch { get; set; }
    }
}
