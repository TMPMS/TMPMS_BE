using System;
using System.Collections.Generic;

namespace BusinessObjects
{
    public static class StockBatchStatus
    {
        public const string Active = "Active";
        public const string Quarantine = "Quarantine";
        public const string ReturnedToVendor = "ReturnedToVendor";
        public const string Expired = "Expired";
        public const string Depleted = "Depleted";
        public const string Disposed = "Disposed";
    }

    public class StockBatch
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }

        public int WarehouseId { get; set; }

        public int? SupplierId { get; set; }

        /// <summary>Số lô — do NCC cung cấp hoặc hệ thống tự sinh. Duy nhất theo (MedicineId, WarehouseId).</summary>
        public string BatchNumber { get; set; }

        public DateTime ManufactureDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        /// <summary>Số lượng nhập ban đầu của lô — không đổi, dùng để đối chiếu.</summary>
        public int QuantityReceived { get; set; }

        /// <summary>Số lượng còn lại trong lô — giảm dần khi xuất/bán/hủy.</summary>
        public int QuantityRemaining { get; set; }

        public decimal? UnitCostPrice { get; set; }

        public DateTime ReceivedAt { get; set; }

        public string Status { get; set; } = StockBatchStatus.Active;

        public string? Note { get; set; }

        public Medicine Medicine { get; set; }

        public Warehouse Warehouse { get; set; }

        public Supplier Supplier { get; set; }

        public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
    }
}
