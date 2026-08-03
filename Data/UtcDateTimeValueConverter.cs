using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TMPMS.Data
{
    /// <summary>
    /// SQL Server datetime2 không lưu DateTimeKind, nên khi đọc lại EF Core trả
    /// Kind=Unspecified và System.Text.Json serialize KHÔNG kèm "Z" -> FE hiểu
    /// nhầm giờ UTC là giờ local (lệch UTC+7).
    /// Converter này ghi qua nguyên vẹn (giá trị đã là UTC tại thời điểm tạo) và
    /// khi đọc lại đánh dấu Kind=Utc để JSON luôn kèm "Z".
    /// </summary>
    public static class UtcDateTimeValueConverter
    {
        public static readonly ValueConverter<DateTime, DateTime> Instance =
            new ValueConverter<DateTime, DateTime>(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
