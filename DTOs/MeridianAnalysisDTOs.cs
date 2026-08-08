using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class AcupointDto
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Location { get; set; } = "";
        public string Benefit { get; set; } = "";
        public double[] Position { get; set; } = new double[] { 0, 0, 0 };
    }

    public class MeridianAnalysisResponseDto
    {
        public string Nature { get; set; } = "";
        public List<string> Meridians { get; set; } = new List<string>();
        public string Functions { get; set; } = "";
        public List<AcupointDto> Acupoints { get; set; } = new List<AcupointDto>();
        public bool IsAiGenerated { get; set; } = true;
        public string Disclaimer { get; set; } = "Thông tin quy kinh này do AI suy luận dựa trên tên và mô tả sản phẩm, chưa được dược sĩ kiểm chứng. Vui lòng tham khảo ý kiến chuyên môn trước khi sử dụng.";
    }
}
