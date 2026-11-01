namespace WebLinhKien.Models.Requests;

public class DatHangRequest
{
    public string TenKhachHang { get; set; } = string.Empty;
    public string Sdt { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public string MaHinhThucThanhToan { get; set; } = string.Empty;   // COD | CHUYEN_KHOAN
    public string? GhiChu { get; set; }
    public List<DatHangItemRequest> Items { get; set; } = new();
}

public class DatHangItemRequest
{
    public int UidSanPham { get; set; }
    public int SoLuong { get; set; }
}
