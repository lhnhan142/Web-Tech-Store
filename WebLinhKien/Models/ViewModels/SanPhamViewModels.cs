using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels.Client;

namespace WebLinhKien.Models.ViewModels;

public class SanPhamListViewModel
{
    public IReadOnlyList<SanPham> Items { get; set; } = Array.Empty<SanPham>();
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    public string? Q { get; set; }
    public string? MaDanhMuc { get; set; }
    public string? MaThuongHieu { get; set; }
    public string? Kho { get; set; }           // null | "sap-het" | "het-hang"

    public int NguongSapHet { get; set; }
    public int SoSapHet { get; set; }          // còn 1..ngưỡng, đếm theo cùng bộ lọc với danh sách
    public int SoHetHang { get; set; }         // tồn kho = 0, đếm theo cùng bộ lọc với danh sách

    public IReadOnlyList<ThuongHieuFilterItem> HangTheoLoai { get; set; } = Array.Empty<ThuongHieuFilterItem>();
    public List<string>? Ts { get; set; } = new List<string>();
    public IReadOnlyDictionary<string, int>? SpecFacets { get; set; } = new Dictionary<string, int>();

    /// <summary>Tham số hiện tại để giữ nguyên bộ lọc khi chuyển trang hoặc đổi trạng thái kho.</summary>
    public Dictionary<string, string?> RouteValues(string? kho, int? page = null)
    {
        var d = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(Q)) d["q"] = Q;
        if (!string.IsNullOrEmpty(MaDanhMuc)) d["danhMuc"] = MaDanhMuc;
        if (!string.IsNullOrEmpty(MaThuongHieu)) d["thuongHieu"] = MaThuongHieu;
        if (!string.IsNullOrEmpty(kho)) d["kho"] = kho;
        if (page is > 1) d["page"] = page.Value.ToString();
        return d;
    }

    public FilterPanelViewModel ToFilterPanelViewModel() => new()
    {
        Mode = "admin",
        ActionUrl = "/Admin/SanPham",
        ResetUrl = "/Admin/SanPham",
        IdPrefix = "admin",
        Q = Q,
        SelectedDanhMuc = MaDanhMuc,
        SelectedThuongHieu = MaThuongHieu,
        Categories = DanhMucs,
        CategoryBrands = HangTheoLoai,
        ShowPriceFilter = false,
        ShowStockFilter = true,
        Kho = Kho,
        NguongSapHet = NguongSapHet,
        SoSapHet = SoSapHet,
        SoHetHang = SoHetHang
    };

    public IReadOnlyList<DanhMuc> DanhMucs { get; set; } = Array.Empty<DanhMuc>();
    public IReadOnlyList<ThuongHieu> ThuongHieus { get; set; } = Array.Empty<ThuongHieu>();

    public string TenDanhMuc(string ma) => DanhMucs.FirstOrDefault(d => d.MaDanhMuc == ma)?.TenDanhMuc ?? ma;
    public string TenThuongHieu(string? ma) =>
        string.IsNullOrEmpty(ma) ? "—" : ThuongHieus.FirstOrDefault(t => t.MaThuongHieu == ma)?.TenThuongHieu ?? ma;
}

/// <summary>Dữ liệu phụ cho form thêm/sửa (truyền qua ViewData để form chỉ cần bind SanPhamForm).</summary>
public class SanPhamFormLookups
{
    public IReadOnlyList<DanhMuc> DanhMucs { get; set; } = Array.Empty<DanhMuc>();
    public IReadOnlyList<ThuongHieu> ThuongHieus { get; set; } = Array.Empty<ThuongHieu>();
    public IReadOnlyList<HinhAnhSanPham> HinhAnhs { get; set; } = Array.Empty<HinhAnhSanPham>();
    public string? AnhDaiDien { get; set; }
    public int NguongSapHet { get; set; }
}
