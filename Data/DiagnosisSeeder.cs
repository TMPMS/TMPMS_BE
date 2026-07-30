using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMPMS.Data
{
    public static class DiagnosisSeeder
    {
        public static async Task SeedAsync(TMPMSDbContext context)
        {
            if (await context.SyndromeTypes.AnyAsync()) return;

            var syndromes = new List<SyndromeType>
            {
                new SyndromeType { Code = "KH", Name = "Khí Hư", Description = "Khí năng trong cơ thể suy giảm, hơi thở ngắn, mệt mỏi hụt hơi, nói nhỏ, ăn uống kém, hay tự hãn (vã mồ hôi khi vận động nhẹ).", RecommendationText = "Nên nghỉ ngơi hợp lý, bổ sung dinh dưỡng thanh nhẹ dễ tiêu (như cháo táo đỏ, hạt sen), hạn chế thức khuya và vận động quá sức. Nếu triệu chứng kéo dài nên khám trực tiếp với bác sĩ Đông y." },
                new SyndromeType { Code = "HH", Name = "Huyết Hư", Description = "Dinh dưỡng huyết dịch không đủ nuôi dưỡng cơ thể, da dẻ xanh xao tái nhợt, hoa mắt chóng mặt, móng tay giòn dễ gãy, ngủ chập chờn hay chiêm bao.", RecommendationText = "Nên tăng cường thực phẩm giàu vi chất và bổ huyết (như gan, thịt đỏ, táo đỏ, long nhãn), giữ tinh thần thoải mái, ngâm chân ấm trước khi ngủ." },
                new SyndromeType { Code = "AH", Name = "Âm Hư", Description = "Âm dịch trong cơ thể tổn hao, cơ thể nóng trong, lòng bàn tay bàn chân nóng, hay bốc hỏa, họng khô miệng khát, mồ hôi trộm về đêm.", RecommendationText = "Nên hạn chế đồ ăn cay nóng, chiên xào, chất kích thích. Bổ sung các món dưỡng âm sinh tân (như chè hạt sen, yến sào, nước rau má, nước dừa)." },
                new SyndromeType { Code = "DH", Name = "Dương Hư", Description = "Dương khí suy yếu không sưởi ấm được cơ thể, cực kỳ sợ lạnh, tay chân lạnh ngắt, lưng gối đau mỏi, tiểu đêm nhiều lần, phân lỏng nát.", RecommendationText = "Nên giữ ấm vùng lưng, bụng và tay chân. Ăn uống thức ăn ấm nóng, thêm gia vị gừng, quế, tỏi. Hạn chế thức ăn sống lạnh và nước đá." },
                new SyndromeType { Code = "DT", Name = "Đàm Thấp", Description = "Tỳ vị vận hóa thủy thấp kém sinh đàm trệ, cơ thể nặng nề ứ trệ, lồng ngực đầy tức, miệng nhớt khát không muốn uống, lười vận động.", RecommendationText = "Nên tập thể dục đều đặn để vã mồ hôi thoát thấp, ăn nhiều rau xanh, đậu đỏ, ý dĩ. Hạn chế chất béo, đồ ngọt và sữa." },
                new SyndromeType { Code = "KUHU", Name = "Khí Trệ Huyết Ứ", Description = "Khí huyết lưu thông kém gây trệ ứ, đau nhói cố định một vị trí như kim châm, sắc mặt tối xạm, môi tím, tính tình dễ căng thẳng uất ức.", RecommendationText = "Nên vận động thể thao nhẹ nhàng, xoa bóp bấm huyệt gia tăng lưu thông khí huyết. Tránh ngồi một chỗ quá lâu, giữ tâm trạng vui vẻ thoải mái." },
                new SyndromeType { Code = "TTLH", Name = "Tâm Tỳ Lưỡng Hư", Description = "Tâm thần không được nuôi dưỡng kết hợp Tỳ khí suy yếu, gây mất ngủ trằn trọc, hồi hộp tim đập nhanh, hay quên, giật mình, chán ăn.", RecommendationText = "Nên dưỡng tâm an thần, ăn các món ăn bài thuốc như chè sen long nhãn, cháo bách hợp. Tránh làm việc mệt mỏi về đêm." },
                new SyndromeType { Code = "CUKT", Name = "Can Uất Khí Trệ", Description = "Chức năng sơ tiết của Can bị rối loạn do áp lực tinh thần, hay thở dài, ngực sườn đau tức căng bĩ, hay gắt gỏng, kinh nguyệt không đều.", RecommendationText = "Nên thư giãn tinh thần, tập thiền, yoga, đi dạo thiên nhiên. Uống trà hoa cúc, trà hoa hồng giúp giải uất can khí." }
            };

            await context.SyndromeTypes.AddRangeAsync(syndromes);
            await context.SaveChangesAsync();

            var kh = syndromes.First(s => s.Code == "KH");
            var hh = syndromes.First(s => s.Code == "HH");
            var ah = syndromes.First(s => s.Code == "AH");
            var dh = syndromes.First(s => s.Code == "DH");
            var dt = syndromes.First(s => s.Code == "DT");
            var kuhu = syndromes.First(s => s.Code == "KUHU");
            var ttlh = syndromes.First(s => s.Code == "TTLH");
            var cukt = syndromes.First(s => s.Code == "CUKT");

            var questions = new List<SymptomQuestion>
            {
                new SymptomQuestion { QuestionOrder = 1, Category = "Năng lượng", QuestionText = "Bạn có thường xuyên cảm thấy mệt mỏi, hụt hơi, nói nhỏ hoặc không có sức lực làm việc không?" },
                new SymptomQuestion { QuestionOrder = 2, Category = "Giấc ngủ", QuestionText = "Chất lượng giấc ngủ của bạn như thế nào? (Mất ngủ, khó ngủ, hay hồi hộp tim đập nhanh)" },
                new SymptomQuestion { QuestionOrder = 3, Category = "Tiêu hóa", QuestionText = "Cảm giác bụng êm hay đầy tức, ăn uống ngon miệng và thói quen tiêu hóa ra sao?" },
                new SymptomQuestion { QuestionOrder = 4, Category = "Nhiệt độ", QuestionText = "Cơ thể bạn nhạy cảm với nhiệt độ như thế nào? (Sợ lạnh, sợ nóng, chân tay lạnh/nóng)" },
                new SymptomQuestion { QuestionOrder = 5, Category = "Tâm trạng", QuestionText = "Tâm trạng và tinh thần gần đây của bạn ra sao? (Căng thẳng, hay thở dài, dễ gắt gỏng)" },
                new SymptomQuestion { QuestionOrder = 6, Category = "Đau nhức", QuestionText = "Bạn có bị đau nhức cơ thể, thắt lưng, mỏi gối hoặc đau nhói cố định vị trí nào không?" },
                new SymptomQuestion { QuestionOrder = 7, Category = "Da tóc", QuestionText = "Tình trạng sắc mặt, làn da và tóc của bạn dạo này thế nào?" },
                new SymptomQuestion { QuestionOrder = 8, Category = "Tiêu tiểu", QuestionText = "Thói quen đi tiểu (tiểu đêm) và tính chất phân của bạn ra sao?" },
                new SymptomQuestion { QuestionOrder = 9, Category = "Mồ hôi", QuestionText = "Tình trạng tiết mồ hôi của bạn thế nào? (Vã mồ hôi khi vận động nhẹ hoặc đổ mồ hôi trộm khi ngủ)" },
                new SymptomQuestion { QuestionOrder = 10, Category = "Tổng quát", QuestionText = "Tình trạng sức khỏe chung và chu kỳ kinh nguyệt (đối với nữ) hoặc sinh lực (đối với nam)?" }
            };

            await context.SymptomQuestions.AddRangeAsync(questions);
            await context.SaveChangesAsync();

            var mappings = new List<AnswerScoreMapping>();

            // Helper to add options and mappings
            void AddOptions(SymptomQuestion q, (string text, Dictionary<SyndromeType, int> scores)[] opts)
            {
                int order = 1;
                foreach (var (text, scoreDict) in opts)
                {
                    var option = new AnswerOption { QuestionId = q.Id, OptionText = text, OptionOrder = order++ };
                    context.AnswerOptions.Add(option);
                    context.SaveChanges();

                    foreach (var kvp in scoreDict)
                    {
                        mappings.Add(new AnswerScoreMapping { AnswerOptionId = option.Id, SyndromeTypeId = kvp.Key.Id, Points = kvp.Value });
                    }
                }
            }

            AddOptions(questions[0], new[] {
                ("Không bao giờ", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng", new Dictionary<SyndromeType, int> { { kh, 1 } }),
                ("Thường xuyên", new Dictionary<SyndromeType, int> { { kh, 2 }, { ttlh, 1 } }),
                ("Rất thường xuyên", new Dictionary<SyndromeType, int> { { kh, 3 }, { ttlh, 2 } })
            });

            AddOptions(questions[1], new[] {
                ("Ngủ tốt, tinh thần sảng khoái", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng trằn trọc", new Dictionary<SyndromeType, int> { { ttlh, 1 } }),
                ("Thường xuyên mất ngủ, ngủ chập chờn", new Dictionary<SyndromeType, int> { { ttlh, 2 }, { hh, 1 } }),
                ("Mất ngủ nghiêm trọng, hồi hộp hay lo âu", new Dictionary<SyndromeType, int> { { ttlh, 3 }, { hh, 2 } })
            });

            AddOptions(questions[2], new[] {
                ("Tiêu hóa bình thường, ăn ngon", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng đầy bụng khó tiêu", new Dictionary<SyndromeType, int> { { dt, 1 } }),
                ("Thường xuyên chán ăn, bụng nặng nề đầy trệ", new Dictionary<SyndromeType, int> { { dt, 2 }, { kh, 1 } }),
                ("Rất hay đầy trệ, chán ăn, miệng nhớt đắng", new Dictionary<SyndromeType, int> { { dt, 3 } })
            });

            AddOptions(questions[3], new[] {
                ("Cơ thể điều hòa nhiệt độ tốt", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng sợ lạnh hoặc bốc hỏa nhẹ", new Dictionary<SyndromeType, int> { { dh, 1 }, { ah, 1 } }),
                ("Thường xuyên tay chân lạnh ngắt, sợ gió lạnh", new Dictionary<SyndromeType, int> { { dh, 3 } }),
                ("Thường xuyên bốc hỏa nóng trong, lòng bàn tay chân nóng", new Dictionary<SyndromeType, int> { { ah, 3 } })
            });

            AddOptions(questions[4], new[] {
                ("Thoải mái, vui vẻ", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng có áp lực nhẹ", new Dictionary<SyndromeType, int> { { cukt, 1 } }),
                ("Thường xuyên stress, hay thở dài, đau căng hai bên sườn", new Dictionary<SyndromeType, int> { { cukt, 2 } }),
                ("Rất hay gắt gỏng, uất ức, ngực sườn đau tức", new Dictionary<SyndromeType, int> { { cukt, 3 }, { kuhu, 1 } })
            });

            AddOptions(questions[5], new[] {
                ("Không đau nhức", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng mỏi lưng nhẹ", new Dictionary<SyndromeType, int> { { dh, 1 } }),
                ("Thường xuyên đau mỏi thắt lưng, gối yếu", new Dictionary<SyndromeType, int> { { dh, 2 } }),
                ("Đau nhói cố định một chỗ như kim châm, lưỡi môi thâm tím", new Dictionary<SyndromeType, int> { { kuhu, 3 } })
            });

            AddOptions(questions[6], new[] {
                ("Hồng hào, khỏe mạnh", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng hơi khô mỏi", new Dictionary<SyndromeType, int> { { hh, 1 } }),
                ("Da xanh xao tái nhợt, móng tay giòn, tóc rụng nhiều", new Dictionary<SyndromeType, int> { { hh, 3 } }),
                ("Sắc mặt tối sạm, da khô ráp hoặc dễ nổi mẩn ngứa", new Dictionary<SyndromeType, int> { { kuhu, 2 }, { ah, 1 } })
            });

            AddOptions(questions[7], new[] {
                ("Bình thường, phân thành khuôn", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng tiểu đêm 1 lần", new Dictionary<SyndromeType, int> { { dh, 1 } }),
                ("Tiểu đêm nhiều lần (>=2 lần), phân lỏng nát", new Dictionary<SyndromeType, int> { { dh, 3 } }),
                ("Táo bón kéo dài, phân khô cứng hoặc đại tiện dính", new Dictionary<SyndromeType, int> { { ah, 2 }, { dt, 1 } })
            });

            AddOptions(questions[8], new[] {
                ("Mồ hôi bình thường", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng ra nhiều mồ hôi khi nóng", new Dictionary<SyndromeType, int> { { kh, 1 } }),
                ("Vận động nhẹ đã vã mồ hôi ướt áo (Tự hãn)", new Dictionary<SyndromeType, int> { { kh, 3 } }),
                ("Đêm ngủ đổ mồ hôi trộm ướt gối họng khô (Đạo hãn)", new Dictionary<SyndromeType, int> { { ah, 3 } })
            });

            AddOptions(questions[9], new[] {
                ("Khỏe mạnh bình thường", new Dictionary<SyndromeType, int>()),
                ("Thỉnh thoảng hơi mệt nhẹ", new Dictionary<SyndromeType, int> { { cukt, 1 } }),
                ("Kinh nguyệt không đều/giảm sinh lực, nhức mỏi", new Dictionary<SyndromeType, int> { { cukt, 2 } }),
                ("Kinh nguyệt vón cục tím đen/suy nhược kéo dài", new Dictionary<SyndromeType, int> { { kuhu, 3 } })
            });

            await context.AnswerScoreMappings.AddRangeAsync(mappings);
            await context.SaveChangesAsync();
        }
    }
}
