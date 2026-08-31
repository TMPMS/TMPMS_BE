namespace TMPMS.Utils
{
    // Ký tự đầu ô Excel khiến trình đọc hiểu là công thức (=, +, -, @) — dữ liệu do người dùng tự
    // nhập (tên nhân viên, tên/mô tả sản phẩm...) có thể chứa các ký tự này và bị Excel thực thi như
    // công thức khi mở file xuất ra (Excel/CSV formula injection). Thêm ' đứng trước để Excel luôn
    // hiểu ô đó là text thuần thay vì công thức.
    public static class ExcelCellSanitizer
    {
        private static readonly char[] DangerousLeadingChars = { '=', '+', '-', '@' };

        public static string SafeText(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
            return System.Array.IndexOf(DangerousLeadingChars, value[0]) >= 0 ? "'" + value : value;
        }
    }
}
