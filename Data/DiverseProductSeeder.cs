using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace TMPMS.Data
{
    /// <summary>
    /// Bổ sung sản phẩm cho các danh mục còn mỏng: Dược mỹ phẩm, Chăm sóc cá nhân,
    /// Thiết bị y tế, Châm cứu. Idempotent theo Name, có thể chạy lại an toàn.
    /// </summary>
    public static class DiverseProductSeeder
    {
        private record ProductSeed(
            string CategoryName,
            string CategoryDescription,
            string Name,
            string Description,
            decimal Price,
            decimal? OldPrice,
            int? Discount,
            string Unit,
            string Origin,
            string Packaging,
            bool RequiresPrescription,
            string ImageUrl);

        public static async Task SeedAsync(TMPMSDbContext context)
        {
            var products = new[]
            {
                // ========== Dược mỹ phẩm ==========
                new ProductSeed("Dược mỹ phẩm", "Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu",
                    "Gel Rửa Mặt CeraVe Foaming Cleanser (236ml)",
                    "Làm sạch sâu bã nhờn và bụi bẩn cho da dầu, da mụn, không gây khô căng, chứa ceramide phục hồi hàng rào bảo vệ da.",
                    285000, 320000, 11, "Chai", "Mỹ", "Chai 236ml", false,
                    "https://images.unsplash.com/photo-1597931752949-98c74b5b159f?w=400&h=400&fit=crop"),
                new ProductSeed("Dược mỹ phẩm", "Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu",
                    "Kem Trị Mụn Benzoyl Peroxide 5% La Roche-Posay (30ml)",
                    "Đặc trị mụn viêm, mụn bọc, kháng khuẩn tại chỗ, giảm sưng đỏ và ngăn ngừa mụn tái phát.",
                    350000, 390000, 10, "Tuýp", "Pháp", "Tuýp 30ml", false,
                    "https://images.unsplash.com/photo-1633171036157-78d53387fdc0?w=400&h=400&fit=crop"),
                new ProductSeed("Dược mỹ phẩm", "Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu",
                    "Mặt Nạ Giấy Innisfree Green Tea (Hộp 10 miếng)",
                    "Cấp ẩm, làm dịu da, chống oxy hóa từ chiết xuất trà xanh đảo Jeju, dùng 2-3 lần/tuần.",
                    240000, 270000, 11, "Hộp", "Hàn Quốc", "Hộp 10 miếng x 20ml", false,
                    "https://images.unsplash.com/photo-1753758541974-e9e1d66cfbd9?w=400&h=400&fit=crop"),
                new ProductSeed("Dược mỹ phẩm", "Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu",
                    "Xịt Khoáng Vichy Mineralizing Thermal Water (150ml)",
                    "Làm dịu da tức thì, cấp ẩm và khoáng chất, phù hợp da nhạy cảm, dùng được sau nắng hoặc trang điểm.",
                    310000, 345000, 10, "Chai", "Pháp", "Chai xịt 150ml", false,
                    "https://images.unsplash.com/photo-1556228852-80b6e5eeff06?w=400&h=400&fit=crop"),
                new ProductSeed("Dược mỹ phẩm", "Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu",
                    "Kem Trị Nám Obagi Nu-Derm Clear (28g)",
                    "Hỗ trợ làm mờ nám, tàn nhang, đốm nâu do tăng sắc tố, dùng theo liệu trình buổi tối.",
                    620000, 690000, 10, "Tuýp", "Mỹ", "Tuýp 28g", false,
                    "https://images.unsplash.com/photo-1623143445418-40c192fa3d11?w=400&h=400&fit=crop"),
                new ProductSeed("Dược mỹ phẩm", "Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu",
                    "Sữa Tắm Gội Cho Bé Cetaphil Baby (400ml)",
                    "Dịu nhẹ, không xà phòng, không gây cay mắt, phù hợp da nhạy cảm của trẻ sơ sinh và trẻ nhỏ.",
                    195000, 220000, 11, "Chai", "Mỹ", "Chai 400ml", false,
                    "https://images.unsplash.com/photo-1601049541079-473f79fd3746?w=400&h=400&fit=crop"),
                new ProductSeed("Dược mỹ phẩm", "Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu",
                    "Kem Phục Hồi Da La Roche-Posay Cicaplast Baume B5 (40ml)",
                    "Làm dịu và phục hồi da bị kích ứng, khô nứt, dùng được cho cả mặt và toàn thân, kể cả trẻ sơ sinh.",
                    265000, 295000, 10, "Tuýp", "Pháp", "Tuýp 40ml", false,
                    "https://images.unsplash.com/photo-1576426863848-c21f53c60b19?w=400&h=400&fit=crop"),

                // ========== Chăm sóc cá nhân ==========
                new ProductSeed("Chăm sóc cá nhân", "Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm",
                    "Nước Súc Miệng Listerine Cool Mint (500ml)",
                    "Diệt khuẩn răng miệng, ngăn ngừa mảng bám và hôi miệng, hương bạc hà the mát kéo dài.",
                    89000, 99000, 10, "Chai", "Mỹ", "Chai 500ml", false,
                    "https://images.unsplash.com/photo-1629198688000-71f23e745b6e?w=400&h=400&fit=crop"),
                new ProductSeed("Chăm sóc cá nhân", "Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm",
                    "Dung Dịch Vệ Sinh Phụ Nữ Lactacyd (250ml)",
                    "Cân bằng độ pH, làm sạch dịu nhẹ, phòng ngừa viêm nhiễm phụ khoa, dùng hằng ngày.",
                    105000, 118000, 11, "Chai", "Việt Nam", "Chai 250ml", false,
                    "https://images.unsplash.com/photo-1618479955358-5f8e5ab7d630?w=400&h=400&fit=crop"),
                new ProductSeed("Chăm sóc cá nhân", "Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm",
                    "Khẩu Trang Y Tế 4 Lớp Kháng Khuẩn (Hộp 50 cái)",
                    "Kháng khuẩn, lọc bụi mịn, kháng giọt bắn, đạt tiêu chuẩn y tế, thoáng khí khi đeo lâu.",
                    45000, 52000, 13, "Hộp", "Việt Nam", "Hộp 50 cái", false,
                    "https://images.unsplash.com/photo-1582750433449-648ed127bb54?w=400&h=400&fit=crop"),
                new ProductSeed("Chăm sóc cá nhân", "Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm",
                    "Bông Băng Gạc Y Tế Vô Trùng (Hộp 10 gói)",
                    "Gạc vô trùng dùng sát trùng, băng vết thương hở, an toàn cho da nhạy cảm.",
                    35000, null, null, "Hộp", "Việt Nam", "Hộp 10 gói", false,
                    "https://images.unsplash.com/photo-1600091474842-83bb9c05a723?w=400&h=400&fit=crop"),
                new ProductSeed("Chăm sóc cá nhân", "Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm",
                    "Xà Phòng Diệt Khuẩn Lifebuoy (90g)",
                    "Loại bỏ 99.9% vi khuẩn gây hại, bảo vệ da khỏi các bệnh ngoài da thông thường.",
                    12000, null, null, "Bánh", "Việt Nam", "Bánh 90g", false,
                    "https://images.unsplash.com/photo-1630398777649-cdfc7c5e8a24?w=400&h=400&fit=crop"),
                new ProductSeed("Chăm sóc cá nhân", "Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm",
                    "Dầu Gội Trị Gàu Head & Shoulders (650ml)",
                    "Loại bỏ gàu, giảm ngứa da đầu, làm sạch dầu thừa, tóc chắc khỏe sau mỗi lần gội.",
                    145000, 165000, 12, "Chai", "Việt Nam", "Chai 650ml", false,
                    "https://images.unsplash.com/photo-1551446339-1e5c6f164ec2?w=400&h=400&fit=crop"),
                new ProductSeed("Chăm sóc cá nhân", "Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm",
                    "Lăn Khử Mùi Nivea Men Fresh Active (50ml)",
                    "Khử mùi hiệu quả 48 giờ, hương tươi mát, không gây bết dính hay kích ứng da.",
                    75000, 85000, 12, "Chai", "Đức", "Chai lăn 50ml", false,
                    "https://images.unsplash.com/photo-1617858123189-b26eb62d8eb4?w=400&h=400&fit=crop"),

                // ========== Thiết bị y tế ==========
                new ProductSeed("Thiết bị y tế", "Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà",
                    "Máy Đo Nồng Độ Oxy SpO2 Kẹp Ngón Tay (Cái)",
                    "Đo nhanh chỉ số SpO2 và nhịp mạch, màn hình LED dễ đọc, phù hợp theo dõi sức khỏe tại nhà.",
                    350000, 390000, 10, "Cái", "Trung Quốc", "1 máy + pin", false,
                    "https://images.unsplash.com/photo-1780461159687-281752b8a85a?w=400&h=400&fit=crop"),
                new ProductSeed("Thiết bị y tế", "Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà",
                    "Cân Sức Khỏe Điện Tử Omron (Cái)",
                    "Đo cân nặng chính xác, mặt kính cường lực chống trơn trượt, tự động tắt nguồn khi không dùng.",
                    420000, 470000, 11, "Cái", "Nhật Bản", "1 cân + pin", false,
                    "https://images.unsplash.com/photo-1701937272186-f9de1561e6cd?w=400&h=400&fit=crop"),
                new ProductSeed("Thiết bị y tế", "Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà",
                    "Túi Chườm Nóng Lạnh Đa Năng (Cái)",
                    "Giảm đau nhức cơ, sưng tấy, bong gân; dùng chườm nóng hoặc lạnh tùy nhu cầu điều trị.",
                    85000, 98000, 13, "Cái", "Việt Nam", "1 túi", false,
                    "https://images.unsplash.com/photo-1657160414310-ff042a2eb931?w=400&h=400&fit=crop"),
                new ProductSeed("Thiết bị y tế", "Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà",
                    "Đai Lưng Cột Sống Hỗ Trợ Thoát Vị Đĩa Đệm (Cái)",
                    "Nẹp cố định vùng thắt lưng, giảm áp lực cột sống, hỗ trợ phục hồi sau chấn thương hoặc thoát vị đĩa đệm.",
                    295000, 330000, 11, "Cái", "Việt Nam", "1 đai điều chỉnh size", false,
                    "https://images.unsplash.com/photo-1740689593879-b44e3eeaef31?w=400&h=400&fit=crop"),
                new ProductSeed("Thiết bị y tế", "Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà",
                    "Gậy Chống Người Già 4 Chân Chống Trượt (Cái)",
                    "Hỗ trợ đi lại vững vàng cho người cao tuổi, người sau chấn thương, đế cao su chống trơn trượt.",
                    165000, 185000, 11, "Cái", "Việt Nam", "1 gậy điều chỉnh độ cao", false,
                    "https://images.unsplash.com/photo-1774537556824-00cd1946eb8d?w=400&h=400&fit=crop"),
                new ProductSeed("Thiết bị y tế", "Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà",
                    "Xe Lăn Mini Gấp Gọn Cho Người Già (Cái)",
                    "Khung nhôm nhẹ, gấp gọn tiện di chuyển, có phanh tay và để chân điều chỉnh được.",
                    2450000, 2700000, 9, "Cái", "Việt Nam", "1 xe + phụ kiện", false,
                    "https://images.unsplash.com/photo-1642680936843-b09109c69104?w=400&h=400&fit=crop"),
                new ProductSeed("Thiết bị y tế", "Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà",
                    "Máy Massage Cầm Tay Xung Điện Trị Liệu (Cái)",
                    "Kích thích cơ bằng xung điện, giảm đau mỏi cơ, nhiều chế độ massage tùy chỉnh.",
                    380000, 420000, 10, "Cái", "Trung Quốc", "1 máy + dây điện cực", false,
                    "https://images.unsplash.com/photo-1746278925416-9d6c71f55c2d?w=400&h=400&fit=crop"),

                // ========== Châm cứu ==========
                new ProductSeed("Châm cứu", "Thiết bị châm cứu, cứu ngải, bấm huyệt",
                    "Bộ Kim Châm Cứu Vô Trùng Dùng Một Lần (Hộp 100 kim)",
                    "Kim châm cứu vô trùng, thân thép không gỉ, dùng một lần, đạt chuẩn y tế cho thầy thuốc Đông Y.",
                    120000, 135000, 11, "Hộp", "Việt Nam", "Hộp 100 kim", true,
                    "https://images.unsplash.com/photo-1598555763574-dca77e10427e?w=400&h=400&fit=crop"),
                new ProductSeed("Châm cứu", "Thiết bị châm cứu, cứu ngải, bấm huyệt",
                    "Điếu Ngải Cứu Trị Liệu Nam Dược (Hộp 10 điếu)",
                    "Ngải nhung nguyên chất, dùng cứu ngải làm ấm huyệt đạo, hỗ trợ lưu thông khí huyết.",
                    95000, 108000, 12, "Hộp", "Việt Nam", "Hộp 10 điếu", false,
                    "https://images.unsplash.com/photo-1778040936324-6ae5d7420013?w=400&h=400&fit=crop"),
                new ProductSeed("Châm cứu", "Thiết bị châm cứu, cứu ngải, bấm huyệt",
                    "Bộ Giác Hơi Chân Không 12 Cốc (Bộ)",
                    "Giác hơi chân không tiện lợi, không cần dùng lửa, hỗ trợ giảm đau nhức cơ và lưu thông khí huyết.",
                    280000, 310000, 10, "Bộ", "Việt Nam", "Bộ 12 cốc + súng hút", false,
                    "https://images.unsplash.com/photo-1745327883389-17150e99dcf7?w=400&h=400&fit=crop"),
                new ProductSeed("Châm cứu", "Thiết bị châm cứu, cứu ngải, bấm huyệt",
                    "Dụng Cụ Cạo Gió Sừng Trâu Tự Nhiên (Cái)",
                    "Chế tác từ sừng trâu tự nhiên, dùng cạo gió giải cảm, thư giãn cơ, an toàn cho da.",
                    65000, null, null, "Cái", "Việt Nam", "1 cái", false,
                    "https://images.unsplash.com/photo-1767043088777-1884c2ef6c97?w=400&h=400&fit=crop"),
                new ProductSeed("Châm cứu", "Thiết bị châm cứu, cứu ngải, bấm huyệt",
                    "Đèn Hồng Ngoại Trị Liệu Vật Lý (Cái)",
                    "Chiếu nhiệt hồng ngoại hỗ trợ giảm đau cơ xương khớp, tăng tuần hoàn máu tại chỗ.",
                    650000, 720000, 10, "Cái", "Trung Quốc", "1 đèn + chân đế", false,
                    "https://images.unsplash.com/photo-1702241271926-4f752a29cb4f?w=400&h=400&fit=crop"),
                new ProductSeed("Châm cứu", "Thiết bị châm cứu, cứu ngải, bấm huyệt",
                    "Máy Bấm Huyệt Xung Điện Cầm Tay (Cái)",
                    "Kích thích huyệt đạo bằng xung điện thấp tần, hỗ trợ giảm đau, thư giãn cơ bắp tại nhà.",
                    290000, 320000, 9, "Cái", "Trung Quốc", "1 máy + đầu dò", false,
                    "https://images.unsplash.com/photo-1757039068319-9a2591965a71?w=400&h=400&fit=crop"),
                new ProductSeed("Châm cứu", "Thiết bị châm cứu, cứu ngải, bấm huyệt",
                    "Dầu Xoa Bóp Giảm Đau Khớp Cường Lâm (Chai 100ml)",
                    "Kết hợp tinh dầu quế, gừng, bạc hà giúp giảm đau nhức xương khớp, làm ấm vùng xoa bóp.",
                    58000, 65000, 11, "Chai", "Việt Nam", "Chai 100ml", false,
                    "https://images.unsplash.com/photo-1515377905703-c4788e51af15?w=400&h=400&fit=crop"),
            };

            var categories = new Dictionary<string, Category>();
            var suppliers = await context.Suppliers.ToListAsync();
            var mainWarehouse = await context.Warehouses.FirstOrDefaultAsync();

            int supplierIdx = 0;
            bool changes = false;

            foreach (var item in products)
            {
                if (!categories.TryGetValue(item.CategoryName, out var category))
                {
                    category = await context.Categories.FirstOrDefaultAsync(c => c.Name == item.CategoryName);
                    if (category == null)
                    {
                        category = new Category { Name = item.CategoryName, Description = item.CategoryDescription };
                        context.Categories.Add(category);
                        await context.SaveChangesAsync();
                    }
                    categories[item.CategoryName] = category;
                }

                var exists = await context.Medicines.AnyAsync(m => m.Name == item.Name);
                if (exists) continue;

                var assignedSupplier = suppliers[supplierIdx % suppliers.Count];
                supplierIdx++;

                const int initialStockQty = 100;

                var med = new Medicine
                {
                    CategoryId = category.Id,
                    SupplierId = assignedSupplier.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    OldPrice = item.OldPrice,
                    Discount = item.Discount,
                    StockQuantity = mainWarehouse != null ? initialStockQty : 0,
                    Unit = item.Unit,
                    Origin = item.Origin,
                    Packaging = item.Packaging,
                    ImageUrl = item.ImageUrl,
                    ManufactureDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddYears(2),
                    RequiresPrescription = item.RequiresPrescription,
                    CreatedAt = DateTime.Now
                };
                context.Medicines.Add(med);
                await context.SaveChangesAsync();

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
                        UnitCostPrice = item.Price,
                        ReceivedAt = DateTime.Now,
                        Status = StockBatchStatus.Active,
                        Note = "Lô khởi tạo tự động khi seed sản phẩm đa dạng"
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

            if (changes)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
