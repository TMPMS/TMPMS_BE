namespace TMPMS.Models
{
    public class PharmacyStore
    {
        public int Id { get; set; }
        public string ProvinceId { get; set; } = string.Empty; // "hn", "hcm", "dn"
        public string ProvinceName { get; set; } = string.Empty; // "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng"
        public string DistrictId { get; set; } = string.Empty; // "hc", "tk", "cg", "dd", "nhs", etc.
        public string DistrictName { get; set; } = string.Empty; // "Quận Hải Châu", "Quận Thanh Khê", etc.
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Hours { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
