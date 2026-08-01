namespace TMPMS.DTOs
{
    public class WarehouseCreateDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class WarehouseUpdateDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class WarehouseResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int TotalMedicineTypes { get; set; }
        public int TotalStockQuantity { get; set; }
    }
}
