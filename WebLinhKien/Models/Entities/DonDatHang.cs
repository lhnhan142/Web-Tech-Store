namespace WebLinhKien.Models.Entities;

public class DonDatHang
{
    public int Uid { get; set; }
    public string MaDon { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string Sdt { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public string MaHinhThucThanhToan { get; set; } = string.Empty;   // COD | CHUYEN_KHOAN
    public decimal TongTienHang { get; set; }
    public decimal PhiGiaoHang { get; set; }
    public decimal TongThanhToan { get; set; }
    public string? GhiChu { get; set; }
    public string MaTinhTrang { get; set; } = "MOI";
    public DateTime NgayDat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}
