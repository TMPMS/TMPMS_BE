-- ============================================================================
-- SEED DATA SQL SCRIPT: THỂ BỆNH & BỘ CÂU HỎI TỰ CHẨN ĐOÁN ĐÔNG Y
-- Lưu ý: Bộ câu hỏi và điểm số này là bản seed demo cho đồ án, 
-- cần được giảng viên/chuyên gia Đông y thật rà soát lại trước khi dùng 
-- cho mục đích y tế thực tế.
-- ============================================================================

SET NOCOUNT ON;

-- 1. SEED 8 THỂ BỆNH ĐÔNG Y (SyndromeTypes)
IF NOT EXISTS (SELECT 1 FROM SyndromeTypes)
BEGIN
    INSERT INTO SyndromeTypes (Code, Name, Description, RecommendationText) VALUES
    (N'KH', N'Khí Hư', N'Khí năng trong cơ thể suy giảm, hơi thở ngắn, mệt mỏi hụt hơi, nói nhỏ, ăn uống kém, hay tự hãn (vã mồ hôi khi vận động nhẹ).', N'Nên nghỉ ngơi hợp lý, bổ sung dinh dưỡng thanh nhẹ dễ tiêu (như cháo táo đỏ, hạt sen), hạn chế thức khuya và vận động quá sức. Nếu triệu chứng kéo dài nên khám trực tiếp với bác sĩ Đông y.'),
    (N'HH', N'Huyết Hư', N'Dinh dưỡng huyết dịch không đủ nuôi dưỡng cơ thể, da dẻ xanh xao tái nhợt, hoa mắt chóng mặt, móng tay giòn dễ gãy, ngủ chập chờn hay chiêm bao.', N'Nên tăng cường thực phẩm giàu vi chất và bổ huyết (như gan, thịt đỏ, táo đỏ, long nhãn), giữ tinh thần thoải mái, ngâm chân ấm trước khi ngủ.'),
    (N'AH', N'Âm Hư', N'Âm dịch trong cơ thể tổn hao, cơ thể nóng trong, lòng bàn tay bàn chân nóng, hay bốc hỏa, họng khô miệng khát, mồ hôi trộm về đêm.', N'Nên hạn chế đồ ăn cay nóng, chiên xào, chất kích thích. Bổ sung các món dưỡng âm sinh tân (như chè hạt sen, yến sào, nước rau má, nước dừa).'),
    (N'DH', N'Dương Hư', N'Dương khí suy yếu không sưởi ấm được cơ thể, cực kỳ sợ lạnh, tay chân lạnh ngắt, lưng gối đau mỏi, tiểu đêm nhiều lần, phân lỏng nát.', N'Nên giữ ấm vùng lưng, bụng và tay chân. Ăn uống thức ăn ấm nóng, thêm gia vị gừng, quế, tỏi. Hạn chế thức ăn sống lạnh và nước đá.'),
    (N'DT', N'Đàm Thấp', N'Tỳ vị vận hóa thủy thấp kém sinh đàm trệ, cơ thể nặng nề ứ trệ, lồng ngực đầy tức, miệng nhớt khát không muốn uống, lười vận động.', N'Nên tập thể dục đều đặn để vã mồ hôi thoát thấp, ăn nhiều rau xanh, đậu đỏ, ý dĩ. Hạn chế chất béo, đồ ngọt và sữa.'),
    (N'KUHU', N'Khí Trệ Huyết Ứ', N'Khí huyết lưu thông kém gây trệ ứ, đau nhói cố định một vị trí như kim châm, sắc mặt tối xạm, môi tím, tính tình dễ căng thẳng uất ức.', N me='Nên vận động thể thao nhẹ nhàng, xoa bóp bấm huyệt gia tăng lưu thông khí huyết. Tránh ngồi một chỗ quá lâu, giữ tâm trạng vui vẻ thoải mái.'),
    (N'TTLH', N'Tâm Tỳ Lưỡng Hư', N'Tâm thần không được nuôi dưỡng kết hợp Tỳ khí suy yếu, gây mất ngủ trằn trọc, hồi hộp tim đập nhanh, hay quên, giật mình, chán ăn.', N'Nên dưỡng tâm an thần, ăn các món ăn bài thuốc như chè sen long nhãn, cháo bách hợp. Tránh làm việc mệt mỏi về đêm.'),
    (N'CUKT', N'Can Uất Khí Trệ', N'Chức năng sơ tiết của Can bị rối loạn do áp lực tinh thần, hay thở dài, ngực sườn đau tức căng bĩ, hay gắt gỏng, kinh nguyệt không đều.', N'Nên thư giãn tinh thần, tập thiền, yoga, đi dạo thiên nhiên. Uống trà hoa cúc, trà hoa hồng giúp giải uất can khí.');
END

-- 2. SEED 10 CÂU HỎI & ĐÁP ÁN (SymptomQuestions & AnswerOptions & AnswerScoreMappings)
IF NOT EXISTS (SELECT 1 FROM SymptomQuestions)
BEGIN
    -- Q1: Năng lượng
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Bạn có thường xuyên cảm thấy mệt mỏi, hụt hơi, nói nhỏ hoặc không có sức lực làm việc không?', 1, N'Năng lượng');
    DECLARE @Q1 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q1, N'Không bao giờ', 1), (@Q1, N'Thỉnh thoảng', 2), (@Q1, N'Thường xuyên', 3), (@Q1, N'Rất thường xuyên', 4);

    -- Q2: Giấc ngủ
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Chất lượng giấc ngủ của bạn như thế nào? (Mất ngủ, khó ngủ, hay hồi hộp tim đập nhanh)', 2, N'Giấc ngủ');
    DECLARE @Q2 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q2, N'Ngủ tốt, tinh thần sảng khoái', 1), (@Q2, N'Thỉnh thoảng trằn trọc', 2), (@Q2, N'Thường xuyên mất ngủ, ngủ chập chờn', 3), (@Q2, N'Mất ngủ nghiêm trọng, hồi hộp hay lo âu', 4);

    -- Q3: Tiêu hóa
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Cảm giác bụng êm hay đầy tức, ăn uống ngon miệng và thói quen tiêu hóa ra sao?', 3, N'Tiêu hóa');
    DECLARE @Q3 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q3, N'Tiêu hóa bình thường, ăn ngon', 1), (@Q3, N'Thỉnh thoảng đầy bụng khó tiêu', 2), (@Q3, N'Thường xuyên chán ăn, bụng nặng nề đầy trệ', 3), (@Q3, N'Rất hay đầy trệ, chán ăn, miệng nhớt đắng', 4);

    -- Q4: Nhiệt độ
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Cơ thể bạn nhạy cảm với nhiệt độ như thế nào? (Sợ lạnh, sợ nóng, chân tay lạnh/nóng)', 4, N'Nhiệt độ');
    DECLARE @Q4 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q4, N'Cơ thể điều hòa nhiệt độ tốt', 1), (@Q4, N'Thỉnh thoảng sợ lạnh hoặc bốc hỏa nhẹ', 2), (@Q4, N'Thường xuyên tay chân lạnh ngắt, sợ gió lạnh', 3), (@Q4, N'Thường xuyên bốc hỏa nóng trong, lòng bàn tay chân nóng', 4);

    -- Q5: Tâm trạng
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Tâm trạng và tinh thần gần đây của bạn ra sao? (Căng thẳng, hay thở dài, dễ gắt gỏng)', 5, N'Tâm trạng');
    DECLARE @Q5 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q5, N'Thoải mái, vui vẻ', 1), (@Q5, N'Thỉnh thoảng có áp lực nhẹ', 2), (@Q5, N'Thường xuyên stress, hay thở dài, đau căng hai bên sườn', 3), (@Q5, N'Rất hay gắt gỏng, uất ức, ngực sườn đau tức', 4);

    -- Q6: Đau nhức
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Bạn có bị đau nhức cơ thể, thắt lưng, mỏi gối hoặc đau nhói cố định vị trí nào không?', 6, N'Đau nhức');
    DECLARE @Q6 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q6, N'Không đau nhức', 1), (@Q6, N'Thỉnh thoảng mỏi lưng nhẹ', 2), (@Q6, N'Thường xuyên đau mỏi thắt lưng, gối yếu', 3), (@Q6, N'Đau nhói cố định một chỗ như kim châm, lưỡi môi thâm tím', 4);

    -- Q7: Da tóc
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Tình trạng sắc mặt, làn da và tóc của bạn dạo này thế nào?', 7, N'Da tóc');
    DECLARE @Q7 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q7, N'Hồng hào, khỏe mạnh', 1), (@Q7, N'Thỉnh thoảng hơi khô mỏi', 2), (@Q7, N'Da xanh xao tái nhợt, móng tay giòn, tóc rụng nhiều', 3), (@Q7, N'Sắc mặt tối sạm, da khô ráp hoặc dễ nổi mẩn ngứa', 4);

    -- Q8: Tiêu tiểu
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Thói quen đi tiểu (tiểu đêm) và tính chất phân của bạn ra sao?', 8, N'Tiêu tiểu');
    DECLARE @Q8 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q8, N'Bình thường, phân thành khuôn', 1), (@Q8, N'Thỉnh thoảng tiểu đêm 1 lần', 2), (@Q8, N'Tiểu đêm nhiều lần (>=2 lần), phân lỏng nát', 3), (@Q8, N'Táo bón kéo dài, phân khô cứng hoặc đại tiện dính', 4);

    -- Q9: Mồ hôi
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Tình trạng tiết mồ hôi của bạn thế nào? (Vã mồ hôi khi vận động nhẹ hoặc đổ mồ hôi trộm khi ngủ)', 9, N'Mồ hôi');
    DECLARE @Q9 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q9, N'Mồ hôi bình thường', 1), (@Q9, N'Thỉnh thoảng ra nhiều mồ hôi khi nóng', 2), (@Q9, N'Vận động nhẹ đã vã mồ hôi ướt áo (Tự hãn)', 3), (@Q9, N'Đêm ngủ đổ mồ hôi trộm ướt gối họng khô (Đạo hãn)', 4);

    -- Q10: Tổng quát & Kinh nguyệt
    INSERT INTO SymptomQuestions (QuestionText, QuestionOrder, Category) VALUES (N'Tình trạng sức khỏe chung và chu kỳ kinh nguyệt (đối với nữ) hoặc sinh lực (đối với nam)?', 10, N'Tổng quát');
    DECLARE @Q10 INT = SCOPE_IDENTITY();
    INSERT INTO AnswerOptions (QuestionId, OptionText, OptionOrder) VALUES 
    (@Q10, N'Khỏe mạnh bình thường', 1), (@Q10, N'Thỉnh thoảng hơi mệt nhẹ', 2), (@Q10, N'Kinh nguyệt không đều/giảm sinh lực, nhức mỏi', 3), (@Q10, N'Kinh nguyệt vón cục tím đen/suy nhược kéo dài', 4);
END

-- 3. MAP ĐIỂM SỐ (AnswerScoreMappings)
-- Fetch IDs for SyndromeTypes
DECLARE @Id_KH INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'KH');
DECLARE @Id_HH INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'HH');
DECLARE @Id_AH INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'AH');
DECLARE @Id_DH INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'DH');
DECLARE @Id_DT INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'DT');
DECLARE @Id_KUHU INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'KUHU');
DECLARE @Id_TTLH INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'TTLH');
DECLARE @Id_CUKT INT = (SELECT Id FROM SyndromeTypes WHERE Code = 'CUKT');

IF NOT EXISTS (SELECT 1 FROM AnswerScoreMappings)
BEGIN
    -- Q1 Mệt mỏi: Opt3 (KH+2, TTLH+1), Opt4 (KH+3, TTLH+2)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KH, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 1 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_TTLH, 1 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 1 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 1 AND ao.OptionOrder = 4;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_TTLH, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 1 AND ao.OptionOrder = 4;

    -- Q2 Giấc ngủ: Opt3 (TTLH+2, HH+1), Opt4 (TTLH+3, HH+2)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_TTLH, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 2 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_HH, 1 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 2 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_TTLH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 2 AND ao.OptionOrder = 4;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_HH, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 2 AND ao.OptionOrder = 4;

    -- Q3 Tiêu hóa: Opt3 (DT+2, KH+1), Opt4 (DT+3, DT+2)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_DT, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 3 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KH, 1 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 3 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_DT, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 3 AND ao.OptionOrder = 4;

    -- Q4 Nhiệt độ: Opt3 (DH+3), Opt4 (AH+3)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_DH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 4 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_AH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 4 AND ao.OptionOrder = 4;

    -- Q5 Tâm trạng: Opt3 (CUKT+2), Opt4 (CUKT+3, KUHU+1)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_CUKT, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 5 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerScoreMappings.AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_CUKT, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 5 AND ao.OptionOrder = 4;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KUHU, 1 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 5 AND ao.OptionOrder = 4;

    -- Q6 Đau nhức: Opt3 (DH+2), Opt4 (KUHU+3)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_DH, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 6 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KUHU, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 6 AND ao.OptionOrder = 4;

    -- Q7 Da tóc: Opt3 (HH+3), Opt4 (KUHU+2, AH+1)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_HH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 7 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KUHU, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 7 AND ao.OptionOrder = 4;

    -- Q8 Tiêu tiểu: Opt3 (DH+3), Opt4 (AH+2, DT+1)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_DH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 8 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_AH, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 8 AND ao.OptionOrder = 4;

    -- Q9 Mồ hôi: Opt3 (KH+3), Opt4 (AH+3)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 9 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_AH, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 9 AND ao.OptionOrder = 4;

    -- Q10 Tổng quát: Opt3 (CUKT+2), Opt4 (KUHU+3)
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_CUKT, 2 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 10 AND ao.OptionOrder = 3;
    INSERT INTO AnswerScoreMappings (AnswerOptionId, SyndromeTypeId, Points)
    SELECT ao.Id, @Id_KUHU, 3 FROM AnswerOptions ao JOIN SymptomQuestions sq ON ao.QuestionId = sq.Id WHERE sq.QuestionOrder = 10 AND ao.OptionOrder = 4;
END
