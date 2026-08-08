using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IMedicineImageSearchService
    {
        // Trả về từ khoá tên sản phẩm đọc được từ ảnh vỏ/hộp thuốc, hoặc null nếu AI không
        // nhận diện được hay chưa cấu hình API key.
        Task<string?> IdentifyAsync(byte[] imageBytes, string mimeType);
    }
}
