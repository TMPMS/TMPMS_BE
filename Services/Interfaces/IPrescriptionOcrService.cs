using System.Threading.Tasks;
using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IPrescriptionOcrService
    {
        // Trả về null nếu AI không đọc được ảnh hoặc chưa cấu hình API key.
        Task<PrescriptionOcrRawResult?> ExtractAsync(byte[] imageBytes, string mimeType);
    }
}
