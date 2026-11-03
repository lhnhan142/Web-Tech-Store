using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels;

/// <summary>Dữ liệu cho trang Tổng quan của khu quản trị.</summary>
public class DashboardViewModel
{
    public IReadOnlyList<DonDatHang> DonMoiNhat { get; set; } = Array.Empty<DonDatHang>();
    public IReadOnlyList<TinhTrangDonHang> TinhTrangs { get; set; } = Array.Empty<TinhTrangDonHang>();
    public Dictionary<string, int> DemTheoTinhTrang { get; set; } = new();
}
