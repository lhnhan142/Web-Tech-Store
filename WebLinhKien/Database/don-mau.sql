/* =====================================================================
   WebLinhKien - don-mau.sql
   6 đơn hàng MẪU để thử trang quản lý đơn (đủ các tình trạng).
   Chạy SAU schema.sql và seed.sql. Chạy lại nhiều lần không bị trùng.
   Tất cả đơn mẫu có ghi chú bắt đầu bằng "Đơn mẫu" để dễ xóa (xem cuối file).
   ===================================================================== */

USE WebLinhKien;
GO
SET NOCOUNT ON;

DECLARE @Don TABLE (
    MaDon NVARCHAR(30), Ten NVARCHAR(100), Sdt NVARCHAR(15), DiaChi NVARCHAR(200),
    ThanhToan NVARCHAR(30), TinhTrang NVARCHAR(30), GioTruoc INT
);
INSERT INTO @Don VALUES
 (N'DH261030090001000', N'Nguyễn Văn An',  N'0912345678', N'12 Lê Lợi, Phường Bến Thành, TP. Hồ Chí Minh', N'COD',          N'MOI',            30),
 (N'DH261030101502000', N'Trần Thị Bình',  N'0987654321', N'45 Nguyễn Huệ, Phường Bến Nghé, TP. Hồ Chí Minh', N'CHUYEN_KHOAN', N'DANG_XU_LY',     28),
 (N'DH261031083003000', N'Lê Hoàng Cường', N'0935123456', N'7 Trần Phú, Phường 4, Quận 5, TP. Hồ Chí Minh',   N'COD',          N'DANG_GIAO_HANG', 20),
 (N'DH261031142004000', N'Phạm Minh Dũng', N'0971234567', N'99 Điện Biên Phủ, Quận Bình Thạnh, TP. Hồ Chí Minh', N'CHUYEN_KHOAN', N'DA_NHAN',      14),
 (N'DH261101070005000', N'Võ Thị Em',      N'0903456789', N'21 Hai Bà Trưng, Quận 3, TP. Hồ Chí Minh',       N'COD',          N'MOI',             3),
 (N'DH261101081506000', N'Đặng Quốc Phong',N'0868123456', N'5 Võ Văn Tần, Quận 3, TP. Hồ Chí Minh',          N'COD',          N'HOAN_TRA',        2);

INSERT INTO dbo.DonDatHangs
    (MaDon, TenKhachHang, Sdt, DiaChi, MaHinhThucThanhToan, TongTienHang, PhiGiaoHang, TongThanhToan, GhiChu, MaTinhTrang, NgayDat)
SELECT d.MaDon, d.Ten, d.Sdt, d.DiaChi, d.ThanhToan, 0, 0, 0,
       N'Đơn mẫu để thử giao diện', d.TinhTrang, DATEADD(HOUR, -d.GioTruoc, SYSDATETIME())
FROM @Don d
WHERE NOT EXISTS (SELECT 1 FROM dbo.DonDatHangs x WHERE x.MaDon = d.MaDon);

DECLARE @CT TABLE (MaDon NVARCHAR(30), MaSanPham NVARCHAR(50), SoLuong INT);
INSERT INTO @CT VALUES
 (N'DH261030090001000', N'RAM-KING-16G',       1),
 (N'DH261030090001000', N'MOUSE-LOGI-G102',    1),
 (N'DH261030101502000', N'SSD-SAM-980-500',    1),
 (N'DH261031083003000', N'CPU-I5-12400F',      1),
 (N'DH261031083003000', N'MB-ASUS-B660M-K',    1),
 (N'DH261031142004000', N'VGA-MSI-RTX3060',    1),
 (N'DH261101070005000', N'MOUSE-LOGI-G102',    1),
 (N'DH261101081506000', N'PSU-COR-CX650',      1);

-- Lưu tên và đơn giá TẠI THỜI ĐIỂM MUA (lấy giá hiện tại của sản phẩm)
INSERT INTO dbo.ChiTietDonHangs (UidDonHang, UidSanPham, TenSanPham, DonGia, SoLuong)
SELECT o.Uid, s.Uid, s.TenSanPham, s.GiaBan, c.SoLuong
FROM @CT c
JOIN dbo.DonDatHangs o ON o.MaDon = c.MaDon
JOIN dbo.SanPhams    s ON s.MaSanPham = c.MaSanPham
WHERE NOT EXISTS (SELECT 1 FROM dbo.ChiTietDonHangs t WHERE t.UidDonHang = o.Uid AND t.UidSanPham = s.Uid);

-- Tính tổng tiền: miễn phí giao hàng từ 500.000đ, dưới mức đó thu 30.000đ
UPDATE o
SET TongTienHang  = x.Tong,
    PhiGiaoHang   = CASE WHEN x.Tong >= 500000 THEN 0 ELSE 30000 END,
    TongThanhToan = x.Tong + CASE WHEN x.Tong >= 500000 THEN 0 ELSE 30000 END
FROM dbo.DonDatHangs o
JOIN (SELECT UidDonHang, SUM(ThanhTien) AS Tong FROM dbo.ChiTietDonHangs GROUP BY UidDonHang) x
     ON x.UidDonHang = o.Uid
WHERE o.MaDon IN (SELECT MaDon FROM @Don);

SELECT MaDon, TenKhachHang, MaTinhTrang, TongTienHang, PhiGiaoHang, TongThanhToan
FROM dbo.DonDatHangs WHERE GhiChu LIKE N'Đơn mẫu%' ORDER BY NgayDat DESC;
GO

/* ---------------------------------------------------------------------
   XÓA đơn mẫu khi không cần nữa (bỏ comment rồi chạy):

   DELETE FROM dbo.ChiTietDonHangs WHERE UidDonHang IN (SELECT Uid FROM dbo.DonDatHangs WHERE GhiChu LIKE N'Đơn mẫu%');
   DELETE FROM dbo.DonDatHangs WHERE GhiChu LIKE N'Đơn mẫu%';
   --------------------------------------------------------------------- */
