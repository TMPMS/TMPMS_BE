using System;

namespace BusinessObjects
{
    // Cặp vị thuốc Đông Y kỵ nhau (Thập Bát Phản / Thập Cửu Úy) — dùng để cảnh báo khi Pharmacy kê đơn
    public class HerbalInteraction
    {
        public int Id { get; set; }

        public int HerbAId { get; set; }
        public int HerbBId { get; set; }

        public string InteractionType { get; set; }     // "ThapBatPhan" / "ThapCuuUy" / "Caution"
        public string Severity { get; set; }             // "Critical" / "Warning" / "Notice"
        public string MechanismDescription { get; set; } // Cơ chế độc tính y khoa

        public int? SuggestedReplacementForAId { get; set; } // Vị thay thế cho HerbA (giữ nguyên HerbB)
        public int? SuggestedReplacementForBId { get; set; } // Vị thay thế cho HerbB (giữ nguyên HerbA)

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Medicine HerbA { get; set; }
        public Medicine HerbB { get; set; }
        public Medicine SuggestedReplacementForA { get; set; }
        public Medicine SuggestedReplacementForB { get; set; }
    }
}
