import openpyxl, sys

wb = openpyxl.load_workbook(r'c:\Users\ngtam\Downloads\đồ án\Danh_sach_gia_duoc_lieu.xlsx')
sheet = wb['Duoc lieu']

items = []
for row in sheet.iter_rows(min_row=2, values_only=True):
    if row[1] is not None:
        name = str(row[1]).strip()
        tier = str(row[2]).strip() if row[2] else 'Thông dụng'
        price_kg = float(row[3]) if row[3] is not None else None
        price_gram = round(price_kg / 1000.0, 2) if price_kg is not None else None
        items.append((name, tier, price_gram))

csharp_code = '''using System;
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
'''

for name, tier, price in items:
    escaped_name = name.replace('"', '\\"')
    escaped_tier = tier.replace('"', '\\"')
    price_str = f'{price}m' if price is not None else '0m'
    csharp_code += f'                ("{escaped_name}", "{escaped_tier}", {price_str}),\n'

csharp_code += '''            };

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
'''

with open(r'c:\Users\ngtam\Downloads\đồ án\TMPMS_BE\Data\HerbalMedicineSeeder.cs', 'w', encoding='utf-8') as f:
    f.write(csharp_code)

print('Successfully regenerated HerbalMedicineSeeder.cs with empty string clinical defaults!')
