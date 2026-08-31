namespace TMPMS.Utils
{
    // Kiểm tra nội dung file thật (magic bytes) thay vì chỉ tin đuôi file client tự khai — trước đây
    // các endpoint upload ảnh (Prescription/Appointment/TongueAnalysis/MedicinesController) chỉ check
    // Path.GetExtension(file.FileName), cho phép đổi tên 1 file bất kỳ (vd .html chứa script) thành
    // .png rồi upload, được lưu và serve công khai qua /uploads/... Cùng logic ProductImportController
    // đã dùng đúng, tách ra dùng chung.
    public static class ImageMagicBytes
    {
        public static bool LooksLikeImage(byte[] data)
        {
            if (data.Length < 4) return false;
            if (data[0] == 0xFF && data[1] == 0xD8) return true;          // JPEG
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E) return true; // PNG
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46) return true; // GIF
            if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46) return true; // RIFF (WEBP)
            if (data[0] == 0x42 && data[1] == 0x4D) return true;          // BMP
            return false;
        }
    }
}
