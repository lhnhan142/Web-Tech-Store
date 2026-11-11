/* =====================================================================
   FILE: seed_case_phukien.sql   (thành viên D)
   MÔ TẢ: Bổ sung CASE và PHUKIEN để mỗi danh mục có >= 30 sản phẩm, và bổ sung
          tên thông số theo quy ước chung (ThongSoKyThuats) cho cả sản phẩm đã có:
            CASE    : Form hỗ trợ (ATX, mATX, ITX), Chiều dài VGA tối đa (mm)
            PHUKIEN : Loại phụ kiện
   NGUYÊN TẮC: chạy lại nhiều lần không trùng (NOT EXISTS), không sửa database.sql.
   GHI CHÚ: vài sản phẩm cố ý để tồn kho 0 và 2-3 để thử lọc "sắp hết / hết hàng" trong admin.
             Ảnh dùng lại ảnh mẫu có sẵn trong wwwroot/images/products.
===================================================================== */

USE WebLinhKien;
GO

SET NOCOUNT ON;
GO

/* 1. Thương hiệu cần dùng (thêm nếu thiếu) */
INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu)
SELECT v.Ma, v.Ten
FROM (VALUES
    (N'ASUS', N'ASUS'), (N'MSI', N'MSI'), (N'GIGABYTE', N'Gigabyte'), (N'NZXT', N'NZXT'),
    (N'THERMALTAKE', N'Thermaltake'), (N'FRACTAL', N'Fractal Design'), (N'CORSAIR', N'Corsair'),
    (N'LOGITECH', N'Logitech'), (N'STEELSERIES', N'SteelSeries'), (N'RAZER', N'Razer'),
    (N'LG', N'LG'), (N'SAMSUNG', N'Samsung')
) AS v(Ma, Ten)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus t WHERE t.MaThuongHieu = v.Ma);
GO

/* 2. Sản phẩm mới */
INSERT INTO dbo.SanPhams
    (MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
     AnhDaiDien, MoTaNgan, MoTa, BaoHanhThang, NoiBat)
SELECT v.Ma, v.Ten, v.DanhMuc, v.ThuongHieu, v.GiaGoc, v.GiaBan, v.TonKho,
       v.Anh, v.MoTaNgan, v.MoTa, v.BaoHanh, 0
FROM (VALUES

    (N'CASE-ASUS-TUF-GAMING-GT301', N'Vỏ case ASUS TUF Gaming GT301', N'CASE', N'ASUS', 2290000, 2090000, 14, N'/images/products/case-corsair-4000d-airflow.jpg', N'Form hỗ trợ: ATX, mATX, ITX, Chiều dài VGA tối đa: 320 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case ASUS TUF Gaming GT301.</p><ul><li>Form hỗ trợ: ATX, mATX, ITX</li><li>Chiều dài VGA tối đa: 320 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-ASUS-PRIME-AP201', N'Vỏ case ASUS Prime AP201', N'CASE', N'ASUS', 1690000, 1590000, 22, N'/images/products/case-deepcool-cc560.jpg', N'Form hỗ trợ: mATX, ITX, Chiều dài VGA tối đa: 338 mm, Nắp hông: Lưới thép', N'<p>Vỏ case ASUS Prime AP201.</p><ul><li>Form hỗ trợ: mATX, ITX</li><li>Chiều dài VGA tối đa: 338 mm</li><li>Nắp hông: Lưới thép</li></ul>', 12),
    (N'CASE-MSI-MAG-FORGE-100R', N'Vỏ case MSI MAG Forge 100R', N'CASE', N'MSI', 1290000, 1190000, 30, N'/images/products/case-montech-air-903-max.jpg', N'Form hỗ trợ: ATX, mATX, ITX, Chiều dài VGA tối đa: 330 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case MSI MAG Forge 100R.</p><ul><li>Form hỗ trợ: ATX, mATX, ITX</li><li>Chiều dài VGA tối đa: 330 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-MSI-MPG-GUNGNIR-110R', N'Vỏ case MSI MPG Gungnir 110R', N'CASE', N'MSI', 2590000, 2390000, 3, N'/images/products/case-nzxt-h510.jpg', N'Form hỗ trợ: ATX, mATX, ITX, Chiều dài VGA tối đa: 400 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case MSI MPG Gungnir 110R.</p><ul><li>Form hỗ trợ: ATX, mATX, ITX</li><li>Chiều dài VGA tối đa: 400 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-GIGABYTE-C301-GLASS', N'Vỏ case Gigabyte C301 Glass', N'CASE', N'GIGABYTE', 1890000, 1690000, 18, N'/images/products/case-deepcool-ch510.jpg', N'Form hỗ trợ: ATX, mATX, ITX, Chiều dài VGA tối đa: 400 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case Gigabyte C301 Glass.</p><ul><li>Form hỗ trợ: ATX, mATX, ITX</li><li>Chiều dài VGA tối đa: 400 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-GIGABYTE-AORUS-C300-GLASS', N'Vỏ case Gigabyte AORUS C300 Glass', N'CASE', N'GIGABYTE', 2190000, 2190000, 0, N'/images/products/case-corsair-3000d-airflow.jpg', N'Form hỗ trợ: ATX, mATX, ITX, Chiều dài VGA tối đa: 400 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case Gigabyte AORUS C300 Glass.</p><ul><li>Form hỗ trợ: ATX, mATX, ITX</li><li>Chiều dài VGA tối đa: 400 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-NZXT-H3-FLOW', N'Vỏ case NZXT H3 Flow', N'CASE', N'NZXT', 2490000, 2490000, 12, N'/images/products/case-nzxt-h5-flow.jpg', N'Form hỗ trợ: ATX, mATX, ITX, Chiều dài VGA tối đa: 365 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case NZXT H3 Flow.</p><ul><li>Form hỗ trợ: ATX, mATX, ITX</li><li>Chiều dài VGA tối đa: 365 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-THERMALTAKE-THE-TOWER-100', N'Vỏ case Thermaltake The Tower 100', N'CASE', N'THERMALTAKE', 3290000, 2990000, 9, N'/images/products/case-cooler-master-nr200p.jpg', N'Form hỗ trợ: ITX, Chiều dài VGA tối đa: 330 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case Thermaltake The Tower 100.</p><ul><li>Form hỗ trợ: ITX</li><li>Chiều dài VGA tối đa: 330 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-FRACTAL-DESIGN-TERRA', N'Vỏ case Fractal Design Terra', N'CASE', N'FRACTAL', 4990000, 4990000, 5, N'/images/products/case-fractal-design-north.jpg', N'Form hỗ trợ: ITX, Chiều dài VGA tối đa: 322 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case Fractal Design Terra.</p><ul><li>Form hỗ trợ: ITX</li><li>Chiều dài VGA tối đa: 322 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'CASE-CORSAIR-6500X', N'Vỏ case Corsair 6500X', N'CASE', N'CORSAIR', 4290000, 3990000, 16, N'/images/products/case-lian-li-o11-dynamic-evo.jpg', N'Form hỗ trợ: ATX, mATX, ITX, Chiều dài VGA tối đa: 400 mm, Nắp hông: Kính cường lực', N'<p>Vỏ case Corsair 6500X.</p><ul><li>Form hỗ trợ: ATX, mATX, ITX</li><li>Chiều dài VGA tối đa: 400 mm</li><li>Nắp hông: Kính cường lực</li></ul>', 12),
    (N'PK-LOGITECH-G203-LIGHTSYNC', N'Chuột gaming Logitech G203 LIGHTSYNC', N'PHUKIEN', N'LOGITECH', 490000, 420000, 60, N'/images/products/pk-logitech-g304-lightspeed.jpg', N'Loại phụ kiện: Chuột, Cảm biến: 8.000 DPI, Kết nối: Có dây USB', N'<p>Chuột gaming Logitech G203 LIGHTSYNC.</p><ul><li>Loại phụ kiện: Chuột</li><li>Cảm biến: 8.000 DPI</li><li>Kết nối: Có dây USB</li></ul>', 24),
    (N'PK-ASUS-ROG-GLADIUS-III', N'Chuột gaming ASUS ROG Gladius III', N'PHUKIEN', N'ASUS', 1990000, 1790000, 12, N'/images/products/pk-logitech-g502-hero.jpg', N'Loại phụ kiện: Chuột, Cảm biến: 26.000 DPI, Kết nối: Có dây USB', N'<p>Chuột gaming ASUS ROG Gladius III.</p><ul><li>Loại phụ kiện: Chuột</li><li>Cảm biến: 26.000 DPI</li><li>Kết nối: Có dây USB</li></ul>', 24),
    (N'PK-MSI-VIGOR-GK30', N'Bộ bàn phím chuột MSI Vigor GK30', N'PHUKIEN', N'MSI', 990000, 890000, 25, N'/images/products/pk-dareu-ek87.jpg', N'Loại phụ kiện: Bàn phím, Kết nối: Có dây USB, Layout: Full-size', N'<p>Bộ bàn phím chuột MSI Vigor GK30.</p><ul><li>Loại phụ kiện: Bàn phím</li><li>Kết nối: Có dây USB</li><li>Layout: Full-size</li></ul>', 12),
    (N'PK-ASUS-ROG-STRIX-SCOPE-II', N'Bàn phím cơ ASUS ROG Strix Scope II', N'PHUKIEN', N'ASUS', 3490000, 3290000, 8, N'/images/products/pk-keychron-k8-pro.jpg', N'Loại phụ kiện: Bàn phím, Kết nối: Có dây USB-C, Layout: Full-size', N'<p>Bàn phím cơ ASUS ROG Strix Scope II.</p><ul><li>Loại phụ kiện: Bàn phím</li><li>Kết nối: Có dây USB-C</li><li>Layout: Full-size</li></ul>', 24),
    (N'PK-STEELSERIES-ARCTIS-NOVA-1', N'Tai nghe SteelSeries Arctis Nova 1', N'PHUKIEN', N'STEELSERIES', 1490000, 1290000, 2, N'/images/products/pk-hyperx-cloud-ii.jpg', N'Loại phụ kiện: Tai nghe, Kết nối: Có dây 3.5 mm / USB, Âm thanh: Stereo', N'<p>Tai nghe SteelSeries Arctis Nova 1.</p><ul><li>Loại phụ kiện: Tai nghe</li><li>Kết nối: Có dây 3.5 mm / USB</li><li>Âm thanh: Stereo</li></ul>', 24),
    (N'PK-RAZER-BARRACUDA-X', N'Tai nghe Razer Barracuda X', N'PHUKIEN', N'RAZER', 2490000, 2190000, 15, N'/images/products/pk-razer-kraken-v3-x.jpg', N'Loại phụ kiện: Tai nghe, Kết nối: Không dây 2.4GHz / Bluetooth, Âm thanh: Stereo', N'<p>Tai nghe Razer Barracuda X.</p><ul><li>Loại phụ kiện: Tai nghe</li><li>Kết nối: Không dây 2.4GHz / Bluetooth</li><li>Âm thanh: Stereo</li></ul>', 24),
    (N'PK-LG-ULTRAGEAR-24GS60F', N'Màn hình LG UltraGear 24GS60F', N'PHUKIEN', N'LG', 3390000, 3190000, 19, N'/images/products/pk-lg-24mr400-b.jpg', N'Loại phụ kiện: Màn hình, Kích thước: 24 inch, Tấm nền: IPS, Tần số quét: 180 Hz, Độ phân giải: 1920 x 1080', N'<p>Màn hình LG UltraGear 24GS60F.</p><ul><li>Loại phụ kiện: Màn hình</li><li>Kích thước: 24 inch</li><li>Tấm nền: IPS</li><li>Tần số quét: 180 Hz</li><li>Độ phân giải: 1920 x 1080</li></ul>', 36),
    (N'PK-SAMSUNG-ODYSSEY-G5-27', N'Màn hình Samsung Odyssey G5 27 inch 2K', N'PHUKIEN', N'SAMSUNG', 6990000, 6290000, 0, N'/images/products/pk-samsung-odyssey-g3-24-inch.jpg', N'Loại phụ kiện: Màn hình, Kích thước: 27 inch, Tấm nền: VA, Tần số quét: 165 Hz, Độ phân giải: 2560 x 1440', N'<p>Màn hình Samsung Odyssey G5 27 inch 2K.</p><ul><li>Loại phụ kiện: Màn hình</li><li>Kích thước: 27 inch</li><li>Tấm nền: VA</li><li>Tần số quét: 165 Hz</li><li>Độ phân giải: 2560 x 1440</li></ul>', 36),
    (N'PK-MSI-PRO-MP251', N'Màn hình MSI PRO MP251 25 inch', N'PHUKIEN', N'MSI', 2090000, 1890000, 27, N'/images/products/pk-msi-g274f.jpg', N'Loại phụ kiện: Màn hình, Kích thước: 25 inch, Tấm nền: IPS, Tần số quét: 100 Hz, Độ phân giải: 1920 x 1080', N'<p>Màn hình MSI PRO MP251 25 inch.</p><ul><li>Loại phụ kiện: Màn hình</li><li>Kích thước: 25 inch</li><li>Tấm nền: IPS</li><li>Tần số quét: 100 Hz</li><li>Độ phân giải: 1920 x 1080</li></ul>', 36),
    (N'PK-LOGITECH-C920E', N'Webcam Logitech C920e Full HD', N'PHUKIEN', N'LOGITECH', 1890000, 1690000, 20, N'/images/products/pk-logitech-g502-hero.jpg', N'Loại phụ kiện: Webcam, Độ phân giải: 1080p 30fps, Kết nối: USB-A', N'<p>Webcam Logitech C920e Full HD.</p><ul><li>Loại phụ kiện: Webcam</li><li>Độ phân giải: 1080p 30fps</li><li>Kết nối: USB-A</li></ul>', 24),
    (N'PK-STEELSERIES-QCK-HEAVY-LARGE', N'Lót chuột SteelSeries QcK Heavy Large', N'PHUKIEN', N'STEELSERIES', 790000, 690000, 40, N'/images/products/pk-steelseries-rival-3.jpg', N'Loại phụ kiện: Lót chuột, Kích thước: 450 x 400 mm, Chất liệu: Vải, đế cao su', N'<p>Lót chuột SteelSeries QcK Heavy Large.</p><ul><li>Loại phụ kiện: Lót chuột</li><li>Kích thước: 450 x 400 mm</li><li>Chất liệu: Vải, đế cao su</li></ul>', 6)
) AS v(Ma, Ten, DanhMuc, ThuongHieu, GiaGoc, GiaBan, TonKho, Anh, MoTaNgan, MoTa, BaoHanh)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhams s WHERE s.MaSanPham = v.Ma);
GO

/* 3. Thông số kỹ thuật theo quy ước cho sản phẩm mới */
INSERT INTO dbo.ThongSoKyThuats (UidSanPham, TenThongSo, GiaTri, Ordering)
SELECT sp.Uid, v.Ten, v.GiaTri, v.Ord
FROM (VALUES
    (N'CASE-ASUS-TUF-GAMING-GT301', N'Form hỗ trợ', N'ATX, mATX, ITX', 1),
    (N'CASE-ASUS-TUF-GAMING-GT301', N'Chiều dài VGA tối đa (mm)', N'320 mm', 2),
    (N'CASE-ASUS-TUF-GAMING-GT301', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-ASUS-PRIME-AP201', N'Form hỗ trợ', N'mATX, ITX', 1),
    (N'CASE-ASUS-PRIME-AP201', N'Chiều dài VGA tối đa (mm)', N'338 mm', 2),
    (N'CASE-ASUS-PRIME-AP201', N'Nắp hông', N'Lưới thép', 3),
    (N'CASE-MSI-MAG-FORGE-100R', N'Form hỗ trợ', N'ATX, mATX, ITX', 1),
    (N'CASE-MSI-MAG-FORGE-100R', N'Chiều dài VGA tối đa (mm)', N'330 mm', 2),
    (N'CASE-MSI-MAG-FORGE-100R', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-MSI-MPG-GUNGNIR-110R', N'Form hỗ trợ', N'ATX, mATX, ITX', 1),
    (N'CASE-MSI-MPG-GUNGNIR-110R', N'Chiều dài VGA tối đa (mm)', N'400 mm', 2),
    (N'CASE-MSI-MPG-GUNGNIR-110R', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-GIGABYTE-C301-GLASS', N'Form hỗ trợ', N'ATX, mATX, ITX', 1),
    (N'CASE-GIGABYTE-C301-GLASS', N'Chiều dài VGA tối đa (mm)', N'400 mm', 2),
    (N'CASE-GIGABYTE-C301-GLASS', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-GIGABYTE-AORUS-C300-GLASS', N'Form hỗ trợ', N'ATX, mATX, ITX', 1),
    (N'CASE-GIGABYTE-AORUS-C300-GLASS', N'Chiều dài VGA tối đa (mm)', N'400 mm', 2),
    (N'CASE-GIGABYTE-AORUS-C300-GLASS', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-NZXT-H3-FLOW', N'Form hỗ trợ', N'ATX, mATX, ITX', 1),
    (N'CASE-NZXT-H3-FLOW', N'Chiều dài VGA tối đa (mm)', N'365 mm', 2),
    (N'CASE-NZXT-H3-FLOW', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-THERMALTAKE-THE-TOWER-100', N'Form hỗ trợ', N'ITX', 1),
    (N'CASE-THERMALTAKE-THE-TOWER-100', N'Chiều dài VGA tối đa (mm)', N'330 mm', 2),
    (N'CASE-THERMALTAKE-THE-TOWER-100', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-FRACTAL-DESIGN-TERRA', N'Form hỗ trợ', N'ITX', 1),
    (N'CASE-FRACTAL-DESIGN-TERRA', N'Chiều dài VGA tối đa (mm)', N'322 mm', 2),
    (N'CASE-FRACTAL-DESIGN-TERRA', N'Nắp hông', N'Kính cường lực', 3),
    (N'CASE-CORSAIR-6500X', N'Form hỗ trợ', N'ATX, mATX, ITX', 1),
    (N'CASE-CORSAIR-6500X', N'Chiều dài VGA tối đa (mm)', N'400 mm', 2),
    (N'CASE-CORSAIR-6500X', N'Nắp hông', N'Kính cường lực', 3),
    (N'PK-LOGITECH-G203-LIGHTSYNC', N'Loại phụ kiện', N'Chuột', 1),
    (N'PK-LOGITECH-G203-LIGHTSYNC', N'Cảm biến', N'8.000 DPI', 2),
    (N'PK-LOGITECH-G203-LIGHTSYNC', N'Kết nối', N'Có dây USB', 3),
    (N'PK-ASUS-ROG-GLADIUS-III', N'Loại phụ kiện', N'Chuột', 1),
    (N'PK-ASUS-ROG-GLADIUS-III', N'Cảm biến', N'26.000 DPI', 2),
    (N'PK-ASUS-ROG-GLADIUS-III', N'Kết nối', N'Có dây USB', 3),
    (N'PK-MSI-VIGOR-GK30', N'Loại phụ kiện', N'Bàn phím', 1),
    (N'PK-MSI-VIGOR-GK30', N'Kết nối', N'Có dây USB', 2),
    (N'PK-MSI-VIGOR-GK30', N'Layout', N'Full-size', 3),
    (N'PK-ASUS-ROG-STRIX-SCOPE-II', N'Loại phụ kiện', N'Bàn phím', 1),
    (N'PK-ASUS-ROG-STRIX-SCOPE-II', N'Kết nối', N'Có dây USB-C', 2),
    (N'PK-ASUS-ROG-STRIX-SCOPE-II', N'Layout', N'Full-size', 3),
    (N'PK-STEELSERIES-ARCTIS-NOVA-1', N'Loại phụ kiện', N'Tai nghe', 1),
    (N'PK-STEELSERIES-ARCTIS-NOVA-1', N'Kết nối', N'Có dây 3.5 mm / USB', 2),
    (N'PK-STEELSERIES-ARCTIS-NOVA-1', N'Âm thanh', N'Stereo', 3),
    (N'PK-RAZER-BARRACUDA-X', N'Loại phụ kiện', N'Tai nghe', 1),
    (N'PK-RAZER-BARRACUDA-X', N'Kết nối', N'Không dây 2.4GHz / Bluetooth', 2),
    (N'PK-RAZER-BARRACUDA-X', N'Âm thanh', N'Stereo', 3),
    (N'PK-LG-ULTRAGEAR-24GS60F', N'Loại phụ kiện', N'Màn hình', 1),
    (N'PK-LG-ULTRAGEAR-24GS60F', N'Kích thước', N'24 inch', 2),
    (N'PK-LG-ULTRAGEAR-24GS60F', N'Tấm nền', N'IPS', 3),
    (N'PK-LG-ULTRAGEAR-24GS60F', N'Tần số quét', N'180 Hz', 4),
    (N'PK-LG-ULTRAGEAR-24GS60F', N'Độ phân giải', N'1920 x 1080', 5),
    (N'PK-SAMSUNG-ODYSSEY-G5-27', N'Loại phụ kiện', N'Màn hình', 1),
    (N'PK-SAMSUNG-ODYSSEY-G5-27', N'Kích thước', N'27 inch', 2),
    (N'PK-SAMSUNG-ODYSSEY-G5-27', N'Tấm nền', N'VA', 3),
    (N'PK-SAMSUNG-ODYSSEY-G5-27', N'Tần số quét', N'165 Hz', 4),
    (N'PK-SAMSUNG-ODYSSEY-G5-27', N'Độ phân giải', N'2560 x 1440', 5),
    (N'PK-MSI-PRO-MP251', N'Loại phụ kiện', N'Màn hình', 1),
    (N'PK-MSI-PRO-MP251', N'Kích thước', N'25 inch', 2),
    (N'PK-MSI-PRO-MP251', N'Tấm nền', N'IPS', 3),
    (N'PK-MSI-PRO-MP251', N'Tần số quét', N'100 Hz', 4),
    (N'PK-MSI-PRO-MP251', N'Độ phân giải', N'1920 x 1080', 5),
    (N'PK-LOGITECH-C920E', N'Loại phụ kiện', N'Webcam', 1),
    (N'PK-LOGITECH-C920E', N'Độ phân giải', N'1080p 30fps', 2),
    (N'PK-LOGITECH-C920E', N'Kết nối', N'USB-A', 3),
    (N'PK-STEELSERIES-QCK-HEAVY-LARGE', N'Loại phụ kiện', N'Lót chuột', 1),
    (N'PK-STEELSERIES-QCK-HEAVY-LARGE', N'Kích thước', N'450 x 400 mm', 2),
    (N'PK-STEELSERIES-QCK-HEAVY-LARGE', N'Chất liệu', N'Vải, đế cao su', 3)
) AS v(Ma, Ten, GiaTri, Ord)
JOIN dbo.SanPhams sp ON sp.MaSanPham = v.Ma
WHERE NOT EXISTS (SELECT 1 FROM dbo.ThongSoKyThuats t WHERE t.UidSanPham = sp.Uid AND t.TenThongSo = v.Ten);
GO

/* 4. Bổ sung tên thông số theo quy ước cho CASE / PHUKIEN đã có từ seed trước (giữ nguyên thông số cũ) */
-- 4a. Form hỗ trợ: đổi cách viết từ "Hỗ trợ mainboard" (Micro-ATX, Mini-ITX) sang ATX / mATX / ITX
INSERT INTO dbo.ThongSoKyThuats (UidSanPham, TenThongSo, GiaTri, Ordering)
SELECT sp.Uid, N'Form hỗ trợ',
       REPLACE(REPLACE(t.GiaTri, N'Micro-ATX', N'mATX'), N'Mini-ITX', N'ITX'), 20
FROM dbo.SanPhams sp
JOIN dbo.ThongSoKyThuats t ON t.UidSanPham = sp.Uid AND t.TenThongSo = N'Hỗ trợ mainboard'
WHERE sp.MaDanhMuc = N'CASE'
  AND NOT EXISTS (SELECT 1 FROM dbo.ThongSoKyThuats x WHERE x.UidSanPham = sp.Uid AND x.TenThongSo = N'Form hỗ trợ');
GO

-- 4b. Chiều dài VGA tối đa (mm): lấy từ "Chiều dài VGA tối đa" (dạng "381 mm")
INSERT INTO dbo.ThongSoKyThuats (UidSanPham, TenThongSo, GiaTri, Ordering)
SELECT sp.Uid, N'Chiều dài VGA tối đa (mm)', t.GiaTri, 21
FROM dbo.SanPhams sp
JOIN dbo.ThongSoKyThuats t ON t.UidSanPham = sp.Uid AND t.TenThongSo = N'Chiều dài VGA tối đa'
WHERE sp.MaDanhMuc = N'CASE'
  AND NOT EXISTS (SELECT 1 FROM dbo.ThongSoKyThuats x WHERE x.UidSanPham = sp.Uid AND x.TenThongSo = N'Chiều dài VGA tối đa (mm)');
GO

-- 4c. Hai sản phẩm gốc trong database.sql chưa đủ thông số
INSERT INTO dbo.ThongSoKyThuats (UidSanPham, TenThongSo, GiaTri, Ordering)
SELECT sp.Uid, v.Ten, v.GiaTri, v.Ord
FROM (VALUES
    (N'CASE-CM-Q300L',   N'Chiều dài VGA tối đa (mm)', N'360 mm', 21),
    (N'MOUSE-LOGI-G102', N'Loại phụ kiện',             N'Chuột',  20)
) AS v(Ma, Ten, GiaTri, Ord)
JOIN dbo.SanPhams sp ON sp.MaSanPham = v.Ma
WHERE NOT EXISTS (SELECT 1 FROM dbo.ThongSoKyThuats x WHERE x.UidSanPham = sp.Uid AND x.TenThongSo = v.Ten);
GO

/* 5. Ảnh đại diện vào bảng ảnh phụ (giống seed RAM/VGA) */
INSERT INTO dbo.HinhAnhSanPhams (UidSanPham, DuongDan, Ordering)
SELECT s.Uid, s.AnhDaiDien, 1
FROM dbo.SanPhams s
WHERE s.AnhDaiDien IS NOT NULL
  AND s.MaDanhMuc IN (N'CASE', N'PHUKIEN')
  AND NOT EXISTS (SELECT 1 FROM dbo.HinhAnhSanPhams h WHERE h.UidSanPham = s.Uid);
GO

/* 6. Kiểm tra số lượng */
SELECT dm.MaDanhMuc, COUNT(sp.Uid) AS TongSoSanPham,
       CASE WHEN COUNT(sp.Uid) >= 30 THEN N'ĐẠT (>= 30)' ELSE N'CHƯA ĐỦ (< 30)' END AS TrangThai
FROM dbo.DanhMucs dm
LEFT JOIN dbo.SanPhams sp ON sp.MaDanhMuc = dm.MaDanhMuc
WHERE dm.MaDanhMuc IN (N'CASE', N'PHUKIEN')
GROUP BY dm.MaDanhMuc;
GO
