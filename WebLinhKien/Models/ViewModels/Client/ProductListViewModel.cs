using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels.Client;

/// <summary>
/// ViewModel toàn diện cho Trang danh sách sản phẩm, Tìm kiếm, Lọc theo Danh mục / Thương hiệu.
/// </summary>
public class ProductListViewModel
{
    public PagedResult<ProductCardViewModel> PagedProducts { get; set; } = new(Array.Empty<ProductCardViewModel>(), 1, 12, 0);
    public ProductFilterRequest Filter { get; set; } = new();

    public IReadOnlyList<DanhMuc> Categories { get; set; } = Array.Empty<DanhMuc>();
    public IReadOnlyList<ThuongHieu> Brands { get; set; } = Array.Empty<ThuongHieu>();

    public DanhMuc? CurrentCategory { get; set; }
    public ThuongHieu? CurrentBrand { get; set; }

    public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();

    public string PageTitle { get; set; } = "Danh sách linh kiện máy tính";
    public string? PageDescription { get; set; }

    /// <summary>Kiểm tra xem người dùng có đang áp dụng bất kỳ tiêu chí lọc nào không</summary>
    public bool HasActiveFilters =>
        !string.IsNullOrEmpty(Filter.Q) ||
        !string.IsNullOrEmpty(Filter.DanhMuc) ||
        !string.IsNullOrEmpty(Filter.ThuongHieu) ||
        Filter.MinPrice.HasValue ||
        Filter.MaxPrice.HasValue ||
        !string.IsNullOrEmpty(Filter.PriceRange) ||
        Filter.ConHang == true;
}

