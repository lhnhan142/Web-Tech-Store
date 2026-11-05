/* =====================================================================
   WebLinhKien - schema.sql
   Tạo cấu trúc database cho website bán linh kiện máy tính (SQL Server)
   Quy ước: chỉ khóa chính, không khai báo FOREIGN KEY (xem phần cuối nếu cần)
   Tiền tệ: VND, kiểu DECIMAL(18,0)
   ===================================================================== */

IF DB_ID(N'WebLinhKien') IS NULL
    CREATE DATABASE WebLinhKien;
GO

USE WebLinhKien;
GO

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
   TÙY CHỌN: khóa ngoại
   Mặc định bỏ qua để giống README. Nếu giảng viên yêu cầu ràng buộc
   toàn vẹn, bỏ comment khối dưới (cần chạy sau khi đã tạo đủ bảng).
   ===================================================================== */
/*
ALTER TABLE dbo.SanPhams          ADD CONSTRAINT FK_SanPhams_DanhMuc     FOREIGN KEY (MaDanhMuc)    REFERENCES dbo.DanhMucs (MaDanhMuc);
ALTER TABLE dbo.SanPhams          ADD CONSTRAINT FK_SanPhams_ThuongHieu  FOREIGN KEY (MaThuongHieu) REFERENCES dbo.ThuongHieus (MaThuongHieu);
ALTER TABLE dbo.HinhAnhSanPhams   ADD CONSTRAINT FK_HinhAnh_SanPham      FOREIGN KEY (UidSanPham)   REFERENCES dbo.SanPhams (Uid) ON DELETE CASCADE;
ALTER TABLE dbo.ThongSoKyThuats   ADD CONSTRAINT FK_ThongSo_SanPham      FOREIGN KEY (UidSanPham)   REFERENCES dbo.SanPhams (Uid) ON DELETE CASCADE;
ALTER TABLE dbo.DonDatHangs       ADD CONSTRAINT FK_DonDatHangs_TinhTrang FOREIGN KEY (MaTinhTrang)  REFERENCES dbo.TinhTrangDonHangs (MaTinhTrang);
ALTER TABLE dbo.ChiTietDonHangs   ADD CONSTRAINT FK_ChiTiet_DonHang      FOREIGN KEY (UidDonHang)   REFERENCES dbo.DonDatHangs (Uid) ON DELETE CASCADE;
ALTER TABLE dbo.ChiTietDonHangs   ADD CONSTRAINT FK_ChiTiet_SanPham      FOREIGN KEY (UidSanPham)   REFERENCES dbo.SanPhams (Uid);
*/
GO
