namespace WebLinhKien.Models.Entities;

public class TaiKhoan
{
    public int Uid { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string? HoTen { get; set; }
    public bool HoatDong { get; set; } = true;

    /// <summary>"Admin" hoặc "NhanVien" (xem AuthServiceExtensions).</summary>
    public string VaiTro { get; set; } = "NhanVien";

    /// <summary>true: bắt đổi mật khẩu ngay lần đăng nhập tới (tài khoản mới tạo hoặc vừa được đặt lại).</summary>
    public bool CanDoiMatKhau { get; set; }

    public DateTime NgayTao { get; set; }
}
