using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class PrescriptionItemCreateDTO
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public string? Instructions { get; set; }
    }

    public class PrescriptionCreateDTO
    {
        public int UserId { get; set; }
        public int? PatientId { get; set; }
        public int? DiagnosisId { get; set; }
        public string? DiagnosisNote { get; set; }
        public int? DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public string DoctorName { get; set; }
        public string Hospital { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string? ImageUrl { get; set; } = "";
        public List<PrescriptionItemCreateDTO> Items { get; set; } = new();
    }

    public class PrescriptionStatusUpdateDTO
    {
        // Pending, Approved, Rejected, Fulfilled
        public string Status { get; set; }
        public string? RejectReason { get; set; }
    }

    public class PrescriptionItemResponseDTO
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? Instructions { get; set; }
    }

    public class PrescriptionResponseDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? PatientId { get; set; }
        public string PatientName { get; set; }
        // true khi tài khoản gửi/tạo đơn (UserId) khác với bệnh nhân thực sự dùng thuốc (PatientId) —
        // ví dụ gửi toa hộ người thân. FE dùng để hiển thị rõ "gửi bởi" vs "khám cho".
        public bool IsSubmittedForOther { get; set; }
        public int? DiagnosisId { get; set; }
        public string DiagnosisNote { get; set; }
        public int? DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public string DoctorName { get; set; }
        public string Hospital { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public List<PrescriptionItemResponseDTO> Items { get; set; } = new();
    }

    // Dữ liệu Dược sĩ nhập để hoàn thiện (kê đơn) một đơn thuốc "Pending" mà khách hàng
    // đã gửi kèm ảnh toa thuốc (chưa có PrescriptionItems). Trừ kho & duyệt đơn trong 1 bước.
    public class PrescriptionFinalizeItemDTO
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public string? Instructions { get; set; }
    }

    public class PrescriptionFinalizeDTO
    {
        public List<PrescriptionFinalizeItemDTO> Items { get; set; } = new();
        public string? DoctorName { get; set; }
        public string? Hospital { get; set; }
        public string? DiagnosisNote { get; set; }

        // Dược sĩ đối chiếu & xác nhận đúng hồ sơ bệnh nhân khám (có thể khác tài khoản đã gửi toa
        // nếu khách gửi hộ người thân). Null = giữ nguyên bệnh nhân hiện tại của đơn.
        public int? PatientId { get; set; }
    }

    // Một dòng thuốc do AI đọc được từ ảnh toa thuốc, kèm gợi ý khớp với danh mục dược phẩm hiện có.
    public class PrescriptionOcrItemDto
    {
        public string RawText { get; set; } = "";
        public int? MatchedMedicineId { get; set; }
        public string? MatchedMedicineName { get; set; }
        public int SuggestedQuantity { get; set; } = 1;
    }

    public class PrescriptionOcrResultDto
    {
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? Hospital { get; set; }
        public string? Diagnosis { get; set; }
        public List<PrescriptionOcrItemDto> Items { get; set; } = new();
        public bool IsAiGenerated { get; set; }
        public string? Disclaimer { get; set; }
    }

    // Kết quả đọc ảnh thô từ AI, trước khi đối chiếu tên thuốc với danh mục dược phẩm trong hệ thống
    // (việc đối chiếu cần truy vấn DB nên do PrescriptionService đảm nhiệm, không thuộc OCR service).
    public class PrescriptionOcrRawResult
    {
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? Hospital { get; set; }
        public string? Diagnosis { get; set; }
        public List<string> MedicationLines { get; set; } = new();
    }
}
