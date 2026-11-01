namespace WebLinhKien.Models.Entities;

public class TaiKhoan
{
    public int Uid { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string? HoTen { get; set; }
    public bool HoatDong { get; set; } = true;
    public DateTime NgayTao { get; set; }
}
