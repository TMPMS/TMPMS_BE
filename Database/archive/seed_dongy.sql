-- ============================================================
-- SEED ĐÔNG Y - 20 sản phẩm Đông Y & Thảo Dược bổ sung
-- Chạy: psql -U postgres -d longchau -f database/seed_dongy.sql
-- ============================================================

INSERT INTO medicines (id, category_id, supplier_id, name, description, price, old_price, unit, discount, origin, packaging, stock_quantity, manufacture_date, expiry_date, requires_prescription, image_url) VALUES

-- ========== NHÓM THẢO DƯỢC / CAO THUỐC (category 1 = Thực phẩm chức năng) ==========
(301, 1, 1, 'An Cung Ngưu Hoàng Hoàn Traphaco (Hộp 1 viên)',
 'Hỗ trợ phục hồi sau tai biến mạch máu não, tăng cường tuần hoàn não, an thần, giảm căng thẳng.',
 280000, 310000, 'Hộp', 10, 'Việt Nam', 'Hộp 1 viên 3g', 60,
 '2026-01-01', '2028-01-01', FALSE,
 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop'),

(302, 1, 3, 'Cao Đinh Lăng Bách Thảo Dược (Lọ 250g)',
 'Bổ khí huyết, tăng cường sinh lực, hỗ trợ điều trị suy nhược cơ thể và mất ngủ.',
 135000, 155000, 'Lọ', 13, 'Việt Nam', 'Lọ 250g', 100,
 '2026-02-01', '2028-02-01', FALSE,
 'https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&h=400&fit=crop'),

(303, 1, 2, 'Viên Uống Tam Thất Bắc OPC (Hộp 60 viên)',
 'Hoạt huyết, tán ứ, cầm máu, hỗ trợ điều trị đau thắt ngực, thiếu máu cơ tim.',
 195000, 220000, 'Hộp', 11, 'Việt Nam', 'Hộp 60 viên', 85,
 '2026-01-15', '2028-07-15', FALSE,
 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop'),

(304, 1, 3, 'Trà Hà Thủ Ô Đỏ Bách Thảo (Hộp 20 túi lọc)',
 'Bổ gan thận, đen tóc, chống lão hóa, cải thiện chức năng sinh lý nam giới.',
 55000, NULL, 'Hộp', NULL, 'Việt Nam', 'Hộp 20 túi x 2g', 150,
 '2026-03-01', '2028-03-01', FALSE,
 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop'),

(305, 1, 1, 'Viên Nang Bạch Quả Ginkgo Traphaco (Hộp 60 viên)',
 'Tăng cường tuần hoàn máu não, cải thiện trí nhớ, giảm ù tai, chóng mặt do thiếu máu não.',
 145000, 165000, 'Hộp', 12, 'Việt Nam', 'Hộp 60 viên 40mg', 120,
 '2026-02-10', '2029-02-10', FALSE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

(306, 1, 3, 'Cao Trinh Nữ Hoàng Cung (Lọ 100ml)',
 'Hỗ trợ điều trị u xơ tử cung, u nang buồng trứng, điều hòa kinh nguyệt.',
 210000, 235000, 'Lọ', 11, 'Việt Nam', 'Lọ 100ml', 70,
 '2026-01-20', '2028-01-20', FALSE,
 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop'),

(307, 1, 2, 'Viên Linh Chi Đỏ OPC (Hộp 30 viên)',
 'Tăng cường miễn dịch, chống oxy hóa, hỗ trợ gan, cải thiện giấc ngủ và giảm căng thẳng.',
 320000, 360000, 'Hộp', 11, 'Việt Nam', 'Hộp 30 viên 500mg', 55,
 '2026-03-05', '2028-09-05', FALSE,
 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop'),

(308, 1, 3, 'Bột Nghệ Nano Curcumin Hữu Cơ (Hộp 30 gói)',
 'Chống viêm, chống loét dạ dày, làm đẹp da, hỗ trợ tiêu hóa và tăng cường miễn dịch.',
 185000, 210000, 'Hộp', 12, 'Việt Nam', 'Hộp 30 gói x 3g', 90,
 '2026-04-01', '2028-04-01', FALSE,
 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400&h=400&fit=crop'),

-- ========== NHÓM THUỐC ĐÔNG Y (category 3 = Thuốc) ==========
(309, 3, 1, 'Lục Vị Địa Hoàng Traphaco (Hộp 200 viên)',
 'Bổ thận âm, điều trị thận âm hư, đau lưng mỏi gối, di tinh, ù tai, hoa mắt chóng mặt.',
 175000, 195000, 'Hộp', 10, 'Việt Nam', 'Hộp 200 viên', 95,
 '2026-01-05', '2029-01-05', FALSE,
 'https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=400&h=400&fit=crop'),

(310, 3, 2, 'Bát Vị Quế Phụ OPC (Hộp 60 viên)',
 'Bổ thận dương, điều trị thận dương hư, đau lưng lạnh, tiểu đêm nhiều lần, liệt dương.',
 155000, 175000, 'Hộp', 11, 'Việt Nam', 'Hộp 60 viên', 80,
 '2026-02-20', '2029-02-20', FALSE,
 'https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=400&h=400&fit=crop'),

(311, 3, 1, 'Độc Hoạt Ký Sinh Traphaco (Hộp 100 viên)',
 'Khu phong thấp, tán hàn, điều trị đau khớp xương, tê bì chân tay do phong thấp.',
 125000, 140000, 'Hộp', 11, 'Việt Nam', 'Hộp 100 viên', 110,
 '2026-01-10', '2028-07-10', FALSE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

(312, 3, 2, 'Tiêu Dao Hoàn OPC (Hộp 200 viên)',
 'Sơ can giải uất, kiện tỳ hòa vị, điều trị viêm dạ dày mạn tính, rối loạn tiêu hóa do tâm lý.',
 110000, 125000, 'Hộp', 12, 'Việt Nam', 'Hộp 200 viên', 130,
 '2026-03-15', '2028-09-15', FALSE,
 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400&h=400&fit=crop'),

(313, 3, 3, 'Phong Tê Thấp Bách Thảo (Hộp 100 viên)',
 'Trừ phong thấp, hoạt lạc, điều trị viêm khớp dạng thấp, thoái hóa khớp gối, đau cổ vai gáy.',
 140000, 160000, 'Hộp', 13, 'Việt Nam', 'Hộp 100 viên', 100,
 '2026-02-05', '2028-08-05', FALSE,
 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=400&h=400&fit=crop'),

-- ========== NHÓM CAO DƯỢC LIỆU & NGÂM RƯỢu (category 1) ==========
(314, 1, 3, 'Cao Ích Mẫu Bách Thảo (Lọ 250ml)',
 'Điều hòa kinh nguyệt, hoạt huyết tán ứ, hỗ trợ điều trị kinh nguyệt không đều, đau bụng kinh.',
 175000, 195000, 'Lọ', 10, 'Việt Nam', 'Lọ 250ml', 75,
 '2026-01-25', '2028-01-25', FALSE,
 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400&h=400&fit=crop'),

(315, 1, 3, 'Rượu Thuốc Ngũ Gia Bì (Chai 500ml)',
 'Bổ can thận, mạnh gân cốt, trừ phong thấp, tăng cường sinh lực, phục hồi sức khỏe sau ốm.',
 230000, 260000, 'Chai', 12, 'Việt Nam', 'Chai 500ml', 50,
 '2025-12-01', '2028-12-01', FALSE,
 'https://images.unsplash.com/photo-1512290923902-8a9f81dc236c?w=400&h=400&fit=crop'),

(316, 1, 4, 'Hồng Sâm Củ Tươi KGC 6 Năm Tuổi (Hộp 150g)',
 'Bồi bổ nguyên khí, phục hồi sức khỏe, tăng cường miễn dịch, chống oxy hóa mạnh.',
 890000, 980000, 'Hộp', 9, 'Hàn Quốc', 'Hộp 150g (3-4 củ)', 30,
 '2025-11-01', '2027-11-01', FALSE,
 'https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&h=400&fit=crop'),

-- ========== NHÓM TRÀ THẢO DƯỢC (category 1) ==========
(317, 1, 3, 'Trà Diệp Hạ Châu Mát Gan (Hộp 20 túi)',
 'Thanh nhiệt giải độc, mát gan, hạ men gan, hỗ trợ viêm gan B mạn tính.',
 42000, NULL, 'Hộp', NULL, 'Việt Nam', 'Hộp 20 túi x 2g', 180,
 '2026-04-01', '2028-04-01', FALSE,
 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop'),

(318, 1, 3, 'Trà Khổ Qua Rừng Giảm Đường Huyết (Hộp 30 túi)',
 'Hỗ trợ hạ đường huyết tự nhiên, giảm mỡ máu, thanh nhiệt, phòng ngừa biến chứng tiểu đường.',
 65000, 75000, 'Hộp', 13, 'Việt Nam', 'Hộp 30 túi x 2g', 140,
 '2026-03-10', '2028-03-10', FALSE,
 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?w=400&h=400&fit=crop'),

(319, 1, 1, 'Viên Uống An Thần Traphaco (Hộp 50 viên)',
 'An thần, dưỡng tâm, hỗ trợ điều trị mất ngủ, lo âu, hồi hộp, suy nhược thần kinh.',
 98000, 115000, 'Hộp', 15, 'Việt Nam', 'Hộp 50 viên', 125,
 '2026-02-15', '2029-02-15', FALSE,
 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?w=400&h=400&fit=crop'),

(320, 1, 2, 'Hoàng Kỳ Chiết Xuất OPC (Hộp 60 viên)',
 'Bổ khí ích vệ, tăng cường miễn dịch, phòng cảm cúm theo mùa, hỗ trợ bệnh nhân sau hóa trị.',
 215000, 240000, 'Hộp', 10, 'Việt Nam', 'Hộp 60 viên 500mg', 65,
 '2026-01-30', '2029-01-30', FALSE,
 'https://images.unsplash.com/photo-1502082553048-f009c37129b9?w=400&h=400&fit=crop')

ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  price = EXCLUDED.price,
  old_price = EXCLUDED.old_price,
  image_url = EXCLUDED.image_url;

-- Reset sequence
SELECT setval('medicines_id_seq', COALESCE((SELECT MAX(id)+1 FROM medicines), 1), false);
