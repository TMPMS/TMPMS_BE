-- ============================================================================
-- HEALTH QUIZ ENGINE SEED DATA
-- File: TMPMS_BE/Database/seed_health_quizzes.sql
-- GHI CHÚ QUAN TRỌNG: Bộ câu hỏi/ngưỡng điểm là bản demo cho đồ án, cần chuyên gia y tế rà soát trước khi dùng thật, không dùng để chẩn đoán y khoa chính thức.
-- ============================================================================

-- 1. Quiz 1: cardio-risk (Bài kiểm tra nguy cơ mắc bệnh tim mạch)
IF NOT EXISTS (SELECT 1 FROM HealthQuizzes WHERE Code = 'cardio-risk')
BEGIN
    INSERT INTO HealthQuizzes (Code, Title, Description, IconUrl, IsActive)
    VALUES ('cardio-risk', N'Bài kiểm tra nguy cơ mắc bệnh tim mạch', N'Đánh giá các yếu tố nguy cơ lối sống, tuổi tác, huyết áp và tiền sử gia đình ảnh hưởng đến sức khỏe tim mạch.', '🫀', 1);

    DECLARE @CardioId INT = SCOPE_IDENTITY();

    -- Result Bands
    INSERT INTO QuizResultBands (QuizId, MinScore, MaxScore, Label, RiskLevel, Description, RecommendationText)
    VALUES 
    (@CardioId, 0, 6, N'Nguy cơ thấp', 'Low', N'Hệ tim mạch của bạn đang ở mức an toàn tốt.', N'Duy trì chế độ ăn uống cân bằng, tập thể dục đều đặn 30 phút mỗi ngày và kiểm tra sức khỏe định kỳ 6-12 tháng/lần.'),
    (@CardioId, 7, 14, N'Nguy cơ trung bình', 'Medium', N'Bạn có một số yếu tố nguy cơ ảnh hưởng đến sức khỏe tim mạch.', N'Nên điều chỉnh thói quen sinh hoạt: giảm bớt muối và mỡ động vật, hạn chế chất kích thích, tăng cường vận động và theo dõi chỉ số huyết áp thường xuyên.'),
    (@CardioId, 15, 50, N'Nguy cơ cao', 'High', N'Bạn có nhiều yếu tố nguy cơ cao mắc bệnh tim mạch.', N'Nên tham khảo ý kiến bác sĩ chuyên khoa tim mạch sớm để được đo điện tâm đồ, xét nghiệm mỡ máu và tư vấn lộ trình kiểm soát nguy cơ hiệu quả.');

    -- Questions & Options
    -- Q1
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Độ tuổi hiện tại của bạn?', 1);
    DECLARE @Q1 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q1, N'Dưới 40 tuổi', 1, 0),
    (@Q1, N'Từ 40 đến 55 tuổi', 2, 2),
    (@Q1, N'Trên 55 tuổi', 3, 4);

    -- Q2
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Thói quen hút thuốc lá của bạn?', 2);
    DECLARE @Q2 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q2, N'Không bao giờ hút', 1, 0),
    (@Q2, N'Đã từng hút và đã bỏ', 2, 1),
    (@Q2, N'Thỉnh thoảng (1-10 điếu/ngày)', 3, 3),
    (@Q2, N'Thường xuyên (trên 10 điếu/ngày)', 4, 5);

    -- Q3
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Huyết áp của bạn thường ở mức nào?', 3);
    DECLARE @Q3 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q3, N'Bình thường (dưới 120/80 mmHg)', 1, 0),
    (@Q3, N'Hơi cao (120-139 / 80-89 mmHg)', 2, 2),
    (@Q3, N'Cao huyết áp (từ 140/90 mmHg trở lên)', 3, 4),
    (@Q3, N'Không rõ / Chưa từng đo', 4, 1);

    -- Q4
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Mức độ vận động thể lực hàng tuần?', 4);
    DECLARE @Q4 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q4, N'Tập đều đặn (trên 150 phút/tuần)', 1, 0),
    (@Q4, N'Ít vận động (1-2 buổi/tuần)', 2, 2),
    (@Q4, N'Hầu như không vận động thể thao', 3, 4);

    -- Q5
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Tiền sử gia đình (bố/mẹ/anh chị em) mắc bệnh tim mạch sớm?', 5);
    DECLARE @Q5 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q5, N'Không có', 1, 0),
    (@Q5, N'Có người mắc bệnh tim/đột quỵ', 2, 3);

    -- Q6
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Chỉ số thể trọng (cân nặng / BMI) của bạn?', 6);
    DECLARE @Q6 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q6, N'Cân đối (BMI < 23)', 1, 0),
    (@Q6, N'Thừa cân nhẹ (BMI 23 - 25)', 2, 1),
    (@Q6, N'Béo phì (BMI > 25)', 3, 3);

    -- Q7
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Mức độ căng thẳng (stress) công việc & cuộc sống?', 7);
    DECLARE @Q7 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q7, N'Hiếm khi căng thẳng', 1, 0),
    (@Q7, N'Đôi khi căng thẳng', 2, 1),
    (@Q7, N'Thường xuyên căng thẳng kéo dài', 3, 2);

    -- Q8
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@CardioId, N'Chế độ ăn uống hàng ngày?', 8);
    DECLARE @Q8 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@Q8, N'Lành mạnh, nhiều rau xanh & trái cây', 1, 0),
    (@Q8, N'Ăn uống bình thường', 2, 1),
    (@Q8, N'Nhiều đồ mặn, chiên xào, thức ăn nhanh', 3, 3);
END;

-- 2. Quiz 2: gerd-risk (Bài kiểm tra nguy cơ trào ngược dạ dày)
IF NOT EXISTS (SELECT 1 FROM HealthQuizzes WHERE Code = 'gerd-risk')
BEGIN
    INSERT INTO HealthQuizzes (Code, Title, Description, IconUrl, IsActive)
    VALUES ('gerd-risk', N'Bài kiểm tra nguy cơ trào ngược dạ dày', N'Đánh giá triệu chứng ợ chua, nóng rát thượng vị và thói quen ăn uống nghi ngờ GERD.', '🫁', 1);

    DECLARE @GerdId INT = SCOPE_IDENTITY();

    -- Result Bands
    INSERT INTO QuizResultBands (QuizId, MinScore, MaxScore, Label, RiskLevel, Description, RecommendationText)
    VALUES 
    (@GerdId, 0, 4, N'Nguy cơ thấp', 'Low', N'Hệ tiêu hóa của bạn khỏe mạnh, chưa phát hiện dấu hiệu trào ngược rõ rệt.', N'Giữ thói quen ăn uống đúng giờ, tránh nằm ngay sau khi ăn và hạn chế ăn đồ quá chua cay hoặc đồ uống có gas.'),
    (@GerdId, 5, 10, N'Nguy cơ trung bình', 'Medium', N'Bạn có một số triệu chứng nghi ngờ trào ngược dạ dày thực quản nhẹ.', N'Nên chia nhỏ bữa ăn, không ăn khuya trước khi ngủ 3 tiếng, kê cao đầu giường 10-15cm khi ngủ và sử dụng các sản phẩm hỗ trợ bảo vệ niêm mạc dạ dày theo tư vấn dược sĩ.'),
    (@GerdId, 11, 50, N'Nguy cơ cao', 'High', N'Các dấu hiệu cho thấy nguy cơ trào ngược dạ dày thực quản rõ rệt.', N'Nên đến cơ sở y tế khám nội soi tiêu hóa để đánh giá mức độ tổn thương niêm mạc thực quản và nhận phác đồ điều trị chuẩn từ bác sĩ.');

    -- Questions & Options
    -- Q1
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@GerdId, N'Bạn có hay bị ợ chua, ợ hơi sau khi ăn?', 1);
    DECLARE @GQ1 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@GQ1, N'Hiếm khi / Không bị', 1, 0),
    (@GQ1, N'Thỉnh thoảng (1-2 lần/tuần)', 2, 2),
    (@GQ1, N'Thường xuyên hàng ngày', 3, 4);

    -- Q2
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@GerdId, N'Cảm giác nóng rát ngực hoặc thượng vị (trên rốn)?', 2);
    DECLARE @GQ2 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@GQ2, N'Không có', 1, 0),
    (@GQ2, N'Đôi khi bị nhẹ', 2, 2),
    (@GQ2, N'Nóng rát dữ dội / Thường xuyên', 3, 4);

    -- Q3
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@GerdId, N'Thói quen ăn khuya trước khi ngủ (< 2 tiếng)?', 3);
    DECLARE @GQ3 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@GQ3, N'Không bao giờ', 1, 0),
    (@GQ3, N'Thỉnh thoảng', 2, 1),
    (@GQ3, N'Thường xuyên', 3, 3);

    -- Q4
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@GerdId, N'Cảm giác vướng họng, đắng miệng khi thức dậy?', 4);
    DECLARE @GQ4 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@GQ4, N'Không bị', 1, 0),
    (@GQ4, N'Đôi khi', 2, 2),
    (@GQ4, N'Thường xuyên', 3, 3);

    -- Q5
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@GerdId, N'Tần suất dùng cà phê, trà đậm, đồ uống có gas?', 5);
    DECLARE @GQ5 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@GQ5, N'Hiếm khi / Không dùng', 1, 0),
    (@GQ5, N'1-2 ly/ngày', 2, 1),
    (@GQ5, N'Nhiều lần trong ngày', 3, 2);

    -- Q6
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@GerdId, N'Thói quen nằm nghỉ ngay sau khi ăn no?', 6);
    DECLARE @GQ6 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@GQ6, N'Không nằm ngay', 1, 0),
    (@GQ6, N'Thỉnh thoảng', 2, 1),
    (@GQ6, N'Thường xuyên nằm ngay', 3, 3);
END;

-- 3. Quiz 3: memory-check (Bài kiểm tra trí nhớ & tập trung)
IF NOT EXISTS (SELECT 1 FROM HealthQuizzes WHERE Code = 'memory-check')
BEGIN
    INSERT INTO HealthQuizzes (Code, Title, Description, IconUrl, IsActive)
    VALUES ('memory-check', N'Bài kiểm tra trí nhớ và mức độ tập trung', N'Đánh giá khả năng ghi nhớ ngắn hạn, mức độ tập trung và ảnh hưởng của thói quen sinh hoạt.', '🧠', 1);

    DECLARE @MemId INT = SCOPE_IDENTITY();

    -- Result Bands
    INSERT INTO QuizResultBands (QuizId, MinScore, MaxScore, Label, RiskLevel, Description, RecommendationText)
    VALUES 
    (@MemId, 0, 4, N'Trí nhớ tốt & Tập trung cao', 'Low', N'Khả năng ghi nhớ và tập trung của bạn ở mức rất tốt.', N'Tiếp tục duy trì thói quen đọc sách, rèn luyện não bộ, tập thể dục và đảm bảo giấc ngủ chất lượng 7-8 tiếng mỗi đêm.'),
    (@MemId, 5, 10, N'Giảm tập trung nhẹ', 'Medium', N'Bạn đang gặp tình trạng giảm tập trung hoặc suy giảm trí nhớ ngắn hạn nhẹ do căng thẳng/thiếu ngủ.', N'Cần sắp xếp lại lịch sinh hoạt, hạn chế thời gian dùng điện thoại/máy tính trước khi ngủ, bổ sung thực phẩm giàu Omega-3, Ginkgo Biloba và vitamin nhóm B.'),
    (@MemId, 11, 50, N'Nguy cơ suy giảm trí nhớ', 'High', N'Tình trạng giảm trí nhớ và khó tập trung diễn ra thường xuyên, ảnh hưởng đến chất lượng sống.', N'Nên tham khảo ý kiến bác sĩ thần kinh để thăm khám, thực hiện các trắc nghiệm tâm lý - thần kinh chuyên sâu và nhận tư vấn chuyên môn phù hợp.');

    -- Questions & Options
    -- Q1
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@MemId, N'Tần suất quên vị trí đồ vật (chìa khóa, ví, điện thoại)?', 1);
    DECLARE @MQ1 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@MQ1, N'Hiếm khi quên', 1, 0),
    (@MQ1, N'Thỉnh thoảng', 2, 2),
    (@MQ1, N'Rất thường xuyên', 3, 4);

    -- Q2
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@MQ1, N'Khả năng tập trung khi làm việc hoặc đọc sách?', 2);
    DECLARE @MQ2 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@MQ2, N'Tốt, ít xao nhãng', 1, 0),
    (@MQ2, N'Đôi khi dễ mất tập trung', 2, 2),
    (@MQ2, N'Rất khó tập trung lâu dài', 3, 4);

    -- Q3
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@MemId, N'Tần suất quên tên người quen hoặc cuộc hẹn?', 3);
    DECLARE @MQ3 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@MQ3, N'Hiếm khi / Không bị', 1, 0),
    (@MQ3, N'Đôi khi quên', 2, 2),
    (@MQ3, N'Thường xuyên quên', 3, 3);

    -- Q4
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@MemId, N'Chất lượng giấc ngủ hàng đêm của bạn?', 4);
    DECLARE @MQ4 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@MQ4, N'Ngủ ngon & sâu (7-8 tiếng)', 1, 0),
    (@MQ4, N'Chập chờn / Thiếu ngủ', 2, 2),
    (@MQ4, N'Mất ngủ kéo dài', 3, 4);

    -- Q5
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@MemId, N'Bạn có hay phải lặp lại câu hỏi hoặc quên điều mình định nói?', 5);
    DECLARE @MQ5 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@MQ5, N'Hiếm khi', 1, 0),
    (@MQ5, N'Đôi khi bị', 2, 2),
    (@MQ5, N'Thường xuyên bị', 3, 3);

    -- Q6
    INSERT INTO QuizQuestions (QuizId, QuestionText, QuestionOrder) VALUES (@MemId, N'Thời gian dùng thiết bị điện tử liên tục hàng ngày?', 6);
    DECLARE @MQ6 INT = SCOPE_IDENTITY();
    INSERT INTO QuizAnswerOptions (QuestionId, OptionText, OptionOrder, Points) VALUES
    (@MQ6, N'Dưới 3 tiếng/ngày', 1, 0),
    (@MQ6, N'Từ 3 đến 6 tiếng/ngày', 2, 1),
    (@MQ6, N'Trên 6 tiếng/ngày', 3, 2);
END;
