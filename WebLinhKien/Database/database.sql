/* =====================================================================
   WebLinhKien - database.sql  (GỘP: cấu trúc + nâng cấp + dữ liệu mẫu)
   Một file duy nhất cho website bán linh kiện máy tính (SQL Server).
   Chạy cả file một lần là xong, chạy lại nhiều lần không bị trùng hay mất dữ liệu.

   Thứ tự trong file:
     A. Tạo database và các bảng        (database mới)
     B. Nâng cấp bảng TaiKhoans         (database cũ chưa có cột VaiTro; database mới tự bỏ qua)
     C. Dữ liệu mẫu                     (tình trạng đơn, cài đặt, danh mục, thương hiệu, sản phẩm)
     D. Thông tin chuyển khoản mẫu      (sửa thành tài khoản ngân hàng thật)
     E. Đơn hàng mẫu                    (tùy chọn, đặt @NapDonMau = 0 để bỏ qua)
     F. Khóa ngoại                      (nối các bảng với nhau để có sơ đồ quan hệ)

   Tài khoản admin KHÔNG nằm ở đây vì cần hash mật khẩu bằng code C#:
   khai báo AdminSeed:TenDangNhap và AdminSeed:MatKhau trong User Secrets, web tự tạo khi chạy lần đầu.

   Quan hệ giữa các bảng được khai báo bằng khóa ngoại ở mục F (cuối file)
   Tiền tệ: VND, kiểu DECIMAL(18,0)
   ===================================================================== */

IF DB_ID(N'WebLinhKien') IS NULL
    CREATE DATABASE WebLinhKien;
GO

USE WebLinhKien;
GO
SET NOCOUNT ON;
GO

/* =====================================================================
   A. TẠO CÁC BẢNG (từ đây đến hết mục A)
   ===================================================================== */

/* ---------------------------------------------------------------------
   1. DanhMucs - loại linh kiện (CPU, Mainboard, RAM, VGA, SSD...)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.DanhMucs', N'U') IS NULL
CREATE TABLE dbo.DanhMucs (
    MaDanhMuc   NVARCHAR(50)  NOT NULL,
    TenDanhMuc  NVARCHAR(100) NOT NULL,
    Ordering    INT           NOT NULL CONSTRAINT DF_DanhMucs_Ordering DEFAULT 0,
    HienThi     BIT           NOT NULL CONSTRAINT DF_DanhMucs_HienThi  DEFAULT 1,
    CONSTRAINT PK_DanhMucs PRIMARY KEY (MaDanhMuc)
);
GO

/* ---------------------------------------------------------------------
   2. ThuongHieus - Intel, AMD, ASUS, Kingston...
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ThuongHieus', N'U') IS NULL
CREATE TABLE dbo.ThuongHieus (
    MaThuongHieu  NVARCHAR(50)  NOT NULL,
    TenThuongHieu NVARCHAR(100) NOT NULL,
    Logo          NVARCHAR(300) NULL,
    HienThi       BIT           NOT NULL CONSTRAINT DF_ThuongHieus_HienThi DEFAULT 1,
    CONSTRAINT PK_ThuongHieus PRIMARY KEY (MaThuongHieu)
);
GO

/* ---------------------------------------------------------------------
   3. SanPhams - thông tin sản phẩm
      MoTa: HTML soạn từ CKEditor
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.SanPhams', N'U') IS NULL
CREATE TABLE dbo.SanPhams (
    Uid            INT IDENTITY(1,1) NOT NULL,
    MaSanPham      NVARCHAR(50)   NOT NULL,
    TenSanPham     NVARCHAR(250)  NOT NULL,
    MaDanhMuc      NVARCHAR(50)   NOT NULL,
    MaThuongHieu   NVARCHAR(50)   NULL,
    GiaGoc         DECIMAL(18,0)  NOT NULL CONSTRAINT DF_SanPhams_GiaGoc DEFAULT 0,
    GiaBan         DECIMAL(18,0)  NOT NULL CONSTRAINT DF_SanPhams_GiaBan DEFAULT 0,
    TonKho         INT            NOT NULL CONSTRAINT DF_SanPhams_TonKho DEFAULT 0,
    AnhDaiDien     NVARCHAR(300)  NULL,
    MoTaNgan       NVARCHAR(500)  NULL,
    MoTa           NVARCHAR(MAX)  NULL,
    BaoHanhThang   INT            NOT NULL CONSTRAINT DF_SanPhams_BaoHanh DEFAULT 0,
    NoiBat         BIT            NOT NULL CONSTRAINT DF_SanPhams_NoiBat DEFAULT 0,
    HienThi        BIT            NOT NULL CONSTRAINT DF_SanPhams_HienThi DEFAULT 1,
    NgayTao        DATETIME2(0)   NOT NULL CONSTRAINT DF_SanPhams_NgayTao DEFAULT SYSDATETIME(),
    NgayCapNhat    DATETIME2(0)   NULL,
    CONSTRAINT PK_SanPhams PRIMARY KEY (Uid),
    CONSTRAINT UQ_SanPhams_MaSanPham UNIQUE (MaSanPham),
    CONSTRAINT CK_SanPhams_Gia    CHECK (GiaGoc >= 0 AND GiaBan >= 0),
    CONSTRAINT CK_SanPhams_TonKho CHECK (TonKho >= 0)
);
GO

/* ---------------------------------------------------------------------
   4. HinhAnhSanPhams - bộ ảnh của từng sản phẩm
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.HinhAnhSanPhams', N'U') IS NULL
CREATE TABLE dbo.HinhAnhSanPhams (
    Uid        INT IDENTITY(1,1) NOT NULL,
    UidSanPham INT           NOT NULL,
    DuongDan   NVARCHAR(300) NOT NULL,
    Ordering   INT           NOT NULL CONSTRAINT DF_HinhAnh_Ordering DEFAULT 0,
    CONSTRAINT PK_HinhAnhSanPhams PRIMARY KEY (Uid)
);
GO

/* ---------------------------------------------------------------------
   5. ThongSoKyThuats - thông số dạng key/value (Socket = LGA1700...)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ThongSoKyThuats', N'U') IS NULL
CREATE TABLE dbo.ThongSoKyThuats (
    Uid        INT IDENTITY(1,1) NOT NULL,
    UidSanPham INT            NOT NULL,
    TenThongSo NVARCHAR(100)  NOT NULL,
    GiaTri     NVARCHAR(300)  NOT NULL,
    Ordering   INT            NOT NULL CONSTRAINT DF_ThongSo_Ordering DEFAULT 0,
    CONSTRAINT PK_ThongSoKyThuats PRIMARY KEY (Uid)
);
GO

/* ---------------------------------------------------------------------
   6. TinhTrangDonHangs - danh mục tình trạng đơn
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TinhTrangDonHangs', N'U') IS NULL
CREATE TABLE dbo.TinhTrangDonHangs (
    MaTinhTrang  NVARCHAR(30)  NOT NULL,
    TenTinhTrang NVARCHAR(100) NOT NULL,
    Ordering     INT           NOT NULL CONSTRAINT DF_TinhTrang_Ordering DEFAULT 0,
    CONSTRAINT PK_TinhTrangDonHangs PRIMARY KEY (MaTinhTrang)
);
GO

/* ---------------------------------------------------------------------
   7. DonDatHangs - đơn đặt hàng
      MaDon: mã hiển thị cho khách (vd DH261101103015123)
      Tiền hàng, phí giao, tổng thanh toán đều do server tính
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.DonDatHangs', N'U') IS NULL
CREATE TABLE dbo.DonDatHangs (
    Uid                  INT IDENTITY(1,1) NOT NULL,
    MaDon                NVARCHAR(30)   NOT NULL,
    TenKhachHang         NVARCHAR(100)  NOT NULL,
    Sdt                  NVARCHAR(15)   NOT NULL,
    DiaChi               NVARCHAR(200)  NOT NULL,
    MaHinhThucThanhToan  NVARCHAR(30)   NOT NULL,
    TongTienHang         DECIMAL(18,0)  NOT NULL,
    PhiGiaoHang          DECIMAL(18,0)  NOT NULL CONSTRAINT DF_DonDatHangs_Phi DEFAULT 0,
    TongThanhToan        DECIMAL(18,0)  NOT NULL,
    GhiChu               NVARCHAR(300)  NULL,
    MaTinhTrang          NVARCHAR(30)   NOT NULL CONSTRAINT DF_DonDatHangs_TinhTrang DEFAULT N'MOI',
    NgayDat              DATETIME2(0)   NOT NULL CONSTRAINT DF_DonDatHangs_NgayDat DEFAULT SYSDATETIME(),
    NgayCapNhat          DATETIME2(0)   NULL,
    CONSTRAINT PK_DonDatHangs PRIMARY KEY (Uid),
    CONSTRAINT UQ_DonDatHangs_MaDon UNIQUE (MaDon),
    CONSTRAINT CK_DonDatHangs_ThanhToan CHECK (MaHinhThucThanhToan IN (N'COD', N'CHUYEN_KHOAN')),
    CONSTRAINT CK_DonDatHangs_Tien CHECK (TongTienHang >= 0 AND PhiGiaoHang >= 0 AND TongThanhToan >= 0)
);
GO

/* ---------------------------------------------------------------------
   8. ChiTietDonHangs - từng dòng sản phẩm trong đơn
      Lưu lại tên và đơn giá tại thời điểm mua để đơn cũ không đổi
      khi sản phẩm được sửa giá hoặc đổi tên sau này
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.ChiTietDonHangs', N'U') IS NULL
CREATE TABLE dbo.ChiTietDonHangs (
    Uid          INT IDENTITY(1,1) NOT NULL,
    UidDonHang   INT            NOT NULL,
    UidSanPham   INT            NOT NULL,
    TenSanPham   NVARCHAR(250)  NOT NULL,
    DonGia       DECIMAL(18,0)  NOT NULL,
    SoLuong      INT            NOT NULL,
    ThanhTien    AS (DonGia * SoLuong) PERSISTED,
    CONSTRAINT PK_ChiTietDonHangs PRIMARY KEY (Uid),
    CONSTRAINT CK_ChiTietDonHangs CHECK (DonGia >= 0 AND SoLuong > 0)
);
GO

/* ---------------------------------------------------------------------
   9. TaiKhoans - tài khoản quản trị (mật khẩu đã hash, không lưu thô)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TaiKhoans', N'U') IS NULL
CREATE TABLE dbo.TaiKhoans (
    Uid          INT IDENTITY(1,1) NOT NULL,
    TenDangNhap  NVARCHAR(50)  NOT NULL,
    MatKhauHash  NVARCHAR(300) NOT NULL,
    HoTen        NVARCHAR(100) NULL,
    HoatDong     BIT           NOT NULL CONSTRAINT DF_TaiKhoans_HoatDong DEFAULT 1,
    VaiTro       NVARCHAR(20)  NOT NULL CONSTRAINT DF_TaiKhoans_VaiTro DEFAULT N'NhanVien'
                               CONSTRAINT CK_TaiKhoans_VaiTro CHECK (VaiTro IN (N'Admin', N'NhanVien')),
    CanDoiMatKhau BIT          NOT NULL CONSTRAINT DF_TaiKhoans_CanDoiMatKhau DEFAULT 0,
    NgayTao      DATETIME2(0)  NOT NULL CONSTRAINT DF_TaiKhoans_NgayTao DEFAULT SYSDATETIME(),
    CONSTRAINT PK_TaiKhoans PRIMARY KEY (Uid),
    CONSTRAINT UQ_TaiKhoans_TenDangNhap UNIQUE (TenDangNhap)
);
GO

/* ---------------------------------------------------------------------
   10. CaiDats - cấu hình dạng key/value
       Ví dụ: PhiGiaoHang, NguongMienPhiGiaoHang, NguongSapHetHang
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.CaiDats', N'U') IS NULL
CREATE TABLE dbo.CaiDats (
    Uid     INT IDENTITY(1,1) NOT NULL,
    Khoa    NVARCHAR(100) NOT NULL,
    GiaTri  NVARCHAR(500) NOT NULL,
    MoTa    NVARCHAR(300) NULL,
    CONSTRAINT PK_CaiDats PRIMARY KEY (Uid),
    CONSTRAINT UQ_CaiDats_Khoa UNIQUE (Khoa)
);
GO

/* =====================================================================
   INDEX - phục vụ các truy vấn thường gặp (lọc, tìm kiếm, tra đơn)
   ===================================================================== */

-- Lọc sản phẩm theo danh mục / thương hiệu, chỉ lấy sản phẩm đang hiển thị
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPhams_DanhMuc' AND object_id = OBJECT_ID(N'dbo.SanPhams'))
    CREATE INDEX IX_SanPhams_DanhMuc ON dbo.SanPhams (MaDanhMuc, HienThi) INCLUDE (GiaBan, TonKho);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPhams_ThuongHieu' AND object_id = OBJECT_ID(N'dbo.SanPhams'))
    CREATE INDEX IX_SanPhams_ThuongHieu ON dbo.SanPhams (MaThuongHieu, HienThi);
-- Tìm theo tên, sắp xếp theo giá / mới nhất
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPhams_TenSanPham' AND object_id = OBJECT_ID(N'dbo.SanPhams'))
    CREATE INDEX IX_SanPhams_TenSanPham ON dbo.SanPhams (TenSanPham);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPhams_GiaBan' AND object_id = OBJECT_ID(N'dbo.SanPhams'))
    CREATE INDEX IX_SanPhams_GiaBan ON dbo.SanPhams (GiaBan);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPhams_NgayTao' AND object_id = OBJECT_ID(N'dbo.SanPhams'))
    CREATE INDEX IX_SanPhams_NgayTao ON dbo.SanPhams (NgayTao DESC);

-- Lấy ảnh và thông số của một sản phẩm
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_SanPham' AND object_id = OBJECT_ID(N'dbo.HinhAnhSanPhams'))
    CREATE INDEX IX_HinhAnh_SanPham ON dbo.HinhAnhSanPhams (UidSanPham, Ordering);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ThongSo_SanPham' AND object_id = OBJECT_ID(N'dbo.ThongSoKyThuats'))
    CREATE INDEX IX_ThongSo_SanPham ON dbo.ThongSoKyThuats (UidSanPham, Ordering);

-- Trang quản lý đơn: lọc theo tình trạng, đơn mới nhất lên đầu
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonDatHangs_TinhTrang' AND object_id = OBJECT_ID(N'dbo.DonDatHangs'))
    CREATE INDEX IX_DonDatHangs_TinhTrang ON dbo.DonDatHangs (MaTinhTrang, NgayDat DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonDatHangs_Sdt' AND object_id = OBJECT_ID(N'dbo.DonDatHangs'))
    CREATE INDEX IX_DonDatHangs_Sdt ON dbo.DonDatHangs (Sdt);

-- Lấy chi tiết của một đơn
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChiTiet_DonHang' AND object_id = OBJECT_ID(N'dbo.ChiTietDonHangs'))
    CREATE INDEX IX_ChiTiet_DonHang ON dbo.ChiTietDonHangs (UidDonHang);
GO

/* =====================================================================
   B. NÂNG CẤP bảng TaiKhoans cho database CŨ (thêm vai trò Admin / NhanVien)
   Database mới tạo ở mục A đã có sẵn 2 cột nên phần này tự bỏ qua.
   Với database cũ: mọi tài khoản đang có được đặt thành Admin
   (vì trước đây ai đăng nhập cũng là admin).
   ===================================================================== */
IF COL_LENGTH(N'dbo.TaiKhoans', N'VaiTro') IS NULL
BEGIN
    ALTER TABLE dbo.TaiKhoans ADD VaiTro NVARCHAR(20) NOT NULL
        CONSTRAINT DF_TaiKhoans_VaiTro DEFAULT N'NhanVien';
    EXEC (N'UPDATE dbo.TaiKhoans SET VaiTro = N''Admin''');
    EXEC (N'ALTER TABLE dbo.TaiKhoans ADD CONSTRAINT CK_TaiKhoans_VaiTro CHECK (VaiTro IN (N''Admin'', N''NhanVien''))');
END
GO

IF COL_LENGTH(N'dbo.TaiKhoans', N'CanDoiMatKhau') IS NULL
    ALTER TABLE dbo.TaiKhoans ADD CanDoiMatKhau BIT NOT NULL
        CONSTRAINT DF_TaiKhoans_CanDoiMatKhau DEFAULT 0;
GO

/* =====================================================================
   C. DỮ LIỆU MẪU
   Giá và tồn kho chỉ là số minh họa, sửa lại theo ý nhóm.
   ===================================================================== */

/* ---------------------------------------------------------------------
   C1. Tình trạng đơn hàng
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
   C2. Cài đặt (đọc bằng Khoa, ép kiểu ở code C#)
   --------------------------------------------------------------------- */
INSERT INTO dbo.CaiDats (Khoa, GiaTri, MoTa)
SELECT v.Khoa, v.GiaTri, v.MoTa
FROM (VALUES
    (N'PhiGiaoHang',           N'30000',       N'Phí giao hàng (VND)'),
    (N'NguongMienPhiGiaoHang', N'500000',      N'Đơn từ mức này trở lên được miễn phí giao hàng (VND)'),
    (N'NguongSapHetHang',      N'5',           N'Tồn kho từ mức này trở xuống thì cảnh báo sắp hết hàng')
) AS v(Khoa, GiaTri, MoTa)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CaiDats c WHERE c.Khoa = v.Khoa);
GO

-- Tên cửa hàng cố định là TECHZONE trong code, không còn chỉnh ở trang Cài đặt: xóa dòng cũ nếu database đã có
DELETE FROM dbo.CaiDats WHERE Khoa = N'TenCuaHang';
GO

/* ---------------------------------------------------------------------
   C3. Danh mục
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
   C4. Thương hiệu
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
   C5. Sản phẩm mẫu (13 sản phẩm, đủ 8 danh mục)
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
   C6. Thông số kỹ thuật (nối với sản phẩm bằng MaSanPham)
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
   C7. Hình ảnh: mỗi sản phẩm có sẵn 1 ảnh = ảnh đại diện
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

/* =====================================================================
   D. THÔNG TIN CHUYỂN KHOẢN (mã VietQR trên trang thanh toán)
   Sửa 4 dòng DECLARE bên dưới thành tài khoản ngân hàng THẬT trước khi chạy.
   Chỉ thêm khi chưa có: nếu đã chỉnh trong trang Cài đặt của admin thì chạy lại file
   này không ghi đè. Muốn đổi sau này, vào trang Cài đặt hoặc sửa trực tiếp bảng CaiDats.
   ===================================================================== */
DECLARE @MaNganHang  NVARCHAR(50)  = N'VCB';
DECLARE @TenNganHang NVARCHAR(100) = N'Vietcombank';
DECLARE @SoTaiKhoan  NVARCHAR(50)  = N'0123456789';
DECLARE @ChuTaiKhoan NVARCHAR(100) = N'CONG TY WEBLINHKIEN';

INSERT INTO dbo.CaiDats (Khoa, GiaTri, MoTa)
SELECT v.Khoa, v.GiaTri, v.MoTa
FROM (VALUES
    (N'QrMaNganHang', @MaNganHang,                                                         N'Mã ngân hàng dùng tạo mã VietQR (VCB, MB, OCB...)'),
    (N'NganHangTen',  CASE WHEN @TenNganHang = N'' THEN @MaNganHang ELSE @TenNganHang END, N'Tên ngân hàng hiển thị cho khách'),
    (N'SoTaiKhoan',   @SoTaiKhoan,                                                         N'Số tài khoản nhận chuyển khoản'),
    (N'ChuTaiKhoan',  @ChuTaiKhoan,                                                        N'Tên chủ tài khoản')
) AS v (Khoa, GiaTri, MoTa)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CaiDats c WHERE c.Khoa = v.Khoa);
GO

/* =====================================================================
   E. ĐƠN HÀNG MẪU (6 đơn, đủ các tình trạng) để thử trang quản lý đơn.
   Đặt @NapDonMau = 0 nếu KHÔNG muốn có đơn mẫu (ví dụ khi nộp bài hoặc chạy thật).
   Mọi đơn mẫu có ghi chú bắt đầu bằng "Đơn mẫu", xóa bằng lệnh ở cuối file.
   ===================================================================== */
DECLARE @NapDonMau BIT = 1;

IF @NapDonMau = 1
BEGIN
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
END
GO

/* =====================================================================
   F. KHÓA NGOẠI (FOREIGN KEY): nối các bảng để SSMS vẽ được sơ đồ quan hệ
   và database tự chặn dữ liệu sai (vd: sản phẩm trỏ tới danh mục không có).
   Đặt ở cuối để dữ liệu mẫu ở trên được nạp xong trước. Khóa nào đã có thì bỏ qua.

   Nếu một lệnh báo lỗi "conflicted with the FOREIGN KEY constraint" nghĩa là
   đang có dòng mồ côi (trỏ tới dòng không còn tồn tại): chạy các câu SELECT
   kiểm tra ở cuối file để tìm và sửa, rồi chạy lại.
   ===================================================================== */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SanPhams_DanhMuc')
    ALTER TABLE dbo.SanPhams ADD CONSTRAINT FK_SanPhams_DanhMuc
        FOREIGN KEY (MaDanhMuc) REFERENCES dbo.DanhMucs (MaDanhMuc);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SanPhams_ThuongHieu')
    ALTER TABLE dbo.SanPhams ADD CONSTRAINT FK_SanPhams_ThuongHieu
        FOREIGN KEY (MaThuongHieu) REFERENCES dbo.ThuongHieus (MaThuongHieu);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HinhAnh_SanPham')
    ALTER TABLE dbo.HinhAnhSanPhams ADD CONSTRAINT FK_HinhAnh_SanPham
        FOREIGN KEY (UidSanPham) REFERENCES dbo.SanPhams (Uid) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ThongSo_SanPham')
    ALTER TABLE dbo.ThongSoKyThuats ADD CONSTRAINT FK_ThongSo_SanPham
        FOREIGN KEY (UidSanPham) REFERENCES dbo.SanPhams (Uid) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DonDatHangs_TinhTrang')
    ALTER TABLE dbo.DonDatHangs ADD CONSTRAINT FK_DonDatHangs_TinhTrang
        FOREIGN KEY (MaTinhTrang) REFERENCES dbo.TinhTrangDonHangs (MaTinhTrang);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ChiTiet_DonHang')
    ALTER TABLE dbo.ChiTietDonHangs ADD CONSTRAINT FK_ChiTiet_DonHang
        FOREIGN KEY (UidDonHang) REFERENCES dbo.DonDatHangs (Uid) ON DELETE CASCADE;

-- Không CASCADE: sản phẩm đã có trong đơn thì không được xóa (web cũng đã chặn việc này)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ChiTiet_SanPham')
    ALTER TABLE dbo.ChiTietDonHangs ADD CONSTRAINT FK_ChiTiet_SanPham
        FOREIGN KEY (UidSanPham) REFERENCES dbo.SanPhams (Uid);
GO

-- Danh sách khóa ngoại đã có (phải thấy 7 dòng)
SELECT fk.name AS KhoaNgoai,
       OBJECT_NAME(fk.parent_object_id)     AS BangCon,
       OBJECT_NAME(fk.referenced_object_id) AS BangCha
FROM sys.foreign_keys fk
ORDER BY BangCon, KhoaNgoai;
GO

UPDATE dbo.TaiKhoans
SET MatKhauHash = N'AQAAAAEAAYagAAAAEIhcjDMO+PmqptXF1GxFQX6y4BRz0AD9AsIr6pyNLUhGlOc2oRJK5GSnuyuyQw5AxA==',
    VaiTro = N'Admin', HoatDong = 1, CanDoiMatKhau = 0
WHERE TenDangNhap = N'admin';

/* ---------------------------------------------------------------------
   KIỂM TRA dòng mồ côi (chỉ cần khi mục F báo lỗi). Mỗi câu phải trả về 0 dòng.

   SELECT * FROM dbo.SanPhams s WHERE NOT EXISTS (SELECT 1 FROM dbo.DanhMucs d WHERE d.MaDanhMuc = s.MaDanhMuc);
   SELECT * FROM dbo.SanPhams s WHERE s.MaThuongHieu IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ThuongHieus t WHERE t.MaThuongHieu = s.MaThuongHieu);
   SELECT * FROM dbo.HinhAnhSanPhams h WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhams s WHERE s.Uid = h.UidSanPham);
   SELECT * FROM dbo.ThongSoKyThuats t WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhams s WHERE s.Uid = t.UidSanPham);
   SELECT * FROM dbo.DonDatHangs o WHERE NOT EXISTS (SELECT 1 FROM dbo.TinhTrangDonHangs t WHERE t.MaTinhTrang = o.MaTinhTrang);
   SELECT * FROM dbo.ChiTietDonHangs c WHERE NOT EXISTS (SELECT 1 FROM dbo.DonDatHangs o WHERE o.Uid = c.UidDonHang);
   SELECT * FROM dbo.ChiTietDonHangs c WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhams s WHERE s.Uid = c.UidSanPham);
   --------------------------------------------------------------------- */

/* ---------------------------------------------------------------------
   XÓA đơn mẫu khi không cần nữa (bỏ comment rồi chạy):

   DELETE FROM dbo.ChiTietDonHangs WHERE UidDonHang IN (SELECT Uid FROM dbo.DonDatHangs WHERE GhiChu LIKE N'Đơn mẫu%');
   DELETE FROM dbo.DonDatHangs WHERE GhiChu LIKE N'Đơn mẫu%';
   --------------------------------------------------------------------- */
