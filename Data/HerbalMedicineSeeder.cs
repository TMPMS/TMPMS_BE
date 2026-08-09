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
        // Icon lá thảo dược vẽ tay (SVG nhúng trực tiếp) — dùng thay cho ảnh Unsplash hotlink cũ
        // (vốn vô tình là ảnh đồng hồ thông minh, không liên quan tới dược liệu).
        private static readonly string DefaultHerbImage = "data:image/svg+xml;utf8," + Uri.EscapeDataString(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 200 200\"><rect width=\"200\" height=\"200\" rx=\"12\" fill=\"#f0fdf4\"/><path d=\"M100 58c-20 10-32 30-32 50 0 14 10 24 22 24h20c12 0 22-10 22-24 0-20-12-40-32-50z\" fill=\"#059669\" opacity=\"0.35\"/><path d=\"M100 66v66\" stroke=\"#059669\" stroke-width=\"4\" opacity=\"0.5\" stroke-linecap=\"round\"/><rect x=\"62\" y=\"146\" width=\"76\" height=\"12\" rx=\"6\" fill=\"#059669\" opacity=\"0.2\"/></svg>"
        );

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

            // 2. Ensure Rich Suppliers exist
            var suppliersList = await context.Suppliers.ToListAsync();
            var supplierNamesToEnsure = new[]
            {
                ("Công ty Cổ phần Traphaco", "Nguyễn Văn A", "traphaco@gmail.com", "0243681161", "75 Yên Ninh, Ba Đình, Hà Nội", "0100108656"),
                ("Công ty TNHH Dược phẩm OPC", "Trần Thị B", "opc@opcpharma.com", "0283960124", "1017 Hồng Bàng, Quận 6, TP. HCM", "0302560112"),
                ("Công ty Cổ phần Bách Thảo Dược", "Lê Văn C", "contact@bachthaoduoc.com.vn", "0225381881", "Lô Q-6, KCN Tràng Duệ, Hải Phòng", "0201882654"),
                ("Nhà sâm KGC Hàn Quốc (Cheong Kwan Jang)", "Kim Min Woo", "kgc_global@kgc.co.kr", "+82-2-2189-6100", "Seoul, Hàn Quốc", "FOREIGN-001"),
                ("Công ty Cổ phần Dược phẩm Thái Minh", "Phạm Văn D", "contact@thaiminh.com.vn", "02432003300", "Cầu Giấy, Hà Nội", "0108119922"),
                ("Công ty Cổ phần Dược Hậu Giang (DHG Pharma)", "Nguyễn Thị E", "dhgpharma@dhgpharma.com.vn", "02923891433", "Cần Thơ, Việt Nam", "1800156711"),
                ("Công ty Cổ phần Dược phẩm Imexpharm", "Hoàng Văn F", "imexpharm@imexpharm.com", "02773851941", "Đồng Tháp, Việt Nam", "1400101928"),
                ("Tập đoàn Dược phẩm Sanofi (Pháp)", "Jean Dupont", "contact@sanofi.fr", "+33-1-53774000", "Paris, Pháp", "FOREIGN-002")
            };

            foreach (var supData in supplierNamesToEnsure)
            {
                if (!suppliersList.Any(s => s.CompanyName == supData.Item1))
                {
                    var newSup = new Supplier
                    {
                        CompanyName = supData.Item1,
                        ContactPerson = supData.Item2,
                        Email = supData.Item3,
                        Phone = supData.Item4,
                        Address = supData.Item5,
                        TaxCode = supData.Item6,
                        Status = "Active"
                    };
                    context.Suppliers.Add(newSup);
                }
            }
            await context.SaveChangesAsync();
            suppliersList = await context.Suppliers.ToListAsync();

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

                // Bổ sung các vị thuộc nhóm "Thập Bát Phản" (kỵ nhau) phục vụ tính năng
                // AI cảnh báo tương tác vị thuốc cho Pharmacy khi kê đơn.
                ("Cam Toại", "Thông dụng", 260.0m),
                ("Đại Kích", "Thông dụng", 275.0m),
                ("Nguyên Hoa", "Thông dụng", 270.0m),
                ("Hải Tảo", "Thông dụng", 180.0m),
                ("Côn Bố", "Phổ thông", 95.0m),
                ("Ô Đầu", "Thông dụng", 290.0m),
                ("Phụ Tử", "Thông dụng", 285.0m),
                ("Bối Mẫu", "Quý / bổ dưỡng", 360.0m),
                ("Qua Lâu", "Thông dụng", 175.0m),
                ("Bạch Liễm", "Thông dụng", 210.0m),
                ("Bạch Cập", "Thông dụng", 245.0m),
                ("Lê Lô", "Thông dụng", 160.0m),
            };

            var mainWarehouse = await context.Warehouses.FirstOrDefaultAsync();

            int supplierIdx = 0;
            bool changes = false;
            foreach (var item in herbList)
            {
                var existingMed = await context.Medicines.FirstOrDefaultAsync(m => m.Name == item.Name);
                if (existingMed == null)
                {
                    var assignedSupplier = suppliersList[supplierIdx % suppliersList.Count];
                    supplierIdx++;

                    const int initialStockQty = 5000;

                    var med = new Medicine
                    {
                        CategoryId = category.Id,
                        SupplierId = assignedSupplier.Id,
                        Name = item.Name,
                        Description = $"Vị thuốc Đông Y {item.Name} ({item.Tier}) - Thảo dược thiên nhiên đạt chuẩn chất lượng từ {assignedSupplier.CompanyName}.",
                        Price = item.PriceGram,
                        StockQuantity = mainWarehouse != null ? initialStockQty : 0,
                        Unit = "gram",
                        ImageUrl = "https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=500",
                        ManufactureDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddYears(2),
                        RequiresPrescription = true,
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

                    // Nhập kho ban đầu (StockBatch + InventoryStock) — nếu không, hệ thống quản lý tồn kho
                    // theo lô sẽ đồng bộ Medicine.StockQuantity về 0 ở lần cập nhật tồn kho kế tiếp.
                    if (mainWarehouse != null)
                    {
                        context.StockBatches.Add(new StockBatch
                        {
                            MedicineId = med.Id,
                            WarehouseId = mainWarehouse.Id,
                            SupplierId = assignedSupplier.Id,
                            BatchNumber = $"INIT-{med.Id}",
                            ManufactureDate = med.ManufactureDate,
                            ExpiryDate = med.ExpiryDate,
                            QuantityReceived = initialStockQty,
                            QuantityRemaining = initialStockQty,
                            UnitCostPrice = item.PriceGram,
                            ReceivedAt = DateTime.Now,
                            Status = StockBatchStatus.Active,
                            Note = "Lô khởi tạo tự động khi seed vị thuốc mới"
                        });
                        context.InventoryStocks.Add(new InventoryStock
                        {
                            MedicineId = med.Id,
                            WarehouseId = mainWarehouse.Id,
                            Quantity = initialStockQty
                        });
                    }

                    changes = true;
                }
            }

            // Backfill tồn kho cho các vị thuốc "Thập Bát Phản" đã được tạo ở lần chạy trước đây
            // (trước khi seeder này có logic tạo StockBatch/InventoryStock) nhưng vẫn còn StockQuantity = 0.
            if (mainWarehouse != null)
            {
                var phanHerbNames = new[] { "Cam Toại", "Đại Kích", "Nguyên Hoa", "Hải Tảo", "Côn Bố", "Ô Đầu", "Phụ Tử", "Bối Mẫu", "Qua Lâu", "Bạch Liễm", "Bạch Cập", "Lê Lô" };
                var toBackfill = await context.Medicines
                    .Where(m => phanHerbNames.Contains(m.Name) && m.StockQuantity == 0)
                    .ToListAsync();

                foreach (var med in toBackfill)
                {
                    var hasBatch = await context.StockBatches.AnyAsync(b => b.MedicineId == med.Id);
                    if (hasBatch) continue;

                    const int backfillQty = 5000;
                    context.StockBatches.Add(new StockBatch
                    {
                        MedicineId = med.Id,
                        WarehouseId = mainWarehouse.Id,
                        SupplierId = med.SupplierId,
                        BatchNumber = $"INIT-{med.Id}",
                        ManufactureDate = med.ManufactureDate,
                        ExpiryDate = med.ExpiryDate,
                        QuantityReceived = backfillQty,
                        QuantityRemaining = backfillQty,
                        UnitCostPrice = med.Price,
                        ReceivedAt = DateTime.Now,
                        Status = StockBatchStatus.Active,
                        Note = "Lô backfill tự động cho vị thuốc Thập Bát Phản seed trước đó"
                    });
                    context.InventoryStocks.Add(new InventoryStock
                    {
                        MedicineId = med.Id,
                        WarehouseId = mainWarehouse.Id,
                        Quantity = backfillQty
                    });
                    med.StockQuantity = backfillQty;
                    changes = true;
                }
            }

            // Diversify supplier IDs for any existing medicines — nhưng CHỈ khi TẤT CẢ medicine hiện tại
            // vẫn còn ở supplier mặc định/chưa gán (<=1), tức là chưa từng chạy diversify lần nào.
            // Trước đây điều kiện này chạy lại mỗi lần khởi động app và ghi đè bất kỳ medicine nào có
            // SupplierId <= 1 — kể cả khi Dược sĩ/Admin đã CỐ Ý gán lại supplier #1 (nhà cung cấp thật,
            // hợp lệ) sau lần seed đầu tiên. Chỉ chạy 1 lần cho dữ liệu hoàn toàn mới để tránh ghi đè dữ liệu thật.
            var allExistingMeds = await context.Medicines.ToListAsync();
            if (allExistingMeds.Count > 0 && allExistingMeds.All(m => m.SupplierId <= 1))
            {
                int supCounter = 0;
                foreach (var m in allExistingMeds)
                {
                    m.SupplierId = suppliersList[supCounter % suppliersList.Count].Id;
                    supCounter++;
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
