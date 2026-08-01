using System;

namespace TMPMS.DTOs
{
    public class HerbalMedicineCreateDTO
    {
        // Thông tin thuốc gốc
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ImageUrl { get; set; }

        // Thông tin thảo dược
        public string OriginPlace { get; set; }
        public string PartUsed { get; set; }
        public string Properties { get; set; }
        public string Effects { get; set; }
        public string UsageInstructions { get; set; }
        public string Dosage { get; set; }
        public string Contraindications { get; set; }
        public string PreservationMethod { get; set; }
    }

    public class HerbalMedicineUpdateDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string OriginPlace { get; set; }
        public string PartUsed { get; set; }
        public string Properties { get; set; }
        public string Effects { get; set; }
        public string UsageInstructions { get; set; }
        public string Dosage { get; set; }
        public string Contraindications { get; set; }
        public string PreservationMethod { get; set; }
    }

    public class HerbalMedicineResponseDTO
    {
        public int MedicineId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public string OriginPlace { get; set; }
        public string PartUsed { get; set; }
        public string Properties { get; set; }
        public string Effects { get; set; }
        public string UsageInstructions { get; set; }
        public string Dosage { get; set; }
        public string Contraindications { get; set; }
        public string PreservationMethod { get; set; }
    }
}
