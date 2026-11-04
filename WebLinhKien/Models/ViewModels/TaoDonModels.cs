namespace WebLinhKien.Models.ViewModels;

/// <summary>Dữ liệu một đơn hàng mới (đã tính tiền ở server) để Repository lưu xuống database.</summary>
public class DonHangMoi
{
    public string MaDon { get; set; } = string.Empty;
    public string TenKhachHang { get; set; } = string.Empty;
    public string Sdt { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public string MaHinhThucThanhToan { get; set; } = string.Empty;
    public decimal TongTienHang { get; set; }
    public decimal PhiGiaoHang { get; set; }
    public decimal TongThanhToan { get; set; }
    public string? GhiChu { get; set; }
}

/// <summary>Một dòng sản phẩm của đơn: lưu lại tên và đơn giá tại thời điểm mua.</summary>
public class DongHangMoi
{
    public int UidSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    public int SoLuong { get; set; }
}

public class TaoDonKetQua
{
    public bool Ok { get; init; }
    /// <summary>Tên sản phẩm không đủ hàng khi trừ kho (chỉ có khi Ok = false).</summary>
    public string? SanPhamThieu { get; init; }
    /// <summary>Tồn kho của từng sản phẩm SAU khi trừ, dùng để cảnh báo sắp hết hàng.</summary>
    public Dictionary<int, int> TonKhoSau { get; init; } = new();

    public static TaoDonKetQua Thanh(Dictionary<int, int> tonKhoSau) => new() { Ok = true, TonKhoSau = tonKhoSau };
    public static TaoDonKetQua ThieuHang(string ten) => new() { Ok = false, SanPhamThieu = ten };
}
