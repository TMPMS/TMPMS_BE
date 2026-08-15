using System.Collections.Generic;

namespace TMPMS.DTOs
{
    // Kết quả phân tích ảnh lưỡi (Thiệt chẩn — 1 trong Tứ chẩn của Đông Y: Vọng/Văn/Vấn/Thiết),
    // dùng bổ trợ cho kết quả tự chẩn đoán theo bảng câu hỏi (Vấn chẩn) đã có.
    public class TongueAnalysisResponseDto
    {
        public string TongueColor { get; set; } = "";       // Sắc chất lưỡi (đỏ nhạt, đỏ sẫm, nhợt...)
        public string CoatingColor { get; set; } = "";       // Màu rêu lưỡi (trắng, vàng, xám...)
        public string CoatingThickness { get; set; } = "";   // Độ dày rêu lưỡi (mỏng, dày, không rêu...)
        public string Moisture { get; set; } = "";           // Độ ẩm (khô, ướt, nhuận...)
        public string Observations { get; set; } = "";       // Mô tả tổng quát hình thái lưỡi
        public List<string> RelatedSyndromes { get; set; } = new List<string>(); // Thể bệnh gợi ý liên quan (khớp SyndromeType.Name nếu có)
        public string Recommendation { get; set; } = "";
        public bool IsAiGenerated { get; set; } = true;
        public string Disclaimer { get; set; } =
            "Kết quả phân tích ảnh lưỡi chỉ mang tính tham khảo bước đầu (Thiệt chẩn), không thay thế chẩn đoán trực tiếp của bác sĩ/dược sĩ có chuyên môn. Vui lòng đặt lịch khám nếu triệu chứng kéo dài.";
    }
}
