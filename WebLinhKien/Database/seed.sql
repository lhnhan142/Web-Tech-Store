/* =====================================================================
   WebLinhKien - seed.sql
   Dữ liệu mẫu. Chạy SAU schema.sql. Chạy lại nhiều lần không bị trùng.
   Giá và tồn kho chỉ là số minh họa, sửa lại theo ý nhóm.
   Tài khoản admin KHÔNG nằm ở đây vì cần hash mật khẩu bằng code C#.
   ===================================================================== */

USE WebLinhKien;
GO
SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------
   1. Tình trạng đơn hàng
   --------------------------------------------------------------------- */
INSERT INTO dbo.TinhTrangDonHangs (MaTinhTrang, TenTinhTrang, Ordering)
SELECT v.Ma, v.Ten, v.Ord
FROM (VALUES
    (N'MOI',            N'Mới đặt hàng',   1),
    (N'DANG_XU_LY',     N'Đang xử lý',     2),
    (N'DANG_GIAO_HANG', N'Đang giao hàng', 3),
    (N'DA_NHAN',        N'Đã nhận',        4),
    (N'HOAN_TRA',       N'Hoàn trả',       5),
    (N'DOI_HANG',       N'Đổi hàng',       6)
) AS v(Ma, Ten, Ord)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TinhTrangDonHangs t WHERE t.MaTinhTrang = v.Ma);
GO

/* ---------------------------------------------------------------------
   2. Cài đặt (đọc bằng Khoa, ép kiểu ở code C#)
   --------------------------------------------------------------------- */
INSERT INTO dbo.CaiDats (Khoa, GiaTri, MoTa)
SELECT v.Khoa, v.GiaTri, v.MoTa
FROM (VALUES
    (N'TenCuaHang',            N'WebLinhKien', N'Tên cửa hàng hiển thị trên website'),
    (N'PhiGiaoHang',           N'30000',       N'Phí giao hàng (VND)'),
    (N'NguongMienPhiGiaoHang', N'500000',      N'Đơn từ mức này trở lên được miễn phí giao hàng (VND)'),
    (N'NguongSapHetHang',      N'5',           N'Tồn kho từ mức này trở xuống thì cảnh báo sắp hết hàng')
) AS v(Khoa, GiaTri, MoTa)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CaiDats c WHERE c.Khoa = v.Khoa);
GO

/* ---------------------------------------------------------------------
   3. Danh mục
   --------------------------------------------------------------------- */
INSERT INTO dbo.DanhMucs (MaDanhMuc, TenDanhMuc, Ordering)
SELECT v.Ma, v.Ten, v.Ord
FROM (VALUES
    (N'CPU',       N'CPU - Bộ vi xử lý',              1),
    (N'MAINBOARD', N'Mainboard - Bo mạch chủ',        2),
    (N'RAM',       N'RAM - Bộ nhớ trong',             3),
    (N'VGA',       N'VGA - Card màn hình',            4),
    (N'SSD',       N'SSD - Ổ cứng thể rắn',           5),
    (N'PSU',       N'Nguồn máy tính',                 6),
    (N'CASE',      N'Vỏ case',                        7),
    (N'PHUKIEN',   N'Phụ kiện (chuột, bàn phím...)',  8)
) AS v(Ma, Ten, Ord)
WHERE NOT EXISTS (SELECT 1 FROM dbo.DanhMucs d WHERE d.MaDanhMuc = v.Ma);
GO

/* ---------------------------------------------------------------------
   4. Thương hiệu
   --------------------------------------------------------------------- */
INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu)
SELECT v.Ma, v.Ten
FROM (VALUES
    (N'INTEL',        N'Intel'),
    (N'AMD',          N'AMD'),
    (N'ASUS',         N'ASUS'),
    (N'MSI',          N'MSI'),
    (N'GIGABYTE',     N'Gigabyte'),
    (N'KINGSTON',     N'Kingston'),
    (N'CORSAIR',      N'Corsair'),
    (N'SAMSUNG',      N'Samsung'),
    (N'COOLERMASTER', N'Cooler Master'),
    (N'LOGITECH',     N'Logitech')
) AS v(Ma, Ten)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus t WHERE t.MaThuongHieu = v.Ma);
GO

/* ---------------------------------------------------------------------
   5. Sản phẩm mẫu (13 sản phẩm, đủ 8 danh mục)
   --------------------------------------------------------------------- */
INSERT INTO dbo.SanPhams
    (MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
     AnhDaiDien, MoTaNgan, MoTa, BaoHanhThang, NoiBat)
SELECT v.Ma, v.Ten, v.DanhMuc, v.ThuongHieu, v.GiaGoc, v.GiaBan, v.TonKho,
       v.Anh, v.MoTaNgan, v.MoTa, v.BaoHanh, v.NoiBat
FROM (VALUES
    (N'CPU-I5-12400F', N'CPU Intel Core i5-12400F', N'CPU', N'INTEL', 3290000, 2890000, 25,
     N'/images/products/cpu-i5-12400f.jpg', N'6 nhân 12 luồng, socket LGA1700, phù hợp máy chơi game tầm trung.',
     N'<p>CPU Intel Core i5-12400F có 6 nhân 12 luồng, không tích hợp card đồ họa nên cần lắp thêm VGA rời.</p>', 36, 1),

    (N'CPU-R5-5600', N'CPU AMD Ryzen 5 5600', N'CPU', N'AMD', 3490000, 3090000, 18,
     N'/images/products/cpu-r5-5600.jpg', N'6 nhân 12 luồng, socket AM4, hiệu năng tốt trong tầm giá.',
     N'<p>CPU AMD Ryzen 5 5600 dùng socket AM4, tương thích với các mainboard B450, B550.</p>', 36, 1),

    (N'MB-ASUS-B660M-K', N'Mainboard ASUS PRIME B660M-K D4', N'MAINBOARD', N'ASUS', 2390000, 2190000, 12,
     N'/images/products/mb-asus-b660m-k.jpg', N'Chipset B660, socket LGA1700, hỗ trợ RAM DDR4, cỡ Micro-ATX.',
     N'<p>Mainboard dành cho CPU Intel thế hệ 12, 13, 14 dùng RAM DDR4.</p>', 36, 0),

    (N'MB-MSI-B550M-MORTAR', N'Mainboard MSI MAG B550M MORTAR', N'MAINBOARD', N'MSI', 3190000, 2890000, 9,
     N'/images/products/mb-msi-b550m-mortar.jpg', N'Chipset B550, socket AM4, hỗ trợ RAM DDR4, cỡ Micro-ATX.',
     N'<p>Mainboard cho CPU AMD Ryzen dùng socket AM4.</p>', 36, 0),

    (N'RAM-KING-16G', N'RAM Kingston Fury Beast 16GB DDR4 3200MHz', N'RAM', N'KINGSTON', 890000, 790000, 40,
     N'/images/products/ram-kingston-16g.jpg', N'1 thanh 16GB, DDR4, bus 3200 MHz.',
     N'<p>RAM desktop DDR4 dung lượng 16GB, dùng được cho nhu cầu học tập, làm việc và chơi game.</p>', 36, 1),

    (N'RAM-COR-32G', N'RAM Corsair Vengeance LPX 32GB (2x16GB) DDR4 3200MHz', N'RAM', N'CORSAIR', 1790000, 1590000, 14,
     N'/images/products/ram-corsair-32g.jpg', N'Kit 2 thanh 16GB, DDR4, bus 3200 MHz, chạy dual channel.',
     N'<p>Bộ kit 32GB gồm 2 thanh 16GB, tản nhiệt nhôm thiết kế thấp, dễ lắp cùng tản CPU lớn.</p>', 36, 0),

    (N'VGA-MSI-RTX3060', N'Card màn hình MSI GeForce RTX 3060 VENTUS 2X 12G OC', N'VGA', N'MSI', 8490000, 7690000, 6,
     N'/images/products/vga-msi-rtx3060.jpg', N'12GB GDDR6, chơi tốt game full HD và 2K.',
     N'<p>Card đồ họa RTX 3060 bộ nhớ 12GB, hỗ trợ ray tracing và DLSS.</p>', 36, 1),

    (N'VGA-GIGA-RX6600', N'Card màn hình Gigabyte Radeon RX 6600 EAGLE 8G', N'VGA', N'GIGABYTE', 5990000, 5490000, 4,
     N'/images/products/vga-giga-rx6600.jpg', N'8GB GDDR6, hiệu năng tốt cho game full HD.',
     N'<p>Card đồ họa AMD Radeon RX 6600, tiêu thụ điện thấp, phù hợp nguồn 450W.</p>', 36, 0),

    (N'SSD-SAM-980-500', N'SSD Samsung 980 500GB NVMe M.2', N'SSD', N'SAMSUNG', 1290000, 1090000, 30,
     N'/images/products/ssd-samsung-980-500.jpg', N'500GB, chuẩn M.2 NVMe PCIe 3.0.',
     N'<p>Ổ SSD NVMe tốc độ cao, giúp khởi động Windows và mở phần mềm nhanh hơn nhiều so với ổ HDD.</p>', 60, 1),

    (N'SSD-KING-NV2-1T', N'SSD Kingston NV2 1TB NVMe M.2', N'SSD', N'KINGSTON', 1990000, 1790000, 22,
     N'/images/products/ssd-kingston-nv2-1t.jpg', N'1TB, chuẩn M.2 NVMe PCIe 4.0.',
     N'<p>Ổ SSD dung lượng 1TB, phù hợp làm ổ chính cho máy văn phòng và máy chơi game.</p>', 36, 0),

    (N'PSU-COR-CX650', N'Nguồn Corsair CX650 650W 80 Plus Bronze', N'PSU', N'CORSAIR', 1490000, 1290000, 15,
     N'/images/products/psu-corsair-cx650.jpg', N'650W, chuẩn 80 Plus Bronze.',
     N'<p>Nguồn máy tính công suất 650W, đủ cho cấu hình tầm trung dùng card đồ họa rời.</p>', 36, 0),

    (N'CASE-CM-Q300L', N'Vỏ case Cooler Master MasterBox Q300L', N'CASE', N'COOLERMASTER', 1090000, 890000, 10,
     N'/images/products/case-cm-q300l.jpg', N'Case nhỏ gọn, hỗ trợ Micro-ATX và Mini-ITX.',
     N'<p>Vỏ case có nắp hông trong suốt, kích thước nhỏ gọn, phù hợp bàn làm việc.</p>', 12, 0),

    (N'MOUSE-LOGI-G102', N'Chuột gaming Logitech G102 LIGHTSYNC', N'PHUKIEN', N'LOGITECH', 450000, 390000, 50,
     N'/images/products/mouse-logitech-g102.jpg', N'Cảm biến 8.000 DPI, LED RGB, kết nối có dây.',
     N'<p>Chuột gaming giá dễ tiếp cận, có phần mềm tùy chỉnh DPI và màu LED.</p>', 24, 0)
) AS v(Ma, Ten, DanhMuc, ThuongHieu, GiaGoc, GiaBan, TonKho, Anh, MoTaNgan, MoTa, BaoHanh, NoiBat)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhams s WHERE s.MaSanPham = v.Ma);
GO

/* ---------------------------------------------------------------------
   6. Thông số kỹ thuật (nối với sản phẩm bằng MaSanPham)
   --------------------------------------------------------------------- */
INSERT INTO dbo.ThongSoKyThuats (UidSanPham, TenThongSo, GiaTri, Ordering)
SELECT sp.Uid, v.Ten, v.GiaTri, v.Ord
FROM (VALUES
    (N'CPU-I5-12400F', N'Socket',               N'LGA1700',        1),
    (N'CPU-I5-12400F', N'Số nhân / luồng',      N'6 / 12',         2),
    (N'CPU-I5-12400F', N'Xung nhịp tối đa',     N'4.4 GHz',        3),
    (N'CPU-I5-12400F', N'TDP',                  N'65W',            4),

    (N'CPU-R5-5600',   N'Socket',               N'AM4',            1),
    (N'CPU-R5-5600',   N'Số nhân / luồng',      N'6 / 12',         2),
    (N'CPU-R5-5600',   N'Xung nhịp tối đa',     N'4.4 GHz',        3),
    (N'CPU-R5-5600',   N'TDP',                  N'65W',            4),

    (N'MB-ASUS-B660M-K',      N'Socket',        N'LGA1700',        1),
    (N'MB-ASUS-B660M-K',      N'Chipset',       N'Intel B660',     2),
    (N'MB-ASUS-B660M-K',      N'Loại RAM hỗ trợ', N'DDR4',         3),
    (N'MB-ASUS-B660M-K',      N'Kích thước',    N'Micro-ATX',      4),

    (N'MB-MSI-B550M-MORTAR',  N'Socket',        N'AM4',            1),
    (N'MB-MSI-B550M-MORTAR',  N'Chipset',       N'AMD B550',       2),
    (N'MB-MSI-B550M-MORTAR',  N'Loại RAM hỗ trợ', N'DDR4',         3),
    (N'MB-MSI-B550M-MORTAR',  N'Kích thước',    N'Micro-ATX',      4),

    (N'RAM-KING-16G',  N'Dung lượng',           N'16GB (1 x 16GB)', 1),
    (N'RAM-KING-16G',  N'Loại RAM',             N'DDR4',           2),
    (N'RAM-KING-16G',  N'Bus',                  N'3200 MHz',       3),

    (N'RAM-COR-32G',   N'Dung lượng',           N'32GB (2 x 16GB)', 1),
    (N'RAM-COR-32G',   N'Loại RAM',             N'DDR4',           2),
    (N'RAM-COR-32G',   N'Bus',                  N'3200 MHz',       3),

    (N'VGA-MSI-RTX3060',  N'GPU',               N'GeForce RTX 3060', 1),
    (N'VGA-MSI-RTX3060',  N'Bộ nhớ',            N'12GB GDDR6',     2),
    (N'VGA-MSI-RTX3060',  N'Nguồn đề nghị',     N'550W',           3),

    (N'VGA-GIGA-RX6600',  N'GPU',               N'Radeon RX 6600', 1),
    (N'VGA-GIGA-RX6600',  N'Bộ nhớ',            N'8GB GDDR6',      2),
    (N'VGA-GIGA-RX6600',  N'Nguồn đề nghị',     N'450W',           3),

    (N'SSD-SAM-980-500',  N'Dung lượng',        N'500GB',          1),
    (N'SSD-SAM-980-500',  N'Chuẩn kết nối',     N'M.2 NVMe PCIe 3.0 x4', 2),
    (N'SSD-SAM-980-500',  N'Tốc độ đọc tối đa', N'3.100 MB/s',     3),
    (N'SSD-SAM-980-500',  N'Tốc độ ghi tối đa', N'2.600 MB/s',     4),

    (N'SSD-KING-NV2-1T',  N'Dung lượng',        N'1TB',            1),
    (N'SSD-KING-NV2-1T',  N'Chuẩn kết nối',     N'M.2 NVMe PCIe 4.0 x4', 2),
    (N'SSD-KING-NV2-1T',  N'Tốc độ đọc tối đa', N'3.500 MB/s',     3),
    (N'SSD-KING-NV2-1T',  N'Tốc độ ghi tối đa', N'2.100 MB/s',     4),

    (N'PSU-COR-CX650',    N'Công suất',         N'650W',           1),
    (N'PSU-COR-CX650',    N'Chuẩn hiệu suất',   N'80 Plus Bronze', 2),
    (N'PSU-COR-CX650',    N'Quạt làm mát',      N'120 mm',         3),

    (N'CASE-CM-Q300L',    N'Hỗ trợ mainboard',  N'Micro-ATX, Mini-ITX', 1),
    (N'CASE-CM-Q300L',    N'Nắp hông',          N'Acrylic trong suốt',  2),

    (N'MOUSE-LOGI-G102',  N'Cảm biến',          N'8.000 DPI',      1),
    (N'MOUSE-LOGI-G102',  N'Kết nối',           N'Có dây USB',     2),
    (N'MOUSE-LOGI-G102',  N'Đèn LED',           N'RGB LIGHTSYNC',  3)
) AS v(Ma, Ten, GiaTri, Ord)
JOIN dbo.SanPhams sp ON sp.MaSanPham = v.Ma
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ThongSoKyThuats t
    WHERE t.UidSanPham = sp.Uid AND t.TenThongSo = v.Ten
);
GO

/* ---------------------------------------------------------------------
   7. Hình ảnh: mỗi sản phẩm có sẵn 1 ảnh = ảnh đại diện
      (thêm ảnh phụ sau qua trang quản trị)
   --------------------------------------------------------------------- */
INSERT INTO dbo.HinhAnhSanPhams (UidSanPham, DuongDan, Ordering)
SELECT s.Uid, s.AnhDaiDien, 1
FROM dbo.SanPhams s
WHERE s.AnhDaiDien IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.HinhAnhSanPhams h WHERE h.UidSanPham = s.Uid);
GO

/* Kiểm tra nhanh sau khi chạy */
SELECT N'DanhMucs' AS Bang, COUNT(*) AS SoDong FROM dbo.DanhMucs
UNION ALL SELECT N'ThuongHieus',       COUNT(*) FROM dbo.ThuongHieus
UNION ALL SELECT N'SanPhams',          COUNT(*) FROM dbo.SanPhams
UNION ALL SELECT N'ThongSoKyThuats',   COUNT(*) FROM dbo.ThongSoKyThuats
UNION ALL SELECT N'HinhAnhSanPhams',   COUNT(*) FROM dbo.HinhAnhSanPhams
UNION ALL SELECT N'TinhTrangDonHangs', COUNT(*) FROM dbo.TinhTrangDonHangs
UNION ALL SELECT N'CaiDats',           COUNT(*) FROM dbo.CaiDats;
GO
