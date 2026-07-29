-- Clear existing medicines to avoid conflicts
DELETE FROM OrderItems;
DELETE FROM CartItems;
DELETE FROM Reviews;
DELETE FROM SupplierMedicines;
DELETE FROM InventoryStocks;
DELETE FROM Medicines;
DELETE FROM Categories;
DELETE FROM Suppliers;

-- Seed Categories
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (Id, Name, Description) VALUES
(1, N'Thực phẩm chức năng', N'Các sản phẩm bổ sung dưỡng chất, vitamin, khoáng chất cải thiện sức khỏe'),
(2, N'Dược mỹ phẩm', N'Sản phẩm chăm sóc da mặt, chống nắng, tẩy trang kết hợp dược liệu'),
(3, N'Thuốc', N'Thuốc kê đơn và không kê đơn điều trị bệnh lý'),
(4, N'Chăm sóc cá nhân', N'Các sản phẩm vệ sinh cơ thể, dầu gội, sữa tắm'),
(5, N'Thiết bị y tế', N'Máy đo huyết áp, nhiệt kế và các thiết bị chăm sóc sức khỏe tại nhà'),
(6, N'Châm cứu', N'Thiết bị châm cứu, cứu ngải, bấm huyệt'),
(7, N'Bệnh & Góc sức khỏe', N'Cung cấp kiến thức y tế'),
(8, N'Hệ thống nhà thuốc', N'Danh sách địa điểm hệ thống nhà thuốc');
SET IDENTITY_INSERT Categories OFF;

-- Seed Suppliers
SET IDENTITY_INSERT Suppliers ON;
INSERT INTO Suppliers (Id, CompanyName, ContactPerson, Email, Phone, Address, TaxCode, Status) VALUES
(1, N'Công ty Cổ phần Traphaco', N'Nguyễn Văn A', 'traphaco@gmail.com', '0243681161', N'75 Yên Ninh, Ba Đình, Hà Nội', '0100108656', 'Active'),
(2, N'Công ty TNHH Dược phẩm OPC', N'Trần Thị B', 'opc@opcpharma.com', '0283960124', N'1017 Hồng Bàng, Quận 6, TP. HCM', '0302560112', 'Active'),
(3, N'Công ty Cổ phần Bách Thảo Dược', N'Lê Văn C', 'contact@bachthaoduoc.com.vn', '0225381881', N'Lô Q-6, KCN Tràng Duệ, Hải Phòng', '0201882654', 'Active'),
(4, N'Nhà sâm KGC Hàn Quốc (Cheong Kwan Jang)', N'Kim Min Woo', 'kgc_global@kgc.co.kr', '+82-2-2189-6100', N'Seoul, Hàn Quốc', 'FOREIGN-001', 'Active');
SET IDENTITY_INSERT Suppliers OFF;

-- Seed Medicines
SET IDENTITY_INSERT Medicines ON;
INSERT INTO Medicines (Id, CategoryId, SupplierId, Name, Description, Price, StockQuantity, ManufactureDate, ExpiryDate, RequiresPrescription, ImageUrl, CreatedAt) VALUES
-- =================== BÁN CHẠY (101-106) ===================
(101, 3, 1, N'Hoạt Huyết Dưỡng Não Traphaco (Hộp 5 vỉ x 20 viên)', N'Bổ não, tăng cường tuần hoàn não, giảm đau đầu, chóng mặt, suy giảm trí nhớ.', 95000, 150, '2026-01-01', '2029-01-01', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(102, 1, 3, N'Trà túi lọc Cà Gai Leo thải độc gan (Hộp 20 túi)', N'Hỗ trợ mát gan, giải độc gan, hạ men gan và phục hồi tế bào gan bị tổn thương.', 45000, 200, '2026-02-01', '2028-02-01', 0, 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop', GETDATE()),
(103, 3, 2, N'Kim Tiền Thảo trị sỏi thận OPC (Hộp 100 viên)', N'Thanh nhiệt, lợi niệu, tiêu sỏi, hỗ trợ điều trị sỏi đường tiết niệu, sỏi thận, sỏi mật.', 65000, 120, '2026-01-10', '2029-01-10', 0, 'https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=400&h=400&fit=crop', GETDATE()),
(104, 1, 3, N'Cao Xương Khớp Bách Thảo Dược (Lọ 100g)', N'Hỗ trợ mạnh gân cốt, giảm đau nhức xương khớp do thoái hóa hoặc phong thấp.', 180000, 80, '2026-03-01', '2028-03-01', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE()),
(105, 1, 3, N'Mật ong hoa rừng nguyên chất Tây Nguyên (Chai 500ml)', N'Mật ong thiên nhiên nguyên chất, hỗ trợ bồi bổ sức khỏe, làm dịu cổ họng và hỗ trợ tiêu hóa.', 120000, 90, '2026-04-01', '2029-04-01', 0, 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop', GETDATE()),
(106, 1, 3, N'Bột gừng mật ong sấy thăng hoa (Hộp 15 gói)', N'Làm ấm cơ thể, phòng cảm lạnh, giảm buồn nôn và tăng cường tiêu hóa.', 75000, 110, '2026-03-15', '2028-03-15', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),

-- =================== THẢO DƯỢC & CAO DƯỢC LIỆU (201-206) ===================
(201, 1, 3, N'Đông Trùng Hạ Thảo Militaris sấy (Lọ 10g)', N'Bồi bổ cơ thể, tăng cường hệ miễn dịch, cải thiện sinh lực và hỗ trợ chức năng hô hấp.', 290000, 60, '2026-01-20', '2027-07-20', 0, 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop', GETDATE()),
(202, 1, 2, N'Cao Atiso Vân Anh Đà Lạt (Hộp 1kg)', N'Giải độc gan, lợi mật, giảm cholesterol, thanh nhiệt cơ thể và cải thiện giấc ngủ.', 220000, 75, '2026-02-15', '2028-02-15', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE()),
(203, 1, 4, N'Nhân sâm lát tẩm mật ong Hàn Quốc (Hộp 10 gói)', N'Tăng cường sức đề kháng, phục hồi sức khỏe, giảm căng thẳng mệt mỏi.', 350000, 50, '2026-01-05', '2029-01-05', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),
(204, 1, 4, N'Tinh chất hồng sâm KGC Everytime (Hộp 30 gói)', N'Chiết xuất hồng sâm 6 năm tuổi cô đặc cao cấp giúp cải thiện trí nhớ, tăng lưu thông máu.', 1450000, 40, '2026-03-10', '2029-03-10', 0, 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop', GETDATE()),
(205, 1, 3, N'Viên nghệ mật ong sữa chúa Tenchi (Hộp 250g)', N'Hỗ trợ viêm loét dạ dày, tá tràng, làm đẹp da và bồi bổ cơ thể.', 160000, 95, '2026-04-12', '2028-04-12', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),
(206, 1, 3, N'Dầu tràm nguyên chất Cung Đình Huế (Chai 50ml)', N'Phòng tránh gió máy, cảm cúm, sổ mũi, côn trùng cắn, thích hợp cho bé và bà mẹ sau sinh.', 125000, 130, '2026-05-01', '2031-05-01', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE()),

-- =================== ĐÔNG Y BỔ SUNG (301-320) ===================
(301, 1, 1, N'An Cung Ngưu Hoàng Hoàn Traphaco (Hộp 1 viên)', N'Hỗ trợ phục hồi sau tai biến mạch máu não, tăng cường tuần hoàn não, an thần, giảm căng thẳng.', 280000, 60, '2026-01-01', '2028-01-01', 0, 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop', GETDATE()),
(302, 1, 3, N'Cao Đinh Lăng Bách Thảo Dược (Lọ 250g)', N'Bổ khí huyết, tăng cường sinh lực, hỗ trợ điều trị suy nhược cơ thể và mất ngủ.', 135000, 100, '2026-02-01', '2028-02-01', 0, 'https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&h=400&fit=crop', GETDATE()),
(303, 1, 2, N'Viên Uống Tam Thất Bắc OPC (Hộp 60 viên)', N'Hoạt huyết, tán ứ, cầm máu, hỗ trợ điều trị đau thắt ngực, thiếu máu cơ tim.', 195000, 85, '2026-01-15', '2028-07-15', 0, 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop', GETDATE()),
(304, 1, 3, N'Trà Hà Thủ Ô Đỏ Bách Thảo (Hộp 20 túi lọc)', N'Bổ gan thận, đen tóc, chống lão hóa, cải thiện chức năng sinh lý nam giới.', 55000, 150, '2026-03-01', '2028-03-01', 0, 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop', GETDATE()),
(305, 1, 1, N'Viên Nang Bạch Quả Ginkgo Traphaco (Hộp 60 viên)', N'Tăng cường tuần hoàn máu não, cải thiện trí nhớ, giảm ù tai, chóng mặt do thiếu máu não.', 145000, 120, '2026-02-10', '2029-02-10', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(306, 1, 3, N'Cao Trinh Nữ Hoàng Cung (Lọ 100ml)', N'Hỗ trợ điều trị u xơ tử cung, u nang buồng trứng, điều hòa kinh nguyệt.', 210000, 70, '2026-01-20', '2028-01-20', 0, 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop', GETDATE()),
(307, 1, 2, N'Viên Linh Chi Đỏ OPC (Hộp 30 viên)', N'Tăng cường miễn dịch, chống oxy hóa, hỗ trợ gan, cải thiện giấc ngủ và giảm căng thẳng.', 320000, 55, '2026-03-05', '2028-09-05', 0, 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop', GETDATE()),
(308, 1, 3, N'Bột Nghệ Nano Curcumin Hữu Cơ (Hộp 30 gói)', N'Chống viêm, chống loét dạ dày, làm đẹp da, hỗ trợ tiêu hóa và tăng cường miễn dịch.', 185000, 90, '2026-04-01', '2028-04-01', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),
(309, 3, 1, N'Lục Vị Địa Hoàng Traphaco (Hộp 200 viên)', N'Bổ thận âm, điều trị thận âm hư, đau lưng mỏi gối, di tinh, ù tai, hoa mắt chóng mặt.', 175000, 95, '2026-01-05', '2029-01-05', 0, 'https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=400&h=400&fit=crop', GETDATE()),
(310, 3, 2, N'Bát Vị Quế Phụ OPC (Hộp 60 viên)', N'Bổ thận dương, điều trị thận dương hư, đau lưng lạnh, tiểu đêm nhiều lần, liệt dương.', 155000, 80, '2026-02-20', '2029-02-20', 0, 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop', GETDATE()),
(311, 3, 1, N'Độc Hoạt Ký Sinh Traphaco (Hộp 100 viên)', N'Khu phong thấp, tán hàn, điều trị đau khớp xương, tê bì chân tay do phong thấp.', 125000, 110, '2026-01-10', '2028-07-10', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(312, 3, 2, N'Tiêu Dao Hoàn OPC (Hộp 200 viên)', N'Sơ can giải uất, kiện tỳ hòa vị, điều trị viêm dạ dày mạn tính, rối loạn tiêu hóa do tâm lý.', 110000, 130, '2026-03-15', '2028-09-15', 0, 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop', GETDATE()),
(313, 3, 3, N'Phong Tê Thấp Bách Thảo (Hộp 100 viên)', N'Trừ phong thấp, hoạt lạc, điều trị viêm khớp dạng thấp, thoái hóa khớp gối, đau cổ vai gáy.', 140000, 100, '2026-02-05', '2028-08-05', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE()),
(314, 1, 3, N'Cao Ích Mẫu Bách Thảo (Lọ 250ml)', N'Điều hòa kinh nguyệt, hoạt huyết tán ứ, hỗ trợ điều trị kinh nguyệt không đều, đau bụng kinh.', 175000, 75, '2026-01-25', '2028-01-25', 0, 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop', GETDATE()),
(315, 1, 3, N'Rượu Thuốc Ngũ Gia Bì (Chai 500ml)', N'Bổ can thận, mạnh gân cốt, trừ phong thấp, tăng cường sinh lực, phục hồi sức khỏe sau ốm.', 230000, 50, '2025-12-01', '2028-12-01', 0, 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop', GETDATE()),
(316, 1, 4, N'Hồng Sâm Củ Tươi KGC 6 Năm Tuổi (Hộp 150g)', N'Bồi bổ nguyên khí, phục hồi sức khỏe, tăng cường miễn dịch, chống oxy hóa mạnh.', 890000, 30, '2025-11-01', '2027-11-01', 0, 'https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&h=400&fit=crop', GETDATE()),
(317, 1, 3, N'Trà Diệp Hạ Châu Mát Gan (Hộp 20 túi)', N'Thanh nhiệt giải độc, mát gan, hạ men gan, hỗ trợ viêm gan B mạn tính.', 42000, 180, '2026-04-01', '2028-04-01', 0, 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop', GETDATE()),
(318, 1, 3, N'Trà Khổ Qua Rừng Giảm Đường Huyết (Hộp 30 túi)', N'Hỗ trợ hạ đường huyết tự nhiên, giảm mỡ máu, thanh nhiệt, phòng ngừa biến chứng tiểu đường.', 65000, 140, '2026-03-10', '2028-03-10', 0, 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop', GETDATE()),
(319, 1, 1, N'Viên Uống An Thần Traphaco (Hộp 50 viên)', N'An thần, dưỡng tâm, hỗ trợ điều trị mất ngủ, lo âu, hồi hộp, suy nhược thần kinh.', 98000, 125, '2026-02-15', '2029-02-15', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(320, 1, 2, N'Hoàng Kỳ Chiết Xuất OPC (Hộp 60 viên)', N'Bổ khí ích vệ, tăng cường miễn dịch, phòng cảm cúm theo mùa, hỗ trợ bệnh nhân sau hóa trị.', 215000, 65, '2026-01-30', '2029-01-30', 0, 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop', GETDATE()),

-- =================== ĐA DẠNG SẢN PHẨM (401-430) ===================
(401, 1, 1, N'Vitamin C 1000mg Traphaco Sủi (Tuýp 20 viên)', N'Bổ sung vitamin C, tăng cường đề kháng, chống oxy hóa, làm đẹp da và hỗ trợ hấp thu sắt.', 45000, 300, '2026-01-01', '2028-01-01', 0, 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop', GETDATE()),
(402, 1, 2, N'Collagen Peptide Nhật Bản Fine (Hộp 30 gói)', N'Bổ sung collagen thủy phân, làm đẹp da, chống lão hóa, cải thiện độ đàn hồi da.', 580000, 80, '2026-02-01', '2028-02-01', 0, 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop', GETDATE()),
(403, 1, 3, N'Omega-3 Fish Oil 1000mg (Hộp 100 viên)', N'Bổ dung DHA + EPA, tốt cho tim mạch, não bộ, giảm triglyceride và chống viêm.', 185000, 150, '2026-01-15', '2028-07-15', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(404, 1, 1, N'Canxi D3 K2 Traphaco (Hộp 60 viên)', N'Bổ sung canxi, vitamin D3 và K2 giúp xương chắc khỏe, phòng loãng xương, hỗ trợ phát triển chiều cao.', 165000, 200, '2026-03-01', '2029-03-01', 0, 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop', GETDATE()),
(405, 1, 4, N'Viên Uống Bổ Não DHA + Ginkgo KGC (Hộp 30 viên)', N'Tăng cường trí nhớ, cải thiện sự tập trung, hỗ trợ tuần hoàn não và giảm suy giảm nhận thức.', 390000, 60, '2026-01-20', '2029-01-20', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(406, 1, 2, N'Men Vi Sinh Probiotic 10 tỷ khuẩn OPC (Hộp 30 gói)', N'Cân bằng hệ vi sinh đường ruột, hỗ trợ tiêu hóa, giảm đầy bụng khó tiêu và tăng miễn dịch.', 220000, 120, '2026-04-01', '2028-04-01', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE()),
(407, 1, 3, N'Yến Sào Thiên Nhiên Khánh Hòa (Hộp 70g)', N'Bồi bổ cơ thể, tăng cường sức đề kháng, hỗ trợ hô hấp và phục hồi sức khỏe.', 1250000, 25, '2025-12-01', '2027-06-01', 0, 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop', GETDATE()),
(408, 1, 1, N'Sắt Hữu Cơ Ferrous Bisglycinate Traphaco (Hộp 30 viên)', N'Bổ sung sắt hữu cơ dễ hấp thu, không gây táo bón, hỗ trợ điều trị thiếu máu thiếu sắt.', 95000, 180, '2026-02-10', '2029-02-10', 0, 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop', GETDATE()),
(409, 2, 2, N'Kem Chống Nắng La Roche-Posay SPF50+ (50ml)', N'Bảo vệ da khỏi tia UV, không gây nhờn, phù hợp da nhạy cảm và có vấn đề về da.', 480000, 90, '2025-11-01', '2027-11-01', 0, 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop', GETDATE()),
(410, 2, 2, N'Serum Vitamin C 20% Làm Sáng Da (Lọ 30ml)', N'Làm sáng da, mờ thâm nám, chống oxy hóa mạnh và kích thích collagen.', 320000, 70, '2026-01-01', '2028-01-01', 0, 'https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=400&h=400&fit=crop', GETDATE()),
(411, 2, 3, N'Sữa Rửa Mặt Nghệ Hữu Cơ Dưỡng Da (150ml)', N'Làm sạch da, dưỡng ẩm, chống viêm từ tinh chất nghệ hữu cơ, phù hợp mọi loại da.', 95000, 200, '2026-03-01', '2028-03-01', 0, 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop', GETDATE()),
(412, 2, 1, N'Toner Trà Xanh Matcha Dưỡng Ẩm (200ml)', N'Cân bằng độ ẩm, thu nhỏ lỗ chân lông, làm dịu kích ứng và cấp ẩm sâu.', 145000, 160, '2026-02-15', '2028-02-15', 0, 'https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=400&h=400&fit=crop', GETDATE()),
(413, 3, 2, N'Paracetamol 500mg OPC (Hộp 100 viên)', N'Hạ sốt, giảm đau đầu, đau răng, đau cơ. Thành phần: Paracetamol 500mg.', 25000, 500, '2026-01-01', '2028-01-01', 0, 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop', GETDATE()),
(414, 3, 1, N'Berberin Traphaco Hỗ Trợ Tiêu Hóa (Hộp 100 viên)', N'Kháng khuẩn đường ruột, điều trị tiêu chảy, đau bụng do rối loạn tiêu hóa.', 32000, 400, '2026-02-01', '2028-02-01', 0, 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop', GETDATE()),
(415, 3, 2, N'Cetirizine 10mg Chống Dị Ứng OPC (Hộp 30 viên)', N'Chống dị ứng, giảm ngứa, viêm mũi dị ứng, mề đay, không gây buồn ngủ mạnh.', 48000, 250, '2026-01-10', '2028-07-10', 0, 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop', GETDATE()),
(416, 3, 1, N'Esomeprazol 20mg Traphaco Dạ Dày (Hộp 28 viên)', N'Ức chế bơm proton, điều trị viêm loét dạ dày tá tràng, trào ngược dạ dày-thực quản.', 85000, 180, '2026-03-01', '2028-03-01', 1, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(417, 4, 3, N'Dầu Gội Bồ Kết Thảo Dược Bách Thảo (400ml)', N'Gội đầu từ bồ kết, bưởi và thảo mộc thiên nhiên, làm sạch da đầu, giảm rụng tóc.', 75000, 200, '2026-04-01', '2028-04-01', 0, 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop', GETDATE()),
(418, 4, 3, N'Sữa Tắm Tinh Chất Gừng Ấm Người (400ml)', N'Sữa tắm thảo dược từ gừng tươi, làm ấm cơ thể, kháng khuẩn và dưỡng ẩm da.', 65000, 250, '2026-03-15', '2028-03-15', 0, 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop', GETDATE()),
(419, 4, 1, N'Kem Dưỡng Thể Tinh Chất Tràm Hương (200ml)', N'Dưỡng ẩm toàn thân, làm mềm da, hương tràm thư giãn, thích hợp dùng sau tắm.', 120000, 150, '2026-02-20', '2028-02-20', 0, 'https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=400&h=400&fit=crop', GETDATE()),
(420, 4, 2, N'Kem Đánh Răng Than Hoạt Tính Trắng Răng (120g)', N'Làm trắng răng tự nhiên với than hoạt tính, diệt khuẩn, thơm miệng và bảo vệ men răng.', 55000, 300, '2026-01-05', '2028-01-05', 0, 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop', GETDATE()),
(421, 5, 2, N'Máy Đo Huyết Áp Bắp Tay Omron HEM-7156 (Cái)', N'Máy đo huyết áp tự động bắp tay, chính xác cao, phát hiện rung nhĩ, bộ nhớ 60 lần.', 1250000, 30, '2025-10-01', '2030-10-01', 0, 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop', GETDATE()),
(422, 5, 3, N'Nhiệt Kế Điện Tử Kẹp Nách (Cái)', N'Nhiệt kế điện tử đo nách/miệng/hậu môn, kết quả trong 60 giây, bộ nhớ nhiệt độ cuối.', 95000, 100, '2025-06-01', '2030-06-01', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(423, 5, 2, N'Máy Đo Đường Huyết Accu-Chek Active (Bộ)', N'Bộ máy đo đường huyết kèm 10 que thử, kết quả 5 giây, bộ nhớ 500 lần, dùng cho người tiểu đường.', 680000, 40, '2025-08-01', '2030-08-01', 0, 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop', GETDATE()),
(424, 5, 3, N'Máy Xông Mũi Họng Khí Dung (Cái)', N'Máy xông khí dung điều trị viêm mũi, viêm họng, hen suyễn, hạt phun mịn 3-5 micron.', 450000, 55, '2025-09-01', '2030-09-01', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(425, 1, 4, N'Bột Sâm Hàn Quốc Nguyên Chất KGC (Hộp 30g)', N'Bột hồng sâm nguyên chất 100%, hòa tan nhanh, bổ sung dễ dàng vào đồ uống hàng ngày.', 650000, 45, '2026-01-01', '2028-01-01', 0, 'https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&h=400&fit=crop', GETDATE()),
(426, 1, 3, N'Cao Actiso Khô Đà Lạt Nguyên Chất (Hộp 500g)', N'Cao actiso cô đặc nguyên chất, hỗ trợ gan mật, thanh nhiệt, giảm mỡ máu.', 280000, 65, '2026-02-01', '2028-02-01', 0, 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop', GETDATE()),
(427, 1, 3, N'Tinh Dầu Gừng Nguyên Chất Bách Thảo (10ml)', N'Tinh dầu gừng 100% nguyên chất, dùng xông tinh dầu, massage giảm đau, chống cảm lạnh.', 85000, 180, '2026-03-01', '2028-03-01', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),
(428, 1, 1, N'Viên Uống Tỏi Đen Lên Men Traphaco (Hộp 30 viên)', N'Tỏi đen lên men tự nhiên, tăng gấp đôi hoạt chất allicin, tốt cho tim mạch và miễn dịch.', 245000, 90, '2026-01-20', '2028-07-20', 0, 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop', GETDATE()),
(429, 1, 3, N'Trà Dây Thìa Canh Hỗ Trợ Tiểu Đường (Hộp 20 túi)', N'Chiết xuất lá dây thìa canh giúp kiểm soát đường huyết tự nhiên, an toàn và hiệu quả.', 58000, 160, '2026-04-01', '2028-04-01', 0, 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop', GETDATE()),
(430, 1, 2, N'Nước Uống Collagen Hàu Biển Tươi (Hộp 10 chai)', N'Collagen từ hàu biển tươi + vitamin C, hỗ trợ đẹp da, cải thiện sinh lý nam giới.', 420000, 70, '2026-03-10', '2027-09-10', 0, 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop', GETDATE()),

-- =================== SẢN PHẨM KHÁCH HÀNG YÊU CẦU (501) ===================
(501, 2, 3, N'Nước tắm thảo dược Sachi 250ml', N'Nước tắm gội thảo dược Sachi mang lại cảm giác dịu mát và hỗ trợ làm sạch nhẹ nhàng cho làn da trẻ nhỏ. Sản phẩm chứa chiết xuất lá tre cùng các thành phần như Glycerine, Purified water, phù hợp dùng hằng ngày, góp phần làm dịu da, hỗ trợ phòng ngừa rôm sảy và mang lại làn da sạch thoáng, dễ chịu cho bé.', 135000, 100, '2026-01-01', '2028-01-01', 0, 'https://cdn.nhathuoclongchau.com.vn/v1/static/nuoc_tam_thao_duoc_sachi_0_month_250ml_lam_diu_da_giup_phong_ngua-rom-say_00050586_1_33a0e9338c.png', GETDATE());

SET IDENTITY_INSERT Medicines OFF;
