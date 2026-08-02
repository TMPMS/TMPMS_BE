namespace BusinessObjects
{
    // Thông tin chuyên biệt cho thuốc thảo dược (Đông y)
    public class HerbalMedicineInfo
    {
        public int Id { get; set; }
        public int? MedicineId { get; set; }
        public string OriginPlace { get; set; }        // Nguồn gốc / xuất xứ
        public string PartUsed { get; set; }           // Bộ phận dùng (rễ, lá, hoa, thân...)
        public string Properties { get; set; }         // Tính vị theo Đông y
        public string Effects { get; set; }             // Công dụng
        public string UsageInstructions { get; set; }  // Cách dùng / sắc thuốc
        public string Dosage { get; set; }              // Liều dùng khuyến nghị
        public string Contraindications { get; set; }  // Chống chỉ định / kiêng kỵ
        public string PreservationMethod { get; set; } // Cách bảo quản

        public Medicine? Medicine { get; set; }
    }
}
