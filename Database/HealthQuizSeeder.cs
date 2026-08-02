using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Models;

namespace TMPMS.Database
{
    public static class HealthQuizSeeder
    {
        public static async Task SeedAsync(TMPMSDbContext context)
        {
            if (context.HealthQuizzes.Any()) return;

            // 1. Quiz 1: cardio-risk
            var cardioQuiz = new HealthQuiz
            {
                Code = "cardio-risk",
                Title = "Bài kiểm tra nguy cơ mắc bệnh tim mạch",
                Description = "Đánh giá các yếu tố nguy cơ lối sống, tuổi tác, huyết áp và tiền sử gia đình ảnh hưởng đến sức khỏe tim mạch.",
                IconUrl = "🫀",
                IsActive = true,
                ResultBands = new List<QuizResultBand>
                {
                    new QuizResultBand { MinScore = 0, MaxScore = 6, Label = "Nguy cơ thấp", RiskLevel = "Low", Description = "Hệ tim mạch của bạn đang ở mức an toàn tốt.", RecommendationText = "Duy trì chế độ ăn uống cân bằng, tập thể dục đều đặn 30 phút mỗi ngày và kiểm tra sức khỏe định kỳ 6-12 tháng/lần." },
                    new QuizResultBand { MinScore = 7, MaxScore = 14, Label = "Nguy cơ trung bình", RiskLevel = "Medium", Description = "Bạn có một số yếu tố nguy cơ ảnh hưởng đến sức khỏe tim mạch.", RecommendationText = "Nên điều chỉnh thói quen sinh hoạt: giảm bớt muối và mỡ động vật, hạn chế chất kích thích, tăng cường vận động và theo dõi chỉ số huyết áp thường xuyên." },
                    new QuizResultBand { MinScore = 15, MaxScore = 50, Label = "Nguy cơ cao", RiskLevel = "High", Description = "Bạn có nhiều yếu tố nguy cơ cao mắc bệnh tim mạch.", RecommendationText = "Nên tham khảo ý kiến bác sĩ chuyên khoa tim mạch sớm để được đo điện tâm đồ, xét nghiệm mỡ máu và tư vấn lộ trình kiểm soát nguy cơ hiệu quả." }
                },
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionOrder = 1,
                        QuestionText = "Độ tuổi hiện tại của bạn?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Dưới 40 tuổi", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Từ 40 đến 55 tuổi", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Trên 55 tuổi", Points = 4 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 2,
                        QuestionText = "Thói quen hút thuốc lá của bạn?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Không bao giờ hút", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Đã từng hút và đã bỏ", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thỉnh thoảng (1-10 điếu/ngày)", Points = 3 },
                            new QuizAnswerOption { OptionOrder = 4, OptionText = "Thường xuyên (trên 10 điếu/ngày)", Points = 5 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 3,
                        QuestionText = "Huyết áp của bạn thường ở mức nào?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Bình thường (dưới 120/80 mmHg)", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Hơi cao (120-139 / 80-89 mmHg)", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Cao huyết áp (từ 140/90 mmHg trở lên)", Points = 4 },
                            new QuizAnswerOption { OptionOrder = 4, OptionText = "Không rõ / Chưa từng đo", Points = 1 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 4,
                        QuestionText = "Mức độ vận động thể lực hàng tuần?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Tập đều đặn (trên 150 phút/tuần)", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Ít vận động (1-2 buổi/tuần)", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Hầu như không vận động thể thao", Points = 4 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 5,
                        QuestionText = "Tiền sử gia đình (bố/mẹ/anh chị em) mắc bệnh tim mạch sớm?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Không có", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Có người mắc bệnh tim/đột quỵ", Points = 3 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 6,
                        QuestionText = "Chỉ số thể trọng (cân nặng / BMI) của bạn?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Cân đối (BMI < 23)", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Thừa cân nhẹ (BMI 23 - 25)", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Béo phì (BMI > 25)", Points = 3 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 7,
                        QuestionText = "Mức độ căng thẳng (stress) công việc & cuộc sống?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Hiếm khi căng thẳng", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Đôi khi căng thẳng", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thường xuyên căng thẳng kéo dài", Points = 2 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 8,
                        QuestionText = "Chế độ ăn uống hàng ngày?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Lành mạnh, nhiều rau xanh & trái cây", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Ăn uống bình thường", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Nhiều đồ mặn, chiên xào, thức ăn nhanh", Points = 3 }
                        }
                    }
                }
            };

            // 2. Quiz 2: gerd-risk
            var gerdQuiz = new HealthQuiz
            {
                Code = "gerd-risk",
                Title = "Bài kiểm tra nguy cơ trào ngược dạ dày",
                Description = "Đánh giá triệu chứng ợ chua, nóng rát thượng vị và thói quen ăn uống nghi ngờ GERD.",
                IconUrl = "🫁",
                IsActive = true,
                ResultBands = new List<QuizResultBand>
                {
                    new QuizResultBand { MinScore = 0, MaxScore = 4, Label = "Nguy cơ thấp", RiskLevel = "Low", Description = "Hệ tiêu hóa của bạn khỏe mạnh, chưa phát hiện dấu hiệu trào ngược rõ rệt.", RecommendationText = "Giữ thói quen ăn uống đúng giờ, tránh nằm ngay sau khi ăn và hạn chế ăn đồ quá chua cay hoặc đồ uống có gas." },
                    new QuizResultBand { MinScore = 5, MaxScore = 10, Label = "Nguy cơ trung bình", RiskLevel = "Medium", Description = "Bạn có một số triệu chứng nghi ngờ trào ngược dạ dày thực quản nhẹ.", RecommendationText = "Nên chia nhỏ bữa ăn, không ăn khuya trước khi ngủ 3 tiếng, kê cao đầu giường 10-15cm khi ngủ và sử dụng các sản phẩm hỗ trợ bảo vệ niêm mạc dạ dày theo tư vấn dược sĩ." },
                    new QuizResultBand { MinScore = 11, MaxScore = 50, Label = "Nguy cơ cao", RiskLevel = "High", Description = "Các dấu hiệu cho thấy nguy cơ trào ngược dạ dày thực quản rõ rệt.", RecommendationText = "Nên đến cơ sở y tế khám nội soi tiêu hóa để đánh giá mức độ tổn thương niêm mạc thực quản và nhận phác đồ điều trị chuẩn từ bác sĩ." }
                },
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionOrder = 1,
                        QuestionText = "Bạn có hay bị ợ chua, ợ hơi sau khi ăn?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Hiếm khi / Không bị", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Thỉnh thoảng (1-2 lần/tuần)", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thường xuyên hàng ngày", Points = 4 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 2,
                        QuestionText = "Cảm giác nóng rát ngực hoặc thượng vị (trên rốn)?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Không có", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Đôi khi bị nhẹ", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Nóng rát dữ dội / Thường xuyên", Points = 4 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 3,
                        QuestionText = "Thói quen ăn khuya trước khi ngủ (< 2 tiếng)?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Không bao giờ", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Thỉnh thoảng", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thường xuyên", Points = 3 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 4,
                        QuestionText = "Cảm giác vướng họng, đắng miệng khi thức dậy?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Không bị", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Đôi khi", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thường xuyên", Points = 3 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 5,
                        QuestionText = "Tần suất dùng cà phê, trà đậm, đồ uống có gas?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Hiếm khi / Không dùng", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "1-2 ly/ngày", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Nhiều lần trong ngày", Points = 2 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 6,
                        QuestionText = "Thói quen nằm nghỉ ngay sau khi ăn no?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Không nằm ngay", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Thỉnh thoảng", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thường xuyên nằm ngay", Points = 3 }
                        }
                    }
                }
            };

            // 3. Quiz 3: memory-check
            var memoryQuiz = new HealthQuiz
            {
                Code = "memory-check",
                Title = "Bài kiểm tra trí nhớ và mức độ tập trung",
                Description = "Đánh giá khả năng ghi nhớ ngắn hạn, mức độ tập trung và ảnh hưởng của thói quen sinh hoạt.",
                IconUrl = "🧠",
                IsActive = true,
                ResultBands = new List<QuizResultBand>
                {
                    new QuizResultBand { MinScore = 0, MaxScore = 4, Label = "Trí nhớ tốt & Tập trung cao", RiskLevel = "Low", Description = "Khả năng ghi nhớ và tập trung của bạn ở mức rất tốt.", RecommendationText = "Tiếp tục duy trì thói quen đọc sách, rèn luyện não bộ, tập thể dục và đảm bảo giấc ngủ chất lượng 7-8 tiếng mỗi đêm." },
                    new QuizResultBand { MinScore = 5, MaxScore = 10, Label = "Giảm tập trung nhẹ", RiskLevel = "Medium", Description = "Bạn đang gặp tình trạng giảm tập trung hoặc suy giảm trí nhớ ngắn hạn nhẹ do căng thẳng/thiếu ngủ.", RecommendationText = "Cần sắp xếp lại lịch sinh hoạt, hạn chế thời gian dùng điện thoại/máy tính trước khi ngủ, bổ sung thực phẩm giàu Omega-3, Ginkgo Biloba và vitamin nhóm B." },
                    new QuizResultBand { MinScore = 11, MaxScore = 50, Label = "Nguy cơ suy giảm trí nhớ", RiskLevel = "High", Description = "Tình trạng giảm trí nhớ và khó tập trung diễn ra thường xuyên, ảnh hưởng đến chất lượng sống.", RecommendationText = "Nên tham khảo ý kiến bác sĩ thần kinh để thăm khám, thực hiện các trắc nghiệm tâm lý - thần kinh chuyên sâu và nhận tư vấn chuyên môn phù hợp." }
                },
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionOrder = 1,
                        QuestionText = "Tần suất quên vị trí đồ vật (chìa khóa, ví, điện thoại)?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Hiếm khi quên", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Thỉnh thoảng", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Rất thường xuyên", Points = 4 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 2,
                        QuestionText = "Khả năng tập trung khi làm việc hoặc đọc sách?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Tốt, ít xao nhãng", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Đôi khi dễ mất tập trung", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Rất khó tập trung lâu dài", Points = 4 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 3,
                        QuestionText = "Tần suất quên tên người quen hoặc cuộc hẹn?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Hiếm khi / Không bị", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Đôi khi quên", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thường xuyên quên", Points = 3 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 4,
                        QuestionText = "Chất lượng giấc ngủ hàng đêm của bạn?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Ngủ ngon & sâu (7-8 tiếng)", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Chập chờn / Thiếu ngủ", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Mất ngủ kéo dài", Points = 4 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 5,
                        QuestionText = "Bạn có hay phải lặp lại câu hỏi hoặc quên điều mình định nói?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Hiếm khi", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Đôi khi bị", Points = 2 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Thường xuyên bị", Points = 3 }
                        }
                    },
                    new QuizQuestion
                    {
                        QuestionOrder = 6,
                        QuestionText = "Thời gian dùng thiết bị điện tử liên tục hàng ngày?",
                        AnswerOptions = new List<QuizAnswerOption>
                        {
                            new QuizAnswerOption { OptionOrder = 1, OptionText = "Dưới 3 tiếng/ngày", Points = 0 },
                            new QuizAnswerOption { OptionOrder = 2, OptionText = "Từ 3 đến 6 tiếng/ngày", Points = 1 },
                            new QuizAnswerOption { OptionOrder = 3, OptionText = "Trên 6 tiếng/ngày", Points = 2 }
                        }
                    }
                }
            };

            context.HealthQuizzes.AddRange(cardioQuiz, gerdQuiz, memoryQuiz);
            await context.SaveChangesAsync();
        }
    }
}
