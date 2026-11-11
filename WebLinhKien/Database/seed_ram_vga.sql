/* =====================================================================
   FILE: seed_ram_vga.sql
   MÔ TẢ: Bổ sung dữ liệu RAM và VGA đảm bảo mỗi danh mục có >= 30 sản phẩm.
   TƯƠNG THÍCH:
     - Cấu trúc database WebLinhKien (schema.sql / database.sql / seed_sanpham_mau.sql)
     - Tính năng Build PC của thành viên C (Loại RAM, Dung lượng, Công suất tiêu thụ, Chiều dài)
     - Hỗ trợ thương hiệu bổ sung: NVIDIA, Colorful, Galax...
   NGUYÊN TẮC:
     - Hoàn toàn Idempotent: Kiểm tra NOT EXISTS trước khi thêm.
     - Không làm thay đổi hay trùng lặp dữ liệu đã có.
===================================================================== */

USE WebLinhKien;
GO

SET NOCOUNT ON;
GO

/* =====================================================================
   1. ĐẢM BẢO CÁC THƯƠNG HIỆU CẦN THIẾT TỒN TẠI
===================================================================== */
IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus WHERE MaThuongHieu = N'NVIDIA')
    INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu) VALUES (N'NVIDIA', N'NVIDIA');

IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus WHERE MaThuongHieu = N'COLORFUL')
    INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu) VALUES (N'COLORFUL', N'Colorful');

IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus WHERE MaThuongHieu = N'GALAX')
    INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu) VALUES (N'GALAX', N'GALAX');

IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus WHERE MaThuongHieu = N'GSKILL')
    INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu) VALUES (N'GSKILL', N'G.Skill');

IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus WHERE MaThuongHieu = N'TEAMGROUP')
    INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu) VALUES (N'TEAMGROUP', N'TeamGroup');

IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus WHERE MaThuongHieu = N'ADATA')
    INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu) VALUES (N'ADATA', N'Adata');

IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus WHERE MaThuongHieu = N'CRUCIAL')
    INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu) VALUES (N'CRUCIAL', N'Crucial');
GO

/* =====================================================================
   2. BỔ SUNG SẢN PHẨM RAM (Đa dạng DDR4, DDR5, 8GB, 16GB, 32GB, 64GB)
===================================================================== */
INSERT INTO dbo.SanPhams
    (MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
     AnhDaiDien, MoTaNgan, MoTa, BaoHanhThang, NoiBat)
SELECT v.Ma, v.Ten, v.DanhMuc, v.ThuongHieu, v.GiaGoc, v.GiaBan, v.TonKho,
       v.Anh, v.MoTaNgan, v.MoTa, v.BaoHanh, v.NoiBat
FROM (VALUES
    (N'RAM-KINGSTON-FURY-BEAST-RGB-16GB-2X8GB-DDR4-3200',
     N'RAM Kingston Fury Beast RGB 16GB (2 x 8GB) DDR4 3200MHz',
     N'RAM', N'KINGSTON', 1150000, 990000, 25,
     N'/images/products/ram-kingston-fury-beast-16gb-1x16gb-ddr5-5600mhz.jpg',
     N'Loại RAM: DDR4, Dung lượng: 16GB (2 x 8GB), Bus: 3200 MHz, LED RGB rực rỡ.',
     N'<p>RAM Kingston Fury Beast RGB 16GB DDR4 3200MHz mang lại hiệu năng ổn định cùng dải đèn RGB sống động hỗ trợ đồng bộ ánh sáng đa nền tảng.</p><ul><li>Loại RAM: DDR4</li><li>Dung lượng: 16GB (2 x 8GB)</li><li>Bus: 3200 MHz</li><li>Hỗ trợ Intel XMP 2.0</li></ul>',
     36, 1),

    (N'RAM-CORSAIR-DOMINATOR-TITANIUM-32GB-DDR5-6600',
     N'RAM Corsair Dominator Titanium RGB 32GB (2 x 16GB) DDR5 6600MHz White',
     N'RAM', N'CORSAIR', 4890000, 4490000, 15,
     N'/images/products/ram-corsair-vengeance-rgb-32gb-2x16gb-ddr5-6000mhz.jpg',
     N'Loại RAM: DDR5, Dung lượng: 32GB (2 x 16GB), Bus: 6600 MHz, Dòng Dominator Titanium cao cấp.',
     N'<p>Corsair Dominator Titanium RGB DDR5 kết hợp thiết kế sang trọng hàng đầu cùng xung nhịp cực cao 6600MHz, công nghệ làm mát DHX tản nhiệt tối ưu.</p><ul><li>Loại RAM: DDR5</li><li>Dung lượng: 32GB (2 x 16GB)</li><li>Bus: 6600 MHz</li><li>Đèn LED Capellix RGB siêu sáng</li></ul>',
     36, 1),

    (N'RAM-GSKILL-TRIDENT-Z5-RGB-64GB-2X32GB-DDR5-6400',
     N'RAM G.Skill Trident Z5 RGB 64GB (2 x 32GB) DDR5 6400MHz Silver',
     N'RAM', N'GSKILL', 6990000, 6450000, 12,
     N'/images/products/ram-g-skill-trident-z5-rgb-32gb-2x16gb-ddr5-f39a0.jpg',
     N'Loại RAM: DDR5, Dung lượng: 64GB (2 x 32GB), Bus: 6400 MHz, Thiết kế nhôm phay cao cấp.',
     N'<p>G.Skill Trident Z5 RGB DDR5 64GB mang đến hiệu năng ép xung vượt trội cho các dàn máy đồ họa chuyên nghiệp và gaming đỉnh cao.</p><ul><li>Loại RAM: DDR5</li><li>Dung lượng: 64GB (2 x 32GB)</li><li>Bus: 6400 MHz</li><li>Độ trễ tối ưu cho chipset Intel Z790/Z890</li></ul>',
     36, 1),

    (N'RAM-TEAMGROUP-T-FORCE-VULCAN-Z-8GB-1X8GB-DDR4-3200',
     N'RAM TeamGroup T-Force Vulcan Z 8GB (1 x 8GB) DDR4 3200MHz Red',
     N'RAM', N'TEAMGROUP', 490000, 420000, 40,
     N'/images/products/ram-teamgroup-t-force-vulcan-16gb-2x8gb-ddr4-ddc89.jpg',
     N'Loại RAM: DDR4, Dung lượng: 8GB (1 x 8GB), Bus: 3200 MHz, Giá thành tối ưu cho văn phòng & eSport.',
     N'<p>TeamGroup T-Force Vulcan Z DDR4 8GB với giáp tản nhiệt nhôm đỏ nổi bật, độ bền cao, thích hợp cho các bộ PC giá rẻ phổ thông.</p><ul><li>Loại RAM: DDR4</li><li>Dung lượng: 8GB (1 x 8GB)</li><li>Bus: 3200 MHz</li></ul>',
     36, 0),

    (N'RAM-ADATA-XPG-LANCER-BLADE-32GB-DDR5-6000',
     N'RAM Adata XPG Lancer Blade RGB 32GB (2 x 16GB) DDR5 6000MHz White',
     N'RAM', N'ADATA', 3390000, 3090000, 20,
     N'/images/products/ram-adata-xpg-lancer-32gb-2x16gb-ddr5-6000mhz.jpg',
     N'Loại RAM: DDR5, Dung lượng: 32GB (2 x 16GB), Bus: 6000 MHz, Cấu hình mỏng gọn tản nhiệt tốt.',
     N'<p>Adata XPG Lancer Blade RGB DDR5 với thiết kế tản nhiệt low-profile màu trắng tinh tế, dễ dàng tương thích với các bộ tản nhiệt khí kích thước lớn.</p><ul><li>Loại RAM: DDR5</li><li>Dung lượng: 32GB (2 x 16GB)</li><li>Bus: 6000 MHz</li><li>Hỗ trợ Intel XMP 3.0 & AMD EXPO</li></ul>',
     36, 0),

    (N'RAM-CRUCIAL-BASICS-8GB-1X8GB-DDR4-2666',
     N'RAM Crucial Basics 8GB (1 x 8GB) DDR4 2666MHz',
     N'RAM', N'CRUCIAL', 390000, 350000, 50,
     N'/images/products/ram-crucial-16gb-1x16gb-ddr4-3200mhz.jpg',
     N'Loại RAM: DDR4, Dung lượng: 8GB (1 x 8GB), Bus: 2666 MHz, Chuẩn OEM siêu bền.',
     N'<p>RAM Crucial 8GB DDR4 2666MHz đem lại sự ổn định tuyệt đối cho các hệ thống văn phòng và nâng cấp máy tính gia đình.</p><ul><li>Loại RAM: DDR4</li><li>Dung lượng: 8GB (1 x 8GB)</li><li>Bus: 2666 MHz</li></ul>',
     36, 0),

    (N'RAM-CORSAIR-VENGEANCE-RGB-64GB-2X32GB-DDR5-6000',
     N'RAM Corsair Vengeance RGB 64GB (2 x 32GB) DDR5 6000MHz White',
     N'RAM', N'CORSAIR', 6490000, 5990000, 10,
     N'/images/products/ram-corsair-vengeance-rgb-32gb-2x16gb-ddr4-3600mhz.jpg',
     N'Loại RAM: DDR5, Dung lượng: 64GB (2 x 32GB), Bus: 6000 MHz, Tone trắng hiện đại.',
     N'<p>Kit RAM Corsair Vengeance RGB 64GB DDR5 màu trắng cao cấp, cung cấp băng thông khổng lồ cho render 3D, dựng phim và chơi game AAA đa nhiệm mượt mà.</p><ul><li>Loại RAM: DDR5</li><li>Dung lượng: 64GB (2 x 32GB)</li><li>Bus: 6000 MHz</li></ul>',
     36, 0),

    (N'RAM-GSKILL-RIPJAWS-V-64GB-2X32GB-DDR4-3200',
     N'RAM G.Skill Ripjaws V 64GB (2 x 32GB) DDR4 3200MHz Black',
     N'RAM', N'GSKILL', 3590000, 3290000, 18,
     N'/images/products/ram-g-skill-ripjaws-v-32gb-2x16gb-ddr4-3600mhz.jpg',
     N'Loại RAM: DDR4, Dung lượng: 64GB (2 x 32GB), Bus: 3200 MHz, Dung lượng khủng DDR4.',
     N'<p>Dành riêng cho người dùng cần dung lượng RAM DDR4 tối đa trên các bo mạch chủ 2 hoặc 4 khe cắm với chi phí tiết kiệm nhất.</p><ul><li>Loại RAM: DDR4</li><li>Dung lượng: 64GB (2 x 32GB)</li><li>Bus: 3200 MHz</li></ul>',
     36, 0)
) AS v(Ma, Ten, DanhMuc, ThuongHieu, GiaGoc, GiaBan, TonKho, Anh, MoTaNgan, MoTa, BaoHanh, NoiBat)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhams sp WHERE sp.MaSanPham = v.Ma);
GO

/* =====================================================================
   3. BỔ SUNG SẢN PHẨM VGA (NVIDIA Founders Edition, Colorful, ASUS ROG, ZOTAC, MSI...)
===================================================================== */
INSERT INTO dbo.SanPhams
    (MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
     AnhDaiDien, MoTaNgan, MoTa, BaoHanhThang, NoiBat)
SELECT v.Ma, v.Ten, v.DanhMuc, v.ThuongHieu, v.GiaGoc, v.GiaBan, v.TonKho,
       v.Anh, v.MoTaNgan, v.MoTa, v.BaoHanh, v.NoiBat
FROM (VALUES
    (N'VGA-NVIDIA-GEFORCE-RTX-4090-FOUNDERS-EDITION-24GB',
     N'Card màn hình NVIDIA GeForce RTX 4090 Founders Edition 24GB GDDR6X',
     N'VGA', N'NVIDIA', 52990000, 49990000, 5,
     N'/images/products/vga-asus-tuf-gaming-geforce-rtx-4080-super-16gb.jpg',
     N'GPU: GeForce RTX 4090, 24GB GDDR6X, Phiên bản nguyên bản chính hãng NVIDIA Founders Edition.',
     N'<p>NVIDIA GeForce RTX 4090 Founders Edition là đỉnh cao của kiến trúc Ada Lovelace, thiết kế luồng gió thổi kép trứ danh, cung cấp hiệu năng đồ họa và AI mạnh mẽ nhất thế giới.</p><ul><li>GPU: GeForce RTX 4090</li><li>Bộ nhớ: 24GB GDDR6X</li><li>Cổng kết nối: 3 x DisplayPort 1.4a, 1 x HDMI 2.1a</li><li>Nguồn đề nghị: 850W</li></ul>',
     36, 1),

    (N'VGA-COLORFUL-IGAME-GEFORCE-RTX-4060-ULTRA-W-OC-8GB',
     N'Card màn hình Colorful iGame GeForce RTX 4060 Ultra W OC 8GB-V',
     N'VGA', N'COLORFUL', 8990000, 8290000, 15,
     N'/images/products/vga-gigabyte-geforce-rtx-4060-eagle-oc-8g.jpg',
     N'GPU: GeForce RTX 4060, 8GB GDDR6, Phong cách Pop-Art trắng hồng cá tính.',
     N'<p>Colorful iGame GeForce RTX 4060 Ultra W OC sở hữu thiết kế Pop-Art độc đáo, hỗ trợ nút bấm One-Key Overclock vật lý tiện lợi cho người dùng.</p><ul><li>GPU: GeForce RTX 4060</li><li>Bộ nhớ: 8GB GDDR6</li><li>Nguồn đề nghị: 500W</li><li>Chiều dài: 255 mm</li></ul>',
     36, 0),

    (N'VGA-COLORFUL-IGAME-RTX-4070-SUPER-VULCAN-W-12GB',
     N'Card màn hình Colorful iGame GeForce RTX 4070 SUPER Vulcan W OC 12GB-V',
     N'VGA', N'COLORFUL', 2150000, 19990000, 8,
     N'/images/products/vga-msi-geforce-rtx-4070-ventus-2x-12g-oc.jpg',
     N'GPU: GeForce RTX 4070 SUPER, 12GB GDDR6X, Tích hợp màn hình LCD hiển thị thông số.',
     N'<p>Dòng Vulcan cao cấp của Colorful với màn hình tháo rời iGame Smart LCD hiển thị nhiệt độ và hình ảnh tùy biến, tản nhiệt buồng hơi siêu mát mẻ.</p><ul><li>GPU: GeForce RTX 4070 SUPER</li><li>Bộ nhớ: 12GB GDDR6X</li><li>Nguồn đề nghị: 650W</li><li>Chiều dài: 310 mm</li></ul>',
     36, 1),

    (N'VGA-ASUS-ROG-STRIX-GEFORCE-RTX-4090-OC-24GB',
     N'Card màn hình ASUS ROG Strix GeForce RTX 4090 24GB GDDR6X OC Edition',
     N'VGA', N'ASUS', 58900000, 55490000, 4,
     N'/images/products/vga-asus-tuf-gaming-geforce-rtx-4080-super-16gb.jpg',
     N'GPU: GeForce RTX 4090, 24GB GDDR6X, Phiên bản ép xung ROG Strix đỉnh nóc.',
     N'<p>ASUS ROG Strix RTX 4090 OC mang khung kim loại đúc nguyên khối, quạt Axial-tech thế hệ mới tăng 23% lưu lượng khí, hệ thống tản nhiệt buồng hơi được cấp bằng sáng chế.</p><ul><li>GPU: GeForce RTX 4090</li><li>Bộ nhớ: 24GB GDDR6X</li><li>Nguồn đề nghị: 1000W</li><li>Chiều dài: 357 mm</li></ul>',
     36, 1),

    (N'VGA-MSI-GEFORCE-RTX-3050-VENTUS-2X-XS-8G-OC',
     N'Card màn hình MSI GeForce RTX 3050 VENTUS 2X XS 8G OC',
     N'VGA', N'MSI', 5790000, 5190000, 22,
     N'/images/products/vga-msi-rtx3060.jpg',
     N'GPU: GeForce RTX 3050, 8GB GDDR6, Phân khúc bình dân cân game Full HD.',
     N'<p>MSI GeForce RTX 3050 VENTUS 2X mang lại trải nghiệm đồ họa Ray Tracing và DLSS với mức giá dễ tiếp cận, thiết kế quạt tản TORX FAN 3.0 êm ái.</p><ul><li>GPU: GeForce RTX 3050</li><li>Bộ nhớ: 8GB GDDR6</li><li>Nguồn đề nghị: 500W</li><li>Chiều dài: 205 mm</li></ul>',
     36, 0),

    (N'VGA-GIGABYTE-RADEON-RX-7700-XT-GAMING-OC-12G',
     N'Card màn hình Gigabyte Radeon RX 7700 XT GAMING OC 12G',
     N'VGA', N'GIGABYTE', 12990000, 11990000, 14,
     N'/images/products/vga-giga-rx6600.jpg',
     N'GPU: Radeon RX 7700 XT, 12GB GDDR6, Kiến trúc RDNA 3 cực mạnh cho game 1440p.',
     N'<p>Trang bị hệ thống làm mát WINDFORCE 3 quạt 90mm quay đối xứng, ống đồng tiếp xúc trực tiếp GPU, tấm lưng kim loại bảo vệ mạch bền bỉ.</p><ul><li>GPU: Radeon RX 7700 XT</li><li>Bộ nhớ: 12GB GDDR6</li><li>Nguồn đề nghị: 700W</li><li>Chiều dài: 301 mm</li></ul>',
     36, 0),

    (N'VGA-ZOTAC-RTX-4070-TI-SUPER-TRINITY-BLACK-16GB',
     N'Card màn hình ZOTAC Gaming GeForce RTX 4070 Ti SUPER Trinity Black Edition 16GB GDDR6X',
     N'VGA', N'ZOTAC', 23990000, 21990000, 9,
     N'/images/products/vga-zotac-gaming-geforce-rtx-4060-twin-edge-8gb.jpg',
     N'GPU: GeForce RTX 4070 Ti SUPER, 16GB GDDR6X, Thiết kế khí động học IceStorm 2.0.',
     N'<p>ZOTAC GAMING RTX 4070 Ti SUPER Trinity sở hữu dung lượng VRAM 16GB GDDR6X rộng rãi cho tác vụ AI và chiến mượt mọi tựa game 2K/4K max settings.</p><ul><li>GPU: GeForce RTX 4070 Ti SUPER</li><li>Bộ nhớ: 16GB GDDR6X</li><li>Nguồn đề nghị: 750W</li><li>Chiều dài: 306 mm</li></ul>',
     36, 0),

    (N'VGA-NVIDIA-RTX-4080-SUPER-FOUNDERS-16GB',
     N'Card màn hình NVIDIA GeForce RTX 4080 SUPER Founders Edition 16GB GDDR6X',
     N'VGA', N'NVIDIA', 31990000, 29990000, 6,
     N'/images/products/vga-asus-tuf-gaming-geforce-rtx-4080-super-16gb.jpg',
     N'GPU: GeForce RTX 4080 SUPER, 16GB GDDR6X, Phiên bản chính hãng thiết kế nguyên bản.',
     N'<p>NVIDIA GeForce RTX 4080 SUPER Founders Edition sở hữu khung nhôm nguyên khối sơn đen mờ sang trọng, hỗ trợ công nghệ DLSS 3 Frame Generation đột phá.</p><ul><li>GPU: GeForce RTX 4080 SUPER</li><li>Bộ nhớ: 16GB GDDR6X</li><li>Nguồn đề nghị: 750W</li><li>Chiều dài: 304 mm</li></ul>',
     36, 1)
) AS v(Ma, Ten, DanhMuc, ThuongHieu, GiaGoc, GiaBan, TonKho, Anh, MoTaNgan, MoTa, BaoHanh, NoiBat)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhams sp WHERE sp.MaSanPham = v.Ma);
GO

/* =====================================================================
   4. BỔ SUNG THÔNG SỐ KỸ THUẬT (CHUẨN HÓA CHO TÍNH NĂNG BUILD PC CỦA THÀNH VIÊN C)
===================================================================== */
INSERT INTO dbo.ThongSoKyThuats (UidSanPham, TenThongSo, GiaTri, Ordering)
SELECT sp.Uid, v.Ten, v.GiaTri, v.Ord
FROM (VALUES
    -- RAM KINGSTON 16GB DDR4 3200
    (N'RAM-KINGSTON-FURY-BEAST-RGB-16GB-2X8GB-DDR4-3200', N'Loại RAM',             N'DDR4', 1),
    (N'RAM-KINGSTON-FURY-BEAST-RGB-16GB-2X8GB-DDR4-3200', N'Dung lượng',           N'16GB (2 x 8GB)', 2),
    (N'RAM-KINGSTON-FURY-BEAST-RGB-16GB-2X8GB-DDR4-3200', N'Bus',                  N'3200 MHz', 3),

    -- RAM CORSAIR TITANIUM 32GB DDR5 6600
    (N'RAM-CORSAIR-DOMINATOR-TITANIUM-32GB-DDR5-6600', N'Loại RAM',         N'DDR5', 1),
    (N'RAM-CORSAIR-DOMINATOR-TITANIUM-32GB-DDR5-6600', N'Dung lượng',       N'32GB (2 x 16GB)', 2),
    (N'RAM-CORSAIR-DOMINATOR-TITANIUM-32GB-DDR5-6600', N'Bus',              N'6600 MHz', 3),

    -- RAM GSKILL TRIDENT Z5 64GB DDR5 6400
    (N'RAM-GSKILL-TRIDENT-Z5-RGB-64GB-2X32GB-DDR5-6400', N'Loại RAM',              N'DDR5', 1),
    (N'RAM-GSKILL-TRIDENT-Z5-RGB-64GB-2X32GB-DDR5-6400', N'Dung lượng',            N'64GB (2 x 32GB)', 2),
    (N'RAM-GSKILL-TRIDENT-Z5-RGB-64GB-2X32GB-DDR5-6400', N'Bus',                   N'6400 MHz', 3),

    -- RAM TEAMGROUP VULCAN Z 8GB DDR4 3200
    (N'RAM-TEAMGROUP-T-FORCE-VULCAN-Z-8GB-1X8GB-DDR4-3200', N'Loại RAM',           N'DDR4', 1),
    (N'RAM-TEAMGROUP-T-FORCE-VULCAN-Z-8GB-1X8GB-DDR4-3200', N'Dung lượng',         N'8GB (1 x 8GB)', 2),
    (N'RAM-TEAMGROUP-T-FORCE-VULCAN-Z-8GB-1X8GB-DDR4-3200', N'Bus',                N'3200 MHz', 3),

    -- RAM ADATA LANCER BLADE 32GB DDR5 6000
    (N'RAM-ADATA-XPG-LANCER-BLADE-32GB-DDR5-6000', N'Loại RAM',         N'DDR5', 1),
    (N'RAM-ADATA-XPG-LANCER-BLADE-32GB-DDR5-6000', N'Dung lượng',       N'32GB (2 x 16GB)', 2),
    (N'RAM-ADATA-XPG-LANCER-BLADE-32GB-DDR5-6000', N'Bus',              N'6000 MHz', 3),

    -- RAM CRUCIAL BASICS 8GB DDR4 2666
    (N'RAM-CRUCIAL-BASICS-8GB-1X8GB-DDR4-2666', N'Loại RAM',                       N'DDR4', 1),
    (N'RAM-CRUCIAL-BASICS-8GB-1X8GB-DDR4-2666', N'Dung lượng',                     N'8GB (1 x 8GB)', 2),
    (N'RAM-CRUCIAL-BASICS-8GB-1X8GB-DDR4-2666', N'Bus',                            N'2666 MHz', 3),

    -- RAM CORSAIR VENGEANCE RGB 64GB DDR5 6000
    (N'RAM-CORSAIR-VENGEANCE-RGB-64GB-2X32GB-DDR5-6000', N'Loại RAM',              N'DDR5', 1),
    (N'RAM-CORSAIR-VENGEANCE-RGB-64GB-2X32GB-DDR5-6000', N'Dung lượng',            N'64GB (2 x 32GB)', 2),
    (N'RAM-CORSAIR-VENGEANCE-RGB-64GB-2X32GB-DDR5-6000', N'Bus',                   N'6000 MHz', 3),

    -- RAM GSKILL RIPJAWS V 64GB DDR4 3200
    (N'RAM-GSKILL-RIPJAWS-V-64GB-2X32GB-DDR4-3200', N'Loại RAM',                   N'DDR4', 1),
    (N'RAM-GSKILL-RIPJAWS-V-64GB-2X32GB-DDR4-3200', N'Dung lượng',                 N'64GB (2 x 32GB)', 2),
    (N'RAM-GSKILL-RIPJAWS-V-64GB-2X32GB-DDR4-3200', N'Bus',                        N'3200 MHz', 3),

    -- VGA NVIDIA RTX 4090 FE
    (N'VGA-NVIDIA-GEFORCE-RTX-4090-FOUNDERS-EDITION-24GB', N'GPU',                 N'GeForce RTX 4090', 1),
    (N'VGA-NVIDIA-GEFORCE-RTX-4090-FOUNDERS-EDITION-24GB', N'Bộ nhớ',              N'24GB GDDR6X', 2),
    (N'VGA-NVIDIA-GEFORCE-RTX-4090-FOUNDERS-EDITION-24GB', N'Nguồn đề nghị',       N'850W', 3),
    (N'VGA-NVIDIA-GEFORCE-RTX-4090-FOUNDERS-EDITION-24GB', N'Công suất tiêu thụ (W)', N'450W', 4),
    (N'VGA-NVIDIA-GEFORCE-RTX-4090-FOUNDERS-EDITION-24GB', N'Chiều dài',           N'304 mm', 5),
    (N'VGA-NVIDIA-GEFORCE-RTX-4090-FOUNDERS-EDITION-24GB', N'Chiều dài (mm)',      N'304 mm', 6),

    -- VGA COLORFUL RTX 4060 ULTRA W
    (N'VGA-COLORFUL-IGAME-GEFORCE-RTX-4060-ULTRA-W-OC-8GB', N'GPU',                N'GeForce RTX 4060', 1),
    (N'VGA-COLORFUL-IGAME-GEFORCE-RTX-4060-ULTRA-W-OC-8GB', N'Bộ nhớ',             N'8GB GDDR6', 2),
    (N'VGA-COLORFUL-IGAME-GEFORCE-RTX-4060-ULTRA-W-OC-8GB', N'Nguồn đề nghị',      N'500W', 3),
    (N'VGA-COLORFUL-IGAME-GEFORCE-RTX-4060-ULTRA-W-OC-8GB', N'Công suất tiêu thụ (W)', N'115W', 4),
    (N'VGA-COLORFUL-IGAME-GEFORCE-RTX-4060-ULTRA-W-OC-8GB', N'Chiều dài',          N'255 mm', 5),
    (N'VGA-COLORFUL-IGAME-GEFORCE-RTX-4060-ULTRA-W-OC-8GB', N'Chiều dài (mm)',     N'255 mm', 6),

    -- VGA COLORFUL RTX 4070 SUPER VULCAN
    (N'VGA-COLORFUL-IGAME-RTX-4070-SUPER-VULCAN-W-12GB', N'GPU',           N'GeForce RTX 4070 SUPER', 1),
    (N'VGA-COLORFUL-IGAME-RTX-4070-SUPER-VULCAN-W-12GB', N'Bộ nhớ',        N'12GB GDDR6X', 2),
    (N'VGA-COLORFUL-IGAME-RTX-4070-SUPER-VULCAN-W-12GB', N'Nguồn đề nghị', N'650W', 3),
    (N'VGA-COLORFUL-IGAME-RTX-4070-SUPER-VULCAN-W-12GB', N'Công suất tiêu thụ (W)', N'220W', 4),
    (N'VGA-COLORFUL-IGAME-RTX-4070-SUPER-VULCAN-W-12GB', N'Chiều dài',     N'310 mm', 5),
    (N'VGA-COLORFUL-IGAME-RTX-4070-SUPER-VULCAN-W-12GB', N'Chiều dài (mm)', N'310 mm', 6),

    -- VGA ASUS ROG STRIX RTX 4090 OC
    (N'VGA-ASUS-ROG-STRIX-GEFORCE-RTX-4090-OC-24GB', N'GPU',                       N'GeForce RTX 4090', 1),
    (N'VGA-ASUS-ROG-STRIX-GEFORCE-RTX-4090-OC-24GB', N'Bộ nhớ',                    N'24GB GDDR6X', 2),
    (N'VGA-ASUS-ROG-STRIX-GEFORCE-RTX-4090-OC-24GB', N'Nguồn đề nghị',              N'1000W', 3),
    (N'VGA-ASUS-ROG-STRIX-GEFORCE-RTX-4090-OC-24GB', N'Công suất tiêu thụ (W)',    N'450W', 4),
    (N'VGA-ASUS-ROG-STRIX-GEFORCE-RTX-4090-OC-24GB', N'Chiều dài',                 N'357 mm', 5),
    (N'VGA-ASUS-ROG-STRIX-GEFORCE-RTX-4090-OC-24GB', N'Chiều dài (mm)',            N'357 mm', 6),

    -- VGA MSI RTX 3050 VENTUS 2X XS 8G
    (N'VGA-MSI-GEFORCE-RTX-3050-VENTUS-2X-XS-8G-OC', N'GPU',                       N'GeForce RTX 3050', 1),
    (N'VGA-MSI-GEFORCE-RTX-3050-VENTUS-2X-XS-8G-OC', N'Bộ nhớ',                    N'8GB GDDR6', 2),
    (N'VGA-MSI-GEFORCE-RTX-3050-VENTUS-2X-XS-8G-OC', N'Nguồn đề nghị',             N'500W', 3),
    (N'VGA-MSI-GEFORCE-RTX-3050-VENTUS-2X-XS-8G-OC', N'Công suất tiêu thụ (W)',    N'115W', 4),
    (N'VGA-MSI-GEFORCE-RTX-3050-VENTUS-2X-XS-8G-OC', N'Chiều dài',                 N'205 mm', 5),
    (N'VGA-MSI-GEFORCE-RTX-3050-VENTUS-2X-XS-8G-OC', N'Chiều dài (mm)',            N'205 mm', 6),

    -- VGA GIGABYTE RX 7700 XT GAMING OC
    (N'VGA-GIGABYTE-RADEON-RX-7700-XT-GAMING-OC-12G', N'GPU',                      N'Radeon RX 7700 XT', 1),
    (N'VGA-GIGABYTE-RADEON-RX-7700-XT-GAMING-OC-12G', N'Bộ nhớ',                   N'12GB GDDR6', 2),
    (N'VGA-GIGABYTE-RADEON-RX-7700-XT-GAMING-OC-12G', N'Nguồn đề nghị',            N'700W', 3),
    (N'VGA-GIGABYTE-RADEON-RX-7700-XT-GAMING-OC-12G', N'Công suất tiêu thụ (W)',   N'245W', 4),
    (N'VGA-GIGABYTE-RADEON-RX-7700-XT-GAMING-OC-12G', N'Chiều dài',                N'301 mm', 5),
    (N'VGA-GIGABYTE-RADEON-RX-7700-XT-GAMING-OC-12G', N'Chiều dài (mm)',           N'301 mm', 6),

    -- VGA ZOTAC RTX 4070 TI SUPER TRINITY
    (N'VGA-ZOTAC-RTX-4070-TI-SUPER-TRINITY-BLACK-16GB', N'GPU',      N'GeForce RTX 4070 Ti SUPER', 1),
    (N'VGA-ZOTAC-RTX-4070-TI-SUPER-TRINITY-BLACK-16GB', N'Bộ nhớ',   N'16GB GDDR6X', 2),
    (N'VGA-ZOTAC-RTX-4070-TI-SUPER-TRINITY-BLACK-16GB', N'Nguồn đề nghị', N'750W', 3),
    (N'VGA-ZOTAC-RTX-4070-TI-SUPER-TRINITY-BLACK-16GB', N'Công suất tiêu thụ (W)', N'285W', 4),
    (N'VGA-ZOTAC-RTX-4070-TI-SUPER-TRINITY-BLACK-16GB', N'Chiều dài', N'306 mm', 5),
    (N'VGA-ZOTAC-RTX-4070-TI-SUPER-TRINITY-BLACK-16GB', N'Chiều dài (mm)', N'306 mm', 6),

    -- VGA NVIDIA RTX 4080 SUPER FE
    (N'VGA-NVIDIA-RTX-4080-SUPER-FOUNDERS-16GB', N'GPU',            N'GeForce RTX 4080 SUPER', 1),
    (N'VGA-NVIDIA-RTX-4080-SUPER-FOUNDERS-16GB', N'Bộ nhớ',         N'16GB GDDR6X', 2),
    (N'VGA-NVIDIA-RTX-4080-SUPER-FOUNDERS-16GB', N'Nguồn đề nghị',  N'750W', 3),
    (N'VGA-NVIDIA-RTX-4080-SUPER-FOUNDERS-16GB', N'Công suất tiêu thụ (W)', N'320W', 4),
    (N'VGA-NVIDIA-RTX-4080-SUPER-FOUNDERS-16GB', N'Chiều dài',      N'304 mm', 5),
    (N'VGA-NVIDIA-RTX-4080-SUPER-FOUNDERS-16GB', N'Chiều dài (mm)', N'304 mm', 6)
) AS v(Ma, Ten, GiaTri, Ord)
JOIN dbo.SanPhams sp ON sp.MaSanPham = v.Ma
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ThongSoKyThuats ts 
    WHERE ts.UidSanPham = sp.Uid AND ts.TenThongSo = v.Ten
);
GO

/* =====================================================================
   5. BỔ SUNG HÌNH ẢNH SẢN PHẨM (IDEMPOTENT)
===================================================================== */
INSERT INTO dbo.HinhAnhSanPhams (UidSanPham, DuongDan, Ordering)
SELECT s.Uid, s.AnhDaiDien, 1
FROM dbo.SanPhams s
WHERE s.AnhDaiDien IS NOT NULL
  AND s.MaDanhMuc IN (N'RAM', N'VGA')
  AND NOT EXISTS (SELECT 1 FROM dbo.HinhAnhSanPhams h WHERE h.UidSanPham = s.Uid);
GO

/* =====================================================================
   6. KIỂM TRA SỐ LƯỢNG SAU KHI CHẠY SEED
===================================================================== */
SELECT 
    dm.MaDanhMuc,
    dm.TenDanhMuc,
    COUNT(sp.Uid) AS TongSoSanPham,
    CASE WHEN COUNT(sp.Uid) >= 30 THEN N'ĐẠT CHUẨN (>= 30)' ELSE N'CHƯA ĐỦ (< 30)' END AS TrangThai
FROM dbo.DanhMucs dm
LEFT JOIN dbo.SanPhams sp ON sp.MaDanhMuc = dm.MaDanhMuc
WHERE dm.MaDanhMuc IN (N'RAM', N'VGA')
GROUP BY dm.MaDanhMuc, dm.TenDanhMuc;
GO

