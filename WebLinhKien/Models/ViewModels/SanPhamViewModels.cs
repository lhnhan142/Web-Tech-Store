using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels;

public class SanPhamListViewModel
{
    public IReadOnlyList<SanPham> Items { get; set; } = Array.Empty<SanPham>();
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    public string? Q { get; set; }
    public string? MaDanhMuc { get; set; }
    public bool ChiSapHet { get; set; }

    public int NguongSapHet { get; set; }
    public int SoSapHet { get; set; }          // số sản phẩm sắp hết / hết hàng (toàn cửa hàng)

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
