using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels;

public class DonHangListViewModel
{
    public IReadOnlyList<DonDatHang> Items { get; set; } = Array.Empty<DonDatHang>();
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    public string? MaTinhTrang { get; set; }   // tab đang chọn (null = tất cả)
    public string? Q { get; set; }             // từ khóa tìm kiếm

    public IReadOnlyList<TinhTrangDonHang> TinhTrangs { get; set; } = Array.Empty<TinhTrangDonHang>();
    public Dictionary<string, int> DemTheoTinhTrang { get; set; } = new();

    public int TongTatCa => DemTheoTinhTrang.Values.Sum();

    public string TenTinhTrang(string ma) =>
        TinhTrangs.FirstOrDefault(t => t.MaTinhTrang == ma)?.TenTinhTrang ?? ma;
}

public class DonHangChiTietViewModel
{
    public DonDatHang Don { get; set; } = new();
    public IReadOnlyList<ChiTietDonHang> ChiTiet { get; set; } = Array.Empty<ChiTietDonHang>();
    public TinhTrangDonHang? TinhTrangHienTai { get; set; }
    public IReadOnlyList<TinhTrangDonHang> TinhTrangTiepTheo { get; set; } = Array.Empty<TinhTrangDonHang>();
}
