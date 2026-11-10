using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels.Client;

/// <summary>
/// Đại diện cho một thương hiệu thuộc danh mục kèm số lượng sản phẩm
/// </summary>
public class ThuongHieuFilterItem
{
    public string MaThuongHieu { get; set; } = string.Empty;
    public string TenThuongHieu { get; set; } = string.Empty;
    public int SoLuongSanPham { get; set; }
}

/// <summary>
/// ViewModel toàn diện cho Trang danh sách sản phẩm, Tìm kiếm, Lọc theo Danh mục / Thương hiệu.
/// </summary>
public class ProductListViewModel
{
    public PagedResult<ProductCardViewModel> PagedProducts { get; set; } = new(Array.Empty<ProductCardViewModel>(), 1, 12, 0);
    public ProductFilterRequest Filter { get; set; } = new();

    public IReadOnlyList<DanhMuc> Categories { get; set; } = Array.Empty<DanhMuc>();
    public IReadOnlyList<ThuongHieu> Brands { get; set; } = Array.Empty<ThuongHieu>();
    public IReadOnlyList<ThuongHieuFilterItem> CategoryBrands { get; set; } = Array.Empty<ThuongHieuFilterItem>();

    public DanhMuc? CurrentCategory { get; set; }
    public ThuongHieu? CurrentBrand { get; set; }

    public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();

    public string PageTitle { get; set; } = "Danh sách linh kiện máy tính";
    public string? PageDescription { get; set; }
    public string? PriceErrorMessage { get; set; }

    /// <summary>Kiểm tra xem người dùng có đang áp dụng bất kỳ tiêu chí lọc nào không</summary>
    public bool HasActiveFilters =>
        !string.IsNullOrEmpty(Filter.Q) ||
        !string.IsNullOrEmpty(Filter.DanhMuc) ||
        !string.IsNullOrEmpty(Filter.ThuongHieu) ||
        Filter.MinPrice.HasValue ||
        Filter.MaxPrice.HasValue ||
        !string.IsNullOrEmpty(Filter.PriceRange) ||
        Filter.ConHang == true;

    /// <summary>Chuyển đổi dữ liệu sang FilterPanelViewModel để dùng chung cho Partial _FilterPanel</summary>
    public FilterPanelViewModel ToFilterPanelViewModel() => new()
    {
        Mode = "client",
        ActionUrl = "/SanPham",
        ResetUrl = "/SanPham",
        IdPrefix = "client",
        Q = Filter.Q,
        SelectedDanhMuc = Filter.DanhMuc,
        SelectedThuongHieu = Filter.ThuongHieu,
        SelectedPriceRange = Filter.PriceRange,
        MinPrice = Filter.MinPrice,
        MaxPrice = Filter.MaxPrice,
        ConHang = Filter.ConHang,
        Sort = Filter.Sort,
        PageSize = Filter.PageSize,
        PriceErrorMessage = PriceErrorMessage,
        Categories = Categories,
        CategoryBrands = CategoryBrands
    };
}

