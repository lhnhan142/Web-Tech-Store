using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels.Client;

/// <summary>
/// Helper lấy icon FontAwesome chuẩn cho từng loại danh mục linh kiện máy tính.
/// </summary>
public static class CategoryIconHelper
{
    public static string GetIcon(string? maDanhMuc) => (maDanhMuc?.ToUpperInvariant()) switch
    {
        "CPU" => "fa-microchip",
        "MAINBOARD" => "fa-server",
        "RAM" => "fa-memory",
        "VGA" => "fa-display",
        "SSD" => "fa-hard-drive",
        "PSU" => "fa-bolt",
        "CASE" => "fa-desktop",
        "PHUKIEN" => "fa-computer-mouse",
        "SCREEN" or "MONITOR" => "fa-tv",
        _ => "fa-box-archive"
    };

    public static string GetCategoryName(string? maDanhMuc) => (maDanhMuc?.ToUpperInvariant()) switch
    {
        "CPU" => "CPU - Bộ vi xử lý",
        "MAINBOARD" => "Bo mạch chủ",
        "RAM" => "Bộ nhớ trong",
        "VGA" => "Card màn hình",
        "SSD" => "Ổ cứng SSD",
        "PSU" => "Nguồn máy tính",
        "CASE" => "Vỏ Case PC",
        "PHUKIEN" => "Phụ kiện Gaming",
        _ => maDanhMuc ?? "Linh kiện"
    };
}

/// <summary>
/// ViewModel phục vụ component hiển thị Card sản phẩm dùng chung (_ProductCard.cshtml).
/// </summary>
public class ProductCardViewModel
{
    public int Uid { get; set; }
    public string MaSanPham { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string MaDanhMuc { get; set; } = string.Empty;
    public string? MaThuongHieu { get; set; }
    public decimal GiaGoc { get; set; }
    public decimal GiaBan { get; set; }
    public int TonKho { get; set; }
    public string? AnhDaiDien { get; set; }
    public string? MoTaNgan { get; set; }
    public int BaoHanhThang { get; set; }
    public bool NoiBat { get; set; }

    public bool ConHang => TonKho > 0;
    public bool CoGiamGia => GiaGoc > GiaBan && GiaBan > 0;

    public int PhanTramGiam => CoGiamGia && GiaGoc > 0
        ? (int)Math.Round((GiaGoc - GiaBan) / GiaGoc * 100)
        : 0;

    public string IconDanhMuc => CategoryIconHelper.GetIcon(MaDanhMuc);

    public ProductCardViewModel() { }

    public ProductCardViewModel(SanPham sp)
    {
        Uid = sp.Uid;
        MaSanPham = sp.MaSanPham;
        TenSanPham = sp.TenSanPham;
        MaDanhMuc = sp.MaDanhMuc;
        MaThuongHieu = sp.MaThuongHieu;
        GiaGoc = sp.GiaGoc;
        GiaBan = sp.GiaBan;
        TonKho = sp.TonKho;
        AnhDaiDien = sp.AnhDaiDien;
        MoTaNgan = sp.MoTaNgan;
        BaoHanhThang = sp.BaoHanhThang;
        NoiBat = sp.NoiBat;
    }

    public static implicit operator ProductCardViewModel(SanPham sp) => new(sp);
}

/// <summary>
/// ViewModel hiển thị Card danh mục (_CategoryCard.cshtml).
/// </summary>
public class CategoryCardViewModel
{
    public string MaDanhMuc { get; set; } = string.Empty;
    public string TenDanhMuc { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-box";
    public int SoLuongSanPham { get; set; }

    public CategoryCardViewModel() { }

    public CategoryCardViewModel(DanhMuc dm, int soLuong = 0)
    {
        MaDanhMuc = dm.MaDanhMuc;
        TenDanhMuc = dm.TenDanhMuc;
        Icon = CategoryIconHelper.GetIcon(dm.MaDanhMuc);
        SoLuongSanPham = soLuong;
    }
}

/// <summary>
/// ViewModel hiển thị Card thương hiệu (_BrandCard.cshtml).
/// </summary>
public class BrandCardViewModel
{
    public string MaThuongHieu { get; set; } = string.Empty;
    public string TenThuongHieu { get; set; } = string.Empty;
    public string? Logo { get; set; }

    public BrandCardViewModel() { }

    public BrandCardViewModel(ThuongHieu th)
    {
        MaThuongHieu = th.MaThuongHieu;
        TenThuongHieu = th.TenThuongHieu;
        Logo = th.Logo;
    }
}

/// <summary>
/// Mục điều hướng breadcrumb (_Breadcrumb.cshtml).
/// </summary>
public class BreadcrumbItem
{
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool IsActive { get; set; }

    public BreadcrumbItem() { }
    public BreadcrumbItem(string title, string? url = null, bool isActive = false)
    {
        Title = title;
        Url = url;
        IsActive = isActive;
    }
}

/// <summary>
/// ViewModel phân trang linh hoạt (_Pagination.cshtml).
/// </summary>
public class PaginationViewModel
{
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; } = 0;
    public int PageSize { get; set; } = 12;
    public string BaseUrl { get; set; } = string.Empty;
    public IDictionary<string, string?> RouteValues { get; set; } = new Dictionary<string, string?>();

    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// ViewModel hiển thị trạng thái rỗng (_EmptyState.cshtml).
/// </summary>
public class EmptyStateViewModel
{
    public string Icon { get; set; } = "fa-box-open";
    public string Title { get; set; } = "Không tìm thấy dữ liệu";
    public string Description { get; set; } = "Hiện chưa có sản phẩm hoặc kết quả nào phù hợp với yêu cầu của bạn.";
    public string? ActionText { get; set; } = "Tiếp tục mua sắm";
    public string ActionUrl { get; set; } = "/";
}

/// <summary>
/// ViewModel dùng chung cho Partial View bộ lọc linh kiện (_FilterPanel.cshtml)
/// Dùng được cho cả trang khách hàng và trang quản trị Admin.
/// </summary>
public class FilterPanelViewModel
{
    /// <summary>Chế độ hiển thị: "client" (mặc định) hoặc "admin"</summary>
    public string Mode { get; set; } = "client";

    /// <summary>URL submit form</summary>
    public string ActionUrl { get; set; } = "/SanPham";

    /// <summary>URL reset bộ lọc</summary>
    public string ResetUrl { get; set; } = "/SanPham";

    /// <summary>Tiền tố phân biệt ID các phần tử HTML để tránh trùng lặp khi nhúng nhiều nơi</summary>
    public string IdPrefix { get; set; } = "client";

    /// <summary>Từ khóa tìm kiếm (q)</summary>
    public string? Q { get; set; }

    /// <summary>Mã danh mục đang chọn</summary>
    public string? SelectedDanhMuc { get; set; }

    /// <summary>Mã thương hiệu đang chọn</summary>
    public string? SelectedThuongHieu { get; set; }

    /// <summary>Khoảng giá nhanh đang chọn</summary>
    public string? SelectedPriceRange { get; set; }

    /// <summary>Giá tối thiểu nhập tay</summary>
    public decimal? MinPrice { get; set; }

    /// <summary>Giá tối đa nhập tay</summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>Chỉ lọc còn hàng</summary>
    public bool? ConHang { get; set; }

    /// <summary>Tiêu chí sắp xếp hiện tại</summary>
    public string? Sort { get; set; } = "default";

    /// <summary>Kích thước trang</summary>
    public int PageSize { get; set; } = 12;

    /// <summary>Thông báo lỗi khoảng giá (nếu có)</summary>
    public string? PriceErrorMessage { get; set; }

    /// <summary>Danh sách danh mục linh kiện</summary>
    public IReadOnlyList<DanhMuc> Categories { get; set; } = Array.Empty<DanhMuc>();

    /// <summary>Danh sách thương hiệu theo danh mục</summary>
    public IReadOnlyList<ThuongHieuFilterItem> CategoryBrands { get; set; } = Array.Empty<ThuongHieuFilterItem>();

    /// <summary>Có hiển thị bộ lọc giá hay không (Admin có thể tắt nếu không dùng)</summary>
    public bool ShowPriceFilter { get; set; } = true;

    /// <summary>Có hiển thị bộ lọc tình trạng kho hay không</summary>
    public bool ShowStockFilter { get; set; } = true;

    // ---- Chỉ dùng khi Mode = "admin" ----
    /// <summary>Trạng thái kho đang lọc: null | "sap-het" | "het-hang"</summary>
    public string? Kho { get; set; }
    public int NguongSapHet { get; set; }
    public int SoSapHet { get; set; }
    public int SoHetHang { get; set; }

    /// <summary>Kiểm tra xem người dùng có đang áp dụng bất kỳ tiêu chí lọc nào không</summary>
    public bool HasActiveFilters =>
        !string.IsNullOrEmpty(Kho) ||
        !string.IsNullOrEmpty(SelectedDanhMuc) ||
        !string.IsNullOrEmpty(SelectedThuongHieu) ||
        !string.IsNullOrEmpty(SelectedPriceRange) ||
        MinPrice.HasValue ||
        MaxPrice.HasValue ||
        ConHang == true;
}

