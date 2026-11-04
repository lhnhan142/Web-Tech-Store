USE WebLinhKien;
GO

DECLARE @MaNganHang  NVARCHAR(50)  = N'VCB';
DECLARE @TenNganHang NVARCHAR(100) = N'Vietcombank';
DECLARE @SoTaiKhoan  NVARCHAR(50)  = N'0123456789';
DECLARE @ChuTaiKhoan NVARCHAR(100) = N'CONG TY WEBLINHKIEN';

IF @MaNganHang = N'' OR @SoTaiKhoan = N'' OR @ChuTaiKhoan = N''
    THROW 50000, N'Hãy điền mã ngân hàng, số tài khoản và chủ tài khoản thật ở đầu file rồi chạy lại.', 1;

MERGE dbo.CaiDats AS t
USING (VALUES
    (N'QrMaNganHang', @MaNganHang,                                   N'Mã ngân hàng dùng tạo mã VietQR (VCB, MB, OCB...)'),
    (N'NganHangTen',  CASE WHEN @TenNganHang = N'' THEN @MaNganHang ELSE @TenNganHang END, N'Tên ngân hàng hiển thị cho khách'),
    (N'SoTaiKhoan',   @SoTaiKhoan,                                   N'Số tài khoản nhận chuyển khoản'),
    (N'ChuTaiKhoan',  @ChuTaiKhoan,                                  N'Tên chủ tài khoản')
) AS v (Khoa, GiaTri, MoTa)
ON t.Khoa = v.Khoa
WHEN MATCHED THEN UPDATE SET GiaTri = v.GiaTri
WHEN NOT MATCHED THEN INSERT (Khoa, GiaTri, MoTa) VALUES (v.Khoa, v.GiaTri, v.MoTa);
GO

-- Kiểm tra: phải thấy đúng 4 dòng
SELECT Khoa, GiaTri FROM dbo.CaiDats
WHERE Khoa IN (N'QrMaNganHang', N'NganHangTen', N'SoTaiKhoan', N'ChuTaiKhoan');
GO