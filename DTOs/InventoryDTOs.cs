using System;

namespace TMPMS.DTOs
{
    // Nhập / Xuất kho
    public class StockTransactionCreateDTO
    {
        public int MedicineId { get; set; }
        public int WarehouseId { get; set; }
        public string Type { get; set; }   // Import, Export, Adjustment
        public int Quantity { get; set; }
        public string ReferenceId { get; set; } // Mã đơn hàng/PO liên quan (nếu có)
    }

    public class InventoryStockResponseDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int Quantity { get; set; }
    }

    public class InventoryTransactionResponseDTO
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string Type { get; set; }
        public int Quantity { get; set; }
        public string ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LowStockAlertDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int CurrentQuantity { get; set; }
        public int Threshold { get; set; }
    }

    public class ExpiryAlertDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
        public int StockQuantity { get; set; }
    }
}
