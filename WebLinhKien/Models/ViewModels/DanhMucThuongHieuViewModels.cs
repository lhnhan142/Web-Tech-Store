using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels;

public class DanhMucListViewModel
{
    public IReadOnlyList<DanhMuc> Items { get; set; } = Array.Empty<DanhMuc>();
    public Dictionary<string, int> DemSanPham { get; set; } = new();
}

public class ThuongHieuListViewModel
{
    public IReadOnlyList<ThuongHieu> Items { get; set; } = Array.Empty<ThuongHieu>();
    public Dictionary<string, int> DemSanPham { get; set; } = new();
}
