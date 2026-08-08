using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMPMS.Migrations
{
    /// <inheritdoc />
    public partial class SeedVouchersAndWheelPrizeTemplates : Migration
    {
        private static readonly string[] Columns = new[]
        {
            "Code", "Name", "DiscountType", "DiscountValue", "MinOrderValue", "MaxDiscount",
            "StartDate", "EndDate", "UsageLimit", "UsedCount", "IsActive", "CreatedAt",
            "Type", "OwnerUserId", "IsWheelPrize", "Weight"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc);

            // Voucher công khai — đa dạng theo loại (sản phẩm/ship) và bậc giá trị đơn tối thiểu,
            // mức giảm trong khoảng 10.000–30.000đ. Thay thế 3 mã demo hardcode cũ
            // (THAIMINH50/LONGCHAU10/FREESHIP) bằng voucher thật trong DB.
            migrationBuilder.InsertData(
                table: "Vouchers",
                columns: Columns,
                values: new object[,]
                {
                    { "SP10K", "Giảm 10.000đ cho đơn từ 100.000đ", "flat", 10000m, 100000m, null, now, null, 1000, 0, true, now, "product", null, false, 0 },
                    { "SP20K", "Giảm 20.000đ cho đơn từ 200.000đ", "flat", 20000m, 200000m, null, now, null, 1000, 0, true, now, "product", null, false, 0 },
                    { "SP30K", "Giảm 30.000đ cho đơn từ 300.000đ", "flat", 30000m, 300000m, null, now, null, 1000, 0, true, now, "product", null, false, 0 },
                    { "SPSALE", "Giảm 5% (tối đa 25.000đ) cho đơn từ 150.000đ", "percent", 5m, 150000m, 25000m, now, null, 1000, 0, true, now, "product", null, false, 0 },
                    { "SHIP10K", "Giảm 10.000đ phí vận chuyển", "flat", 10000m, 0m, null, now, null, 1000, 0, true, now, "shipping", null, false, 0 },
                    { "SHIP20K", "Giảm 20.000đ phí vận chuyển cho đơn từ 150.000đ", "flat", 20000m, 150000m, null, now, null, 1000, 0, true, now, "shipping", null, false, 0 },
                    { "SHIP30K", "Giảm 30.000đ phí vận chuyển cho đơn từ 300.000đ", "flat", 30000m, 300000m, null, now, null, 1000, 0, true, now, "shipping", null, false, 0 },

                    // Mẫu phần thưởng vòng quay may mắn (IsWheelPrize = true) — không nhập tay được,
                    // khi trúng thưởng server sẽ nhân bản thành voucher cá nhân riêng cho người thắng.
                    // Có tier 15k/25k riêng không có ở mã công khai để vòng quay "đặc biệt" hơn.
                    { "WHEEL-TPL-P10", "Vòng quay: Giảm 10.000đ sản phẩm", "flat", 10000m, 0m, null, now, null, 999999, 0, true, now, "product", null, true, 30 },
                    { "WHEEL-TPL-P15", "Vòng quay: Giảm 15.000đ sản phẩm", "flat", 15000m, 0m, null, now, null, 999999, 0, true, now, "product", null, true, 25 },
                    { "WHEEL-TPL-P20", "Vòng quay: Giảm 20.000đ sản phẩm", "flat", 20000m, 0m, null, now, null, 999999, 0, true, now, "product", null, true, 20 },
                    { "WHEEL-TPL-P25", "Vòng quay: Giảm 25.000đ sản phẩm", "flat", 25000m, 0m, null, now, null, 999999, 0, true, now, "product", null, true, 15 },
                    { "WHEEL-TPL-P30", "Vòng quay: Giảm 30.000đ sản phẩm", "flat", 30000m, 0m, null, now, null, 999999, 0, true, now, "product", null, true, 10 },
                    { "WHEEL-TPL-S10", "Vòng quay: Giảm 10.000đ phí ship", "flat", 10000m, 0m, null, now, null, 999999, 0, true, now, "shipping", null, true, 30 },
                    { "WHEEL-TPL-S20", "Vòng quay: Giảm 20.000đ phí ship", "flat", 20000m, 0m, null, now, null, 999999, 0, true, now, "shipping", null, true, 20 },
                    { "WHEEL-TPL-S30", "Vòng quay: Giảm 30.000đ phí ship", "flat", 30000m, 0m, null, now, null, 999999, 0, true, now, "shipping", null, true, 10 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Vouchers",
                keyColumn: "Code",
                keyValues: new object[]
                {
                    "SP10K", "SP20K", "SP30K", "SPSALE", "SHIP10K", "SHIP20K", "SHIP30K",
                    "WHEEL-TPL-P10", "WHEEL-TPL-P15", "WHEEL-TPL-P20", "WHEEL-TPL-P25", "WHEEL-TPL-P30",
                    "WHEEL-TPL-S10", "WHEEL-TPL-S20", "WHEEL-TPL-S30"
                });
        }
    }
}
