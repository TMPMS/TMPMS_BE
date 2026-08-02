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
(101, 3, 1, N'Hoạt Huyết Dưỡng Não Traphaco (Hộp 5 vỉ x 20 viên)', N'Bổ não, tăng cường tuần hoàn não, giảm đau đầu, chóng mặt, suy giảm trí nhớ.', 95000, 150, '2026-01-01', '2029-01-01', 0, 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop', GETDATE()),
(102, 1, 3, N'Trà túi lọc Cà Gai Leo thải độc gan (Hộp 20 túi)', N'Hỗ trợ mát gan, giải độc gan, hạ men gan và phục hồi tế bào gan bị tổn thương.', 45000, 200, '2026-02-01', '2028-02-01', 0, 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop', GETDATE()),
(103, 3, 2, N'Kim Tiền Thảo trị sỏi thận OPC (Hộp 100 viên)', N'Thanh nhiệt, lợi niệu, tiêu sỏi, hỗ trợ điều trị sỏi đường tiết niệu, sỏi thận, sỏi mật.', 65000, 120, '2026-01-10', '2029-01-10', 0, 'https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=400&h=400&fit=crop', GETDATE()),
(104, 1, 3, N'Cao Xương Khớp Bách Thảo Dược (Lọ 100g)', N'Hỗ trợ mạnh gân cốt, giảm đau nhức xương khớp do thoái hóa hoặc phong thấp.', 180000, 80, '2026-03-01', '2028-03-01', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE()),
(105, 1, 3, N'Mật ong hoa rừng nguyên chất Tây Nguyên (Chai 50ml)', N'Mật ong thiên nhiên nguyên chất, hỗ trợ bồi bổ sức khỏe, làm dịu cổ họng và hỗ trợ tiêu hóa.', 120000, 90, '2026-04-01', '2029-04-01', 0, 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop', GETDATE()),
(106, 1, 3, N'Bột gừng mật ong sấy thăng hoa (Hộp 15 gói)', N'Làm ấm cơ thể, phòng cảm lạnh, giảm buồn nôn và tăng cường tiêu hóa.', 75000, 110, '2026-03-15', '2028-03-15', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),
(201, 1, 3, N'Đông Trùng Hạ Thảo Militaris sấy (Lọ 10g)', N'Bồi bổ cơ thể, tăng cường hệ miễn dịch, cải thiện sinh lực và hỗ trợ chức năng hô hấp.', 290000, 60, '2026-01-20', '2027-07-20', 0, 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop', GETDATE()),
(202, 1, 2, N'Cao Atiso Vân Anh Đà Lạt (Hộp 1kg)', N'Giải độc gan, lợi mật, giảm cholesterol, thanh nhiệt cơ thể và cải thiện giấc ngủ.', 220000, 75, '2026-02-15', '2028-02-15', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE()),
(203, 1, 4, N'Nhân sâm lát tẩm mật ong Hàn Quốc (Hộp 10 gói)', N'Tăng cường sức đề kháng, phục hồi sức khỏe, giảm căng thẳng mệt mỏi.', 350000, 50, '2026-01-05', '2029-01-05', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),
(204, 1, 4, N'Tinh chất hồng sâm KGC Everytime (Hộp 30 gói)', N'Chiết xuất hồng sâm 6 năm tuổi cô đặc cao cấp giúp cải thiện trí nhớ, tăng lưu thông máu.', 1450000, 40, '2026-03-10', '2029-03-10', 0, 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop', GETDATE()),
(205, 1, 3, N'Viên nghệ mật ong sữa chúa Tenchi (Hộp 250g)', N'Hỗ trợ viêm loét dạ dày, tá tràng, làm đẹp da và bồi bổ cơ thể.', 160000, 95, '2026-04-12', '2028-04-12', 0, 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop', GETDATE()),
(206, 1, 3, N'Dầu tràm nguyên chất Cung Đình Huế (Chai 50ml)', N'Phòng tránh gió máy, cảm cúm, sổ mũi, côn trùng cắn, thích hợp cho bé và bà mẹ sau sinh.', 125000, 130, '2026-05-01', '2031-05-01', 0, 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop', GETDATE());
SET IDENTITY_INSERT Medicines OFF;
