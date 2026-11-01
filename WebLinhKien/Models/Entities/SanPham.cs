namespace WebLinhKien.Models.Entities;

public class SanPham
{
    public int Uid { get; set; }
    public string MaSanPham { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string MaDanhMuc { get; set; } = string.Empty;
    public string? MaThuongHieu { get; set; }
    public decimal GiaGoc { get; set; }
    public decimal GiaBan { get; set; }
    public int TonKho { get; set; }
    public string? AnhDaiDien { get; set; }
    public string? MoTaNgan { get; set; }
    public string? MoTa { get; set; }          // HTML từ CKEditor
    public int BaoHanhThang { get; set; }
    public bool NoiBat { get; set; }
    public bool HienThi { get; set; } = true;
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}
