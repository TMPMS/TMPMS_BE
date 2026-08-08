using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using TMPMS.DTOs;

namespace TMPMS.Data
{
    // Seed dữ liệu "Thập Bát Phản" — các cặp vị thuốc Đông Y kỵ nhau tuyệt đối theo y văn cổ truyền.
    public static class HerbalInteractionSeeder
    {
        public static async Task SeedAsync(TMPMSDbContext context)
        {
            if (await context.HerbalInteractions.AnyAsync()) return;

            const string aconiteMechanism = "Chứa aconitin độc tính cao với tim mạch và thần kinh; phối hợp làm tăng hấp thu/hiệp đồng độc tính, dễ gây loạn nhịp tim, tê liệt thần kinh cấp.";
            const string leiLuMechanism = "Lê Lô có độc gây kích ứng mạnh đường tiêu hóa và thần kinh trung ương; phối hợp với các vị Sâm bổ khí/huyết làm khuếch đại độc tính, gây nôn dữ dội và rối loạn nhịp tim, đồng thời triệt tiêu tác dụng bổ dưỡng.";

            var seedData = new List<HerbalInteractionSeedDTO>
            {
                // Nhóm 1: Cam Thảo phản Cam Toại / Đại Kích / Nguyên Hoa / Hải Tảo
                new() { HerbAName = "Cam Thảo Bắc", HerbBName = "Hải Tảo", InteractionType = "ThapBatPhan", Severity = "Critical",
                    MechanismDescription = "Kết hợp làm tăng giữ nước, gây hạ kali máu nghiêm trọng, biến chứng huyết áp và tăng nguy cơ độc tính với thận.",
                    ReplacementForAName = "Cam Thảo Nam", ReplacementForBName = "Côn Bố" },
                new() { HerbAName = "Cam Thảo Bắc", HerbBName = "Đại Kích", InteractionType = "ThapBatPhan", Severity = "Critical",
                    MechanismDescription = "Đại Kích vốn có độc mạnh với đường tiêu hóa; Cam Thảo làm giảm khả năng đào thải độc tố, gây tích lũy và tăng nguy cơ ngộ độc toàn thân.",
                    ReplacementForAName = "Cam Thảo Nam" },
                new() { HerbAName = "Cam Thảo Bắc", HerbBName = "Cam Toại", InteractionType = "ThapBatPhan", Severity = "Critical",
                    MechanismDescription = "Cam Toại vốn có độc, khi phối với Cam Thảo sinh phản ứng hiệp đồng độc tính, gây đau bụng dữ dội, tiêu chảy và tổn thương gan thận.",
                    ReplacementForAName = "Cam Thảo Nam" },
                new() { HerbAName = "Cam Thảo Bắc", HerbBName = "Nguyên Hoa", InteractionType = "ThapBatPhan", Severity = "Critical",
                    MechanismDescription = "Nguyên Hoa có độc mạnh với gan; Cam Thảo làm biến đổi dược tính khiến độc tính gan tăng cao, dễ gây ngộ độc cấp.",
                    ReplacementForAName = "Cam Thảo Nam" },

                // Nhóm 2: Ô Đầu / Phụ Tử phản Bán Hạ / Qua Lâu / Bối Mẫu / Bạch Liễm / Bạch Cập
                new() { HerbAName = "Ô Đầu", HerbBName = "Bán Hạ", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Ô Đầu", HerbBName = "Qua Lâu", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Ô Đầu", HerbBName = "Bối Mẫu", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Ô Đầu", HerbBName = "Bạch Liễm", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Ô Đầu", HerbBName = "Bạch Cập", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Phụ Tử", HerbBName = "Bán Hạ", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Phụ Tử", HerbBName = "Qua Lâu", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Phụ Tử", HerbBName = "Bối Mẫu", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Phụ Tử", HerbBName = "Bạch Liễm", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },
                new() { HerbAName = "Phụ Tử", HerbBName = "Bạch Cập", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = aconiteMechanism },

                // Nhóm 3: Lê Lô phản Nhân Sâm / Sa Sâm / Đan Sâm / Huyền Sâm / Khổ Sâm / Tế Tân / Bạch Thược
                new() { HerbAName = "Lê Lô", HerbBName = "Nhân Sâm", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = leiLuMechanism },
                new() { HerbAName = "Lê Lô", HerbBName = "Sa Sâm", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = leiLuMechanism },
                new() { HerbAName = "Lê Lô", HerbBName = "Đan Sâm", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = leiLuMechanism },
                new() { HerbAName = "Lê Lô", HerbBName = "Huyền Sâm", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = leiLuMechanism },
                new() { HerbAName = "Lê Lô", HerbBName = "Khổ Sâm", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = leiLuMechanism },
                new() { HerbAName = "Lê Lô", HerbBName = "Tế Tân", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = leiLuMechanism },
                new() { HerbAName = "Lê Lô", HerbBName = "Bạch Thược", InteractionType = "ThapBatPhan", Severity = "Critical", MechanismDescription = leiLuMechanism },
            };

            var medicines = await context.Medicines.Where(m => m.CategoryId != 0).ToDictionaryAsync(m => m.Name, m => m.Id);

            foreach (var item in seedData)
            {
                if (!medicines.TryGetValue(item.HerbAName, out var herbAId)) continue;
                if (!medicines.TryGetValue(item.HerbBName, out var herbBId)) continue;

                int? replacementForAId = item.ReplacementForAName != null && medicines.TryGetValue(item.ReplacementForAName, out var repA) ? repA : null;
                int? replacementForBId = item.ReplacementForBName != null && medicines.TryGetValue(item.ReplacementForBName, out var repB) ? repB : null;

                context.HerbalInteractions.Add(new HerbalInteraction
                {
                    HerbAId = herbAId,
                    HerbBId = herbBId,
                    InteractionType = item.InteractionType,
                    Severity = item.Severity,
                    MechanismDescription = item.MechanismDescription,
                    SuggestedReplacementForAId = replacementForAId,
                    SuggestedReplacementForBId = replacementForBId
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
