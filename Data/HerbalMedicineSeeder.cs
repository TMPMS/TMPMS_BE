using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace TMPMS.Data
{
    public static class HerbalMedicineSeeder
    {
        public static async Task SeedAsync(TMPMSDbContext context)
        {
            // 1. Ensure Category 'Thuốc Đông Y - Thảo dược' exists
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Thuốc Đông Y - Thảo dược");
            if (category == null)
            {
                category = new Category
                {
                    Name = "Thuốc Đông Y - Thảo dược",
                    Description = "Danh mục thảo dược, vị thuốc Đông Y cổ truyền bán theo gram/kg"
                };
                context.Categories.Add(category);
                await context.SaveChangesAsync();
            }

            // 2. Ensure Supplier exists
            var supplier = await context.Suppliers.FirstOrDefaultAsync();
            if (supplier == null)
            {
                supplier = new Supplier
                {
                    CompanyName = "Công ty Dược Liệu Trung Ương 1",
                    ContactPerson = "Nguyễn Văn Hùng",
                    Email = "lh@duoclieutw1.vn",
                    Phone = "02438254123",
                    Address = "Số 138 Giảng Võ, Ba Đình, Hà Nội",
                    TaxCode = "0100108921",
                    Status = "Active"
                };
                context.Suppliers.Add(supplier);
                await context.SaveChangesAsync();
            }

            var herbList = new (string Name, string Tier, decimal PriceGram)[]
            {
                ("Cẩu Tích", "Thông dụng", 235.0m),
                ("Cốt Toái Bổ", "Thông dụng", 145.0m),
                ("Bạch Chỉ", "Thông dụng", 240.0m),
                ("Sinh Địa", "Thông dụng", 230.0m),
                ("Bạc Hà", "Phổ thông", 80.0m),
                ("Bồ Công Anh", "Phổ thông", 75.0m),
                ("Sài Đất", "Phổ thông", 105.0m),
                ("Táo Nhân", "Thông dụng", 210.0m),
                ("Kinh Giới", "Phổ thông", 65.0m),
                ("Hoài Sơn", "Thông dụng", 190.0m),
                ("Kỷ Tử", "Quý / bổ dưỡng", 320.0m),
                ("Hà Thủ Ô", "Quý / bổ dưỡng", 390.0m),
                ("Thảo Quyết Minh", "Thông dụng", 130.0m),
                ("Đào Nhân", "Thông dụng", 195.0m),
                ("Đại Hồi", "Phổ thông", 70.0m),
                ("Ngọc Trúc", "Thông dụng", 175.0m),
                ("Thổ Phục Linh", "Thông dụng", 160.0m),
                ("Táo Đen", "Thông dụng", 165.0m),
                ("Thục Địa", "Thông dụng", 225.0m),
                ("Xuyên Khung", "Thông dụng", 135.0m),
                ("Bạch Thược", "Thông dụng", 210.0m),
                ("Đỗ Trọng", "Quý / bổ dưỡng", 440.0m),
                ("Quy Bản", "Quý / bổ dưỡng", 390.0m),
                ("Sa Sâm", "Quý / bổ dưỡng", 310.0m),
                ("Hoàng Kỳ", "Quý / bổ dưỡng", 350.0m),
                ("Bạch Truật", "Thông dụng", 155.0m),
                ("Bạch Linh (Phục Linh)", "Thông dụng", 210.0m),
                ("Quế Chi", "Thông dụng", 220.0m),
                ("Ngưu Tất Bắc", "Thông dụng", 195.0m),
                ("Cam Thảo Bắc", "Thông dụng", 250.0m),
                ("Cam Thảo Nam", "Phổ thông", 100.0m),
                ("Mộc Hương", "Thông dụng", 170.0m),
                ("Phòng Phong", "Thông dụng", 225.0m),
                ("Mẫu Đơn Bì", "Thông dụng", 135.0m),
                ("Ngũ Vị Tử", "Thông dụng", 220.0m),
                ("Bạch Tật Lê", "Thông dụng", 170.0m),
                ("Chi Tử", "Phổ thông", 65.0m),
                ("Viễn Chí", "Thông dụng", 145.0m),
                ("Sơn Thù", "Thông dụng", 225.0m),
                ("Hương Phụ", "Thông dụng", 195.0m),
                ("Thiên Niên Kiện", "Thông dụng", 245.0m),
                ("Xuyên Tâm Liên", "Thông dụng", 220.0m),
                ("Tục Đoạn", "Thông dụng", 195.0m),
                ("Xáo Tam Phân", "Thông dụng", 245.0m),
                ("Kim Ngân Hoa", "Phổ thông", 90.0m),
                ("Quả La Hán", "Phổ thông", 110.0m),
                ("Nhục Thung Dung", "Quý / bổ dưỡng", 390.0m),
                ("Trần Bì", "Phổ thông", 115.0m),
                ("Hoa Hòe", "Phổ thông", 75.0m),
                ("Bán Chi Liên", "Thông dụng", 205.0m),
                ("Xích Thược", "Thông dụng", 230.0m),
                ("Khoan Cân Đằng", "Thông dụng", 130.0m),
                ("Giao Đằng", "Thông dụng", 195.0m),
                ("Bạch Hoa Xà (Thiệt Thảo)", "Thông dụng", 175.0m),
                ("Giảo Cổ Lam", "Thông dụng", 165.0m),
                ("Bồ Kết", "Phổ thông", 90.0m),
                ("Hương Nhu", "Phổ thông", 100.0m),
                ("Mật Nhân", "Thông dụng", 245.0m),
                ("Tam Thất Bắc (củ)", "Cao cấp / hiếm", 820.0m),
                ("Ba Kích (củ)", "Cao cấp / hiếm", 730.0m),
                ("Dâm Dương Hoắc", "Thông dụng", 165.0m),
                ("Mộc Miết Tử", "Thông dụng", 130.0m),
                ("Thương Truật", "Thông dụng", 205.0m),
                ("Đại Phúc Bì", "Thông dụng", 140.0m),
                ("Đại Phúc Tử", "Thông dụng", 140.0m),
                ("Đại Hoàng", "Thông dụng", 210.0m),
                ("Khổ Sâm", "Thông dụng", 150.0m),
                ("Đan Sâm", "Quý / bổ dưỡng", 310.0m),
                ("Đảng Sâm", "Quý / bổ dưỡng", 405.0m),
                ("Phúc Bổn Tử", "Thông dụng", 205.0m),
                ("Hoàng Liên", "Quý / bổ dưỡng", 405.0m),
                ("Hoàng Cầm", "Thông dụng", 220.0m),
                ("Hạnh Nhân", "Thông dụng", 220.0m),
                ("Liên Kiều", "Thông dụng", 155.0m),
                ("Tỳ Giải", "Thông dụng", 200.0m),
                ("Mạch Môn Đông", "Thông dụng", 180.0m),
                ("Mộc Qua", "Thông dụng", 120.0m),
                ("Khiếm Thực", "Thông dụng", 125.0m),
                ("Cửu Thái Tử (hạt hẹ)", "Thông dụng", 195.0m),
                ("Bình Lang (Tân Lang)", "Thông dụng", 230.0m),
                ("Ý Dĩ", "Thông dụng", 145.0m),
                ("Uy Linh Tiên", "Thông dụng", 245.0m),
                ("Huyền Sâm", "Quý / bổ dưỡng", 335.0m),
                ("Tế Tân", "Thông dụng", 130.0m),
                ("Ích Trí Nhân", "Thông dụng", 150.0m),
                ("Tri Mẫu", "Thông dụng", 225.0m),
                ("Đương Quy", "Quý / bổ dưỡng", 420.0m),
                ("Miết Giáp", "Thông dụng", 140.0m),
                ("Thương Nhĩ Tử", "Phổ thông", 115.0m),
                ("Long Nhãn", "Quý / bổ dưỡng", 350.0m),
                ("Tần Giao", "Thông dụng", 195.0m),
                ("Tân Di", "Thông dụng", 150.0m),
                ("Quế Nhục", "Thông dụng", 240.0m),
                ("Khương Hoàng (Nghệ Vàng)", "Phổ thông", 75.0m),
                ("Địa Long", "Thông dụng", 230.0m),
                ("Trạch Tả", "Thông dụng", 120.0m),
                ("Phá Cố Chỉ", "Thông dụng", 225.0m),
                ("Độc Hoạt", "Thông dụng", 170.0m),
                ("Tỳ Bà Diệp", "Phổ thông", 75.0m),
                ("Xà Sàng Tử", "Thông dụng", 245.0m),
                ("Tiểu Hồi", "Thông dụng", 225.0m),
                ("Tiền Hồ", "Thông dụng", 205.0m),
                ("Mẫu Lệ", "Thông dụng", 160.0m),
                ("Thuyền Thoái", "Thông dụng", 210.0m),
                ("Thần Khúc", "Thông dụng", 125.0m),
                ("Thăng Ma", "Thông dụng", 205.0m),
                ("Sài Hồ", "Thông dụng", 130.0m),
                ("Chu Sa", "Quý / bổ dưỡng", 250.0m),
                ("Huyết Giác", "Thông dụng", 160.0m),
                ("Ngưu Bàng Tử", "Phổ thông", 120.0m),
                ("Ngũ Bội Tử", "Thông dụng", 220.0m),
                ("Ngô Thù Du", "Thông dụng", 135.0m),
                ("Một Dược", "Quý / bổ dưỡng", 305.0m),
                ("Mộc Thông", "Thông dụng", 185.0m),
                ("Ích Mẫu", "Phổ thông", 110.0m),
                ("Long Não", "Thông dụng", 180.0m),
                ("Liên Tâm", "Phổ thông", 105.0m),
                ("Khoản Đông Hoa", "Thông dụng", 140.0m),
                ("Huyền Hồ", "Thông dụng", 235.0m),
                ("Bá Tử Nhân", "Thông dụng", 215.0m),
                ("Hồ Đào Nhân", "Thông dụng", 190.0m),
                ("Hoàng Bá", "Thông dụng", 175.0m),
                ("Hạ Khô Thảo", "Phổ thông", 85.0m),
                ("Đinh Hương", "Thông dụng", 195.0m),
                ("Tang Tiêu Phiêu", "Thông dụng", 170.0m),
                ("Cúc Vàng", "Phổ thông", 105.0m),
                ("Cát Cánh", "Thông dụng", 180.0m),
                ("Bồ Hoàng", "Phổ thông", 65.0m),
                ("Trắc Bách Diệp", "Phổ thông", 105.0m),
                ("Hy Thiêm", "Phổ thông", 95.0m),
                ("Huyết Dụ", "Phổ thông", 65.0m),
                ("Địa Liền", "Phổ thông", 90.0m),
                ("Cốt Khí", "Phổ thông", 70.0m),
                ("Hoắc Hương", "Phổ thông", 60.0m),
                ("Hồng Hoa", "Thông dụng", 235.0m),
                ("Long Cốt", "Thông dụng", 125.0m),
                ("Sa Uyển Tử", "Thông dụng", 165.0m),
                ("Bán Hạ", "Thông dụng", 180.0m),
                ("Thỏ Ty Tử", "Thông dụng", 180.0m),
                ("Xa Tiền Tử", "Phổ thông", 75.0m),
                ("Nữ Trinh Tử", "Thông dụng", 225.0m),
                ("Nhân Sâm", "Cao cấp / hiếm", 975.0m),
                ("Kha Tử", "Thông dụng", 130.0m),
                ("Tang Ký Sinh", "Phổ thông", 115.0m),
            };

            bool changes = false;
            foreach (var item in herbList)
            {
                var existingMed = await context.Medicines.FirstOrDefaultAsync(m => m.Name == item.Name);
                if (existingMed == null)
                {
                    var med = new Medicine
                    {
                        CategoryId = category.Id,
                        SupplierId = supplier.Id,
                        Name = item.Name,
                        Description = $"Vị thuốc Đông Y {item.Name} ({item.Tier}) - Thảo dược thiên nhiên đạt chuẩn chất lượng.",
                        Price = item.PriceGram,
                        StockQuantity = 0,
                        Unit = "gram",
                        ImageUrl = "https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=500",
                        ManufactureDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddYears(2),
                        RequiresPrescription = false,
                        CreatedAt = DateTime.Now
                    };
                    context.Medicines.Add(med);
                    await context.SaveChangesAsync();

                    var info = new HerbalMedicineInfo
                    {
                        MedicineId = med.Id,
                        OriginPlace = "",
                        PartUsed = "",
                        Properties = "",
                        Effects = "",
                        UsageInstructions = "",
                        Dosage = "",
                        Contraindications = "",
                        PreservationMethod = ""
                    };
                    context.HerbalMedicineInfos.Add(info);
                    changes = true;
                }
            }

            if (changes)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
