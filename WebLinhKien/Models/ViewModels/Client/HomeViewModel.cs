namespace WebLinhKien.Models.ViewModels.Client;

/// <summary>
/// ViewModel truyền dữ liệu thực tế từ SQL Server sang Trang chủ (Views/Home/Index.cshtml).
/// </summary>
public class HomeViewModel
{
    public IReadOnlyList<CategoryCardViewModel> Categories { get; set; } = Array.Empty<CategoryCardViewModel>();
    public IReadOnlyList<BrandCardViewModel> Brands { get; set; } = Array.Empty<BrandCardViewModel>();
    public IReadOnlyList<ProductCardViewModel> FeaturedProducts { get; set; } = Array.Empty<ProductCardViewModel>();
    public IReadOnlyList<ProductCardViewModel> NewArrivals { get; set; } = Array.Empty<ProductCardViewModel>();
    public IReadOnlyList<ProductCardViewModel> BestDeals { get; set; } = Array.Empty<ProductCardViewModel>();
}

