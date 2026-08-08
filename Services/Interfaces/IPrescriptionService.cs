using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IPrescriptionService
    {
        Task<PrescriptionResponseDTO> Create(PrescriptionCreateDTO dto);
        Task<PrescriptionResponseDTO> GetById(int id);
        Task<List<PrescriptionResponseDTO>> GetByUser(int userId);
        Task<List<PrescriptionResponseDTO>> GetByStatus(string status);
        Task<List<PrescriptionResponseDTO>> GetAll();
        Task<PrescriptionResponseDTO> UpdateStatus(int id, PrescriptionStatusUpdateDTO dto);
        Task<bool> Delete(int id);

        // Đọc ảnh toa thuốc (ImageUrl đã có sẵn trên đơn) bằng AI và gợi ý khớp với danh mục dược phẩm.
        // Chỉ trả về gợi ý, không ghi vào DB — Dược sĩ xem/chỉnh sửa rồi mới gọi Finalize.
        Task<PrescriptionOcrResultDto> ScanImage(int id);

        // Dược sĩ hoàn thiện (kê đơn) một đơn thuốc "Pending" gửi kèm ảnh: gắn danh sách thuốc,
        // trừ kho và chuyển trạng thái sang Approved trong 1 bước giao dịch.
        Task<PrescriptionResponseDTO> Finalize(int id, PrescriptionFinalizeDTO dto);
    }
}
