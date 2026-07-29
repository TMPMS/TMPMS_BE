-- ============================================================
-- SEED SẢN PHẨM BÁN THÊM - Đa dạng danh mục (ID 401-460)
-- ============================================================

INSERT INTO medicines (id, category_id, supplier_id, name, description, price, old_price, unit, discount, origin, packaging, stock_quantity, manufacture_date, expiry_date, requires_prescription, image_url) VALUES

-- ========== THỰC PHẨM CHỨC NĂNG (category 1) ==========
(401, 1, 1, 'Vitamin C 1000mg Traphaco Sủi (Tuýp 20 viên)',
 'Bổ sung vitamin C, tăng cường đề kháng, chống oxy hóa, làm đẹp da và hỗ trợ hấp thu sắt.',
 45000, 52000, 'Tuýp', 13, 'Việt Nam', 'Tuýp 20 viên sủi', 300,
 '2026-01-01', '2028-01-01', FALSE,
 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop'),

(402, 1, 2, 'Collagen Peptide Nhật Bản Fine (Hộp 30 gói)',
 'Bổ sung collagen thủy phân, làm đẹp da, chống lão hóa, cải thiện độ đàn hồi da.',
 580000, 650000, 'Hộp', 11, 'Nhật Bản', 'Hộp 30 gói x 5g', 80,
 '2026-02-01', '2028-02-01', FALSE,
 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop'),

(403, 1, 3, 'Omega-3 Fish Oil 1000mg (Hộp 100 viên)',
 'Bổ sung DHA + EPA, tốt cho tim mạch, não bộ, giảm triglyceride và chống viêm.',
 185000, 210000, 'Hộp', 12, 'Việt Nam', 'Hộp 100 viên softgel', 150,
 '2026-01-15', '2028-07-15', FALSE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

(404, 1, 1, 'Canxi D3 K2 Traphaco (Hộp 60 viên)',
 'Bổ sung canxi, vitamin D3 và K2 giúp xương chắc khỏe, phòng loãng xương, hỗ trợ phát triển chiều cao.',
 165000, 185000, 'Hộp', 11, 'Việt Nam', 'Hộp 60 viên nhai', 200,
 '2026-03-01', '2029-03-01', FALSE,
 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop'),

(405, 1, 4, 'Viên Uống Bổ Não DHA + Ginkgo KGC (Hộp 30 viên)',
 'Tăng cường trí nhớ, cải thiện sự tập trung, hỗ trợ tuần hoàn não và giảm suy giảm nhận thức.',
 390000, 430000, 'Hộp', 9, 'Hàn Quốc', 'Hộp 30 viên', 60,
 '2026-01-20', '2029-01-20', FALSE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

(406, 1, 2, 'Men Vi Sinh Probiotic 10 tỷ khuẩn OPC (Hộp 30 gói)',
 'Cân bằng hệ vi sinh đường ruột, hỗ trợ tiêu hóa, giảm đầy bụng khó tiêu và tăng miễn dịch.',
 220000, 250000, 'Hộp', 12, 'Việt Nam', 'Hộp 30 gói', 120,
 '2026-04-01', '2028-04-01', FALSE,
 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop'),

(407, 1, 3, 'Yến Sào Thiên Nhiên Khánh Hòa (Hộp 70g)',
 'Bồi bổ cơ thể, tăng cường sức đề kháng, hỗ trợ hô hấp và phục hồi sức khỏe.',
 1250000, 1380000, 'Hộp', 9, 'Việt Nam', 'Hộp 70g (2 lọ)', 25,
 '2025-12-01', '2027-06-01', FALSE,
 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop'),

(408, 1, 1, 'Sắt Hữu Cơ Ferrous Bisglycinate Traphaco (Hộp 30 viên)',
 'Bổ sung sắt hữu cơ dễ hấp thu, không gây táo bón, hỗ trợ điều trị thiếu máu thiếu sắt.',
 95000, 110000, 'Hộp', 14, 'Việt Nam', 'Hộp 30 viên', 180,
 '2026-02-10', '2029-02-10', FALSE,
 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop'),

-- ========== DƯỢC MỸ PHẨM (category 2) ==========
(409, 2, 2, 'Kem Chống Nắng La Roche-Posay SPF50+ (50ml)',
 'Bảo vệ da khỏi tia UV, không gây nhờn, phù hợp da nhạy cảm và có vấn đề về da.',
 480000, 530000, 'Tuýp', 9, 'Pháp', 'Tuýp 50ml', 90,
 '2025-11-01', '2027-11-01', FALSE,
 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop'),

(410, 2, 2, 'Serum Vitamin C 20% Làm Sáng Da (Lọ 30ml)',
 'Làm sáng da, mờ thâm nám, chống oxy hóa mạnh và kích thích collagen.',
 320000, 365000, 'Lọ', 12, 'Pháp', 'Lọ 30ml', 70,
 '2026-01-01', '2028-01-01', FALSE,
 'https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=400&h=400&fit=crop'),

(411, 2, 3, 'Sữa Rửa Mặt Nghệ Hữu Cơ Dưỡng Da (150ml)',
 'Làm sạch da, dưỡng ẩm, chống viêm từ tinh chất nghệ hữu cơ, phù hợp mọi loại da.',
 95000, 110000, 'Chai', 14, 'Việt Nam', 'Chai 150ml', 200,
 '2026-03-01', '2028-03-01', FALSE,
 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop'),

(412, 2, 1, 'Toner Trà Xanh Matcha Dưỡng Ẩm (200ml)',
 'Cân bằng độ ẩm, thu nhỏ lỗ chân lông, làm dịu kích ứng và cấp ẩm sâu.',
 145000, 165000, 'Chai', 12, 'Việt Nam', 'Chai 200ml', 160,
 '2026-02-15', '2028-02-15', FALSE,
 'https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=400&h=400&fit=crop'),

-- ========== THUỐC (category 3) ==========
(413, 3, 2, 'Paracetamol 500mg OPC (Hộp 100 viên)',
 'Hạ sốt, giảm đau đầu, đau răng, đau cơ. Thành phần: Paracetamol 500mg.',
 25000, NULL, 'Hộp', NULL, 'Việt Nam', 'Hộp 10 vỉ x 10 viên', 500,
 '2026-01-01', '2028-01-01', FALSE,
 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop'),

(414, 3, 1, 'Berberin Traphaco Hỗ Trợ Tiêu Hóa (Hộp 100 viên)',
 'Kháng khuẩn đường ruột, điều trị tiêu chảy, đau bụng do rối loạn tiêu hóa.',
 32000, NULL, 'Hộp', NULL, 'Việt Nam', 'Hộp 10 vỉ x 10 viên', 400,
 '2026-02-01', '2028-02-01', FALSE,
 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop'),

(415, 3, 2, 'Cetirizine 10mg Chống Dị Ứng OPC (Hộp 30 viên)',
 'Chống dị ứng, giảm ngứa, viêm mũi dị ứng, mề đay, không gây buồn ngủ mạnh.',
 48000, 55000, 'Hộp', 13, 'Việt Nam', 'Hộp 3 vỉ x 10 viên', 250,
 '2026-01-10', '2028-07-10', FALSE,
 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop'),

(416, 3, 1, 'Esomeprazol 20mg Traphaco Dạ Dày (Hộp 28 viên)',
 'Ức chế bơm proton, điều trị viêm loét dạ dày tá tràng, trào ngược dạ dày-thực quản.',
 85000, 98000, 'Hộp', 13, 'Việt Nam', 'Hộp 2 vỉ x 14 viên', 180,
 '2026-03-01', '2028-03-01', TRUE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

-- ========== CHĂM SÓC CÁ NHÂN (category 4) ==========
(417, 4, 3, 'Dầu Gội Bồ Kết Thảo Dược Bách Thảo (400ml)',
 'Gội đầu từ bồ kết, bưởi và thảo mộc thiên nhiên, làm sạch da đầu, giảm rụng tóc.',
 75000, 88000, 'Chai', 15, 'Việt Nam', 'Chai 400ml', 200,
 '2026-04-01', '2028-04-01', FALSE,
 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop'),

(418, 4, 3, 'Sữa Tắm Tinh Chất Gừng Ấm Người (400ml)',
 'Sữa tắm thảo dược từ gừng tươi, làm ấm cơ thể, kháng khuẩn và dưỡng ẩm da.',
 65000, 75000, 'Chai', 13, 'Việt Nam', 'Chai 400ml', 250,
 '2026-03-15', '2028-03-15', FALSE,
 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop'),

(419, 4, 1, 'Kem Dưỡng Thể Tinh Chất Tràm Hương (200ml)',
 'Dưỡng ẩm toàn thân, làm mềm da, hương tràm thư giãn, thích hợp dùng sau tắm.',
 120000, 135000, 'Hũ', 11, 'Việt Nam', 'Hũ 200ml', 150,
 '2026-02-20', '2028-02-20', FALSE,
 'https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=400&h=400&fit=crop'),

(420, 4, 2, 'Kem Đánh Răng Than Hoạt Tính Trắng Răng (120g)',
 'Làm trắng răng tự nhiên với than hoạt tính, diệt khuẩn, thơm miệng và bảo vệ men răng.',
 55000, 62000, 'Tuýp', 11, 'Việt Nam', 'Tuýp 120g', 300,
 '2026-01-05', '2028-01-05', FALSE,
 'https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400&h=400&fit=crop'),

-- ========== THIẾT BỊ Y TẾ (category 5) ==========
(421, 5, 2, 'Máy Đo Huyết Áp Bắp Tay Omron HEM-7156 (Cái)',
 'Máy đo huyết áp tự động bắp tay, chính xác cao, phát hiện rung nhĩ, bộ nhớ 60 lần.',
 1250000, 1380000, 'Cái', 9, 'Nhật Bản', '1 máy + phụ kiện', 30,
 '2025-10-01', '2030-10-01', FALSE,
 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop'),

(422, 5, 3, 'Nhiệt Kế Điện Tử Kẹp Nách (Cái)',
 'Nhiệt kế điện tử đo nách/miệng/hậu môn, kết quả trong 60 giây, bộ nhớ nhiệt độ cuối.',
 95000, 110000, 'Cái', 14, 'Việt Nam', '1 cái', 100,
 '2025-06-01', '2030-06-01', FALSE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

(423, 5, 2, 'Máy Đo Đường Huyết Accu-Chek Active (Bộ)',
 'Bộ máy đo đường huyết kèm 10 que thử, kết quả 5 giây, bộ nhớ 500 lần, dùng cho người tiểu đường.',
 680000, 750000, 'Bộ', 9, 'Đức', 'Máy + 10 que + kim', 40,
 '2025-08-01', '2030-08-01', FALSE,
 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop'),

(424, 5, 3, 'Máy Xông Mũi Họng Khí Dung (Cái)',
 'Máy xông khí dung điều trị viêm mũi, viêm họng, hen suyễn, hạt phun mịn 3-5 micron.',
 450000, 500000, 'Cái', 10, 'Việt Nam', '1 bộ đầy đủ', 55,
 '2025-09-01', '2030-09-01', FALSE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

-- ========== THÊM ĐÔNG Y CAO CẤP (category 1) ==========
(425, 1, 4, 'Bột Sâm Hàn Quốc Nguyên Chất KGC (Hộp 30g)',
 'Bột hồng sâm nguyên chất 100%, hòa tan nhanh, bổ sung dễ dàng vào đồ uống hàng ngày.',
 650000, 720000, 'Hộp', 10, 'Hàn Quốc', 'Hộp 30g', 45,
 '2026-01-01', '2028-01-01', FALSE,
 'https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&h=400&fit=crop'),

(426, 1, 3, 'Cao Actiso Khô Đà Lạt Nguyên Chất (Hộp 500g)',
 'Cao actiso cô đặc nguyên chất, hỗ trợ gan mật, thanh nhiệt, giảm mỡ máu.',
 280000, 315000, 'Hộp', 11, 'Việt Nam', 'Hộp 500g', 65,
 '2026-02-01', '2028-02-01', FALSE,
 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop'),

(427, 1, 3, 'Tinh Dầu Gừng Nguyên Chất Bách Thảo (10ml)',
 'Tinh dầu gừng 100% nguyên chất, dùng xông tinh dầu, massage giảm đau, chống cảm lạnh.',
 85000, 95000, 'Lọ', 11, 'Việt Nam', 'Lọ 10ml', 180,
 '2026-03-01', '2028-03-01', FALSE,
 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop'),

(428, 1, 1, 'Viên Uống Tỏi Đen Lên Men Traphaco (Hộp 30 viên)',
 'Tỏi đen lên men tự nhiên, tăng gấp đôi hoạt chất allicin, tốt cho tim mạch và miễn dịch.',
 245000, 275000, 'Hộp', 11, 'Việt Nam', 'Hộp 30 viên', 90,
 '2026-01-20', '2028-07-20', FALSE,
 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop'),

(429, 1, 3, 'Trà Dây Thìa Canh Hỗ Trợ Tiểu Đường (Hộp 20 túi)',
 'Chiết xuất lá dây thìa canh giúp kiểm soát đường huyết tự nhiên, an toàn và hiệu quả.',
 58000, NULL, 'Hộp', NULL, 'Việt Nam', 'Hộp 20 túi x 2g', 160,
 '2026-04-01', '2028-04-01', FALSE,
 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop'),

(430, 1, 2, 'Nước Uống Collagen Hàu Biển Tươi (Hộp 10 chai)',
 'Collagen từ hàu biển tươi + vitamin C, hỗ trợ đẹp da, cải thiện sinh lý nam giới.',
 420000, 475000, 'Hộp', 12, 'Việt Nam', 'Hộp 10 chai x 50ml', 70,
 '2026-03-10', '2027-09-10', FALSE,
 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop')

ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  price = EXCLUDED.price,
  old_price = EXCLUDED.old_price,
  image_url = EXCLUDED.image_url,
  stock_quantity = EXCLUDED.stock_quantity;

-- Reset sequence
SELECT setval('medicines_id_seq', COALESCE((SELECT MAX(id)+1 FROM medicines), 1), false);

-- Xác nhận
SELECT category_id, COUNT(*) as so_luong
FROM medicines
GROUP BY category_id
ORDER BY category_id;
