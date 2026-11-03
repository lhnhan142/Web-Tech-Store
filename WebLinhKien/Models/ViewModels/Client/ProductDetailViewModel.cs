using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels.Client;

/// <summary>
/// ViewModel đầy đủ phục vụ hiển thị Trang chi tiết sản phẩm (/SanPham/ChiTiet/{id}).
/// </summary>
public class ProductDetailViewModel
{
    public SanPham SanPham { get; set; } = null!;
    public DanhMuc? DanhMuc { get; set; }
    public ThuongHieu? ThuongHieu { get; set; }

    public IReadOnlyList<HinhAnhSanPham> HinhAnh { get; set; } = Array.Empty<HinhAnhSanPham>();
    public IReadOnlyList<ThongSoKyThuat> ThongSo { get; set; } = Array.Empty<ThongSoKyThuat>();
    public IReadOnlyList<ProductCardViewModel> SanPhamLienQuan { get; set; } = Array.Empty<ProductCardViewModel>();

    public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();

    public bool ConHang => SanPham != null && SanPham.TonKho > 0;
    public bool CoGiamGia => SanPham != null && SanPham.GiaGoc > SanPham.GiaBan && SanPham.GiaBan > 0;

    public int PhanTramGiam => CoGiamGia && SanPham.GiaGoc > 0
        ? (int)Math.Round((SanPham.GiaGoc - SanPham.GiaBan) / SanPham.GiaGoc * 100)
        : 0;

    public decimal SoTienTietKiem => CoGiamGia ? (SanPham.GiaGoc - SanPham.GiaBan) : 0;

    public string IconDanhMuc => CategoryIconHelper.GetIcon(SanPham?.MaDanhMuc);
}

