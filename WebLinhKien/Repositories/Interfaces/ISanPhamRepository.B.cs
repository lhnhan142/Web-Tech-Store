using WebLinhKien.Models;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels.Client;

namespace WebLinhKien.Repositories.Interfaces;

/// <summary>
/// Mở rộng ISanPhamRepository dành riêng cho Thành viên B:
/// Lấy sản phẩm nổi bật, mới về, khuyến mãi, lọc đa tiêu chí, gợi ý search, gallery ảnh, thông số kỹ thuật và sản phẩm liên quan.
/// </summary>
public partial interface ISanPhamRepository
{
    /// <summary>Lấy danh sách sản phẩm nổi bật (NoiBat = 1, HienThi = 1)</summary>
    Task<IReadOnlyList<SanPham>> GetFeaturedProductsAsync(int limit = 8);

    /// <summary>Lấy danh sách sản phẩm mới nhất theo ngày tạo (HienThi = 1)</summary>
    Task<IReadOnlyList<SanPham>> GetNewArrivalsAsync(int limit = 8);

    /// <summary>Lấy danh sách sản phẩm đang có giá khuyến mãi (GiaGoc > GiaBan, HienThi = 1)</summary>
    Task<IReadOnlyList<SanPham>> GetBestDealsAsync(int limit = 8);

    /// <summary>Lọc sản phẩm đa tiêu chí kết hợp sắp xếp và phân trang tại SQL Server</summary>
    Task<PagedResult<SanPham>> GetPagedProductsAsync(ProductFilterRequest filter);

    /// <summary>Gợi ý tìm kiếm nhanh (Live Search) theo từ khóa (tên hoặc mã sản phẩm)</summary>
    Task<IReadOnlyList<SanPham>> SearchSuggestionsAsync(string keyword, int limit = 6);

    /// <summary>Lấy danh sách ảnh phụ của một sản phẩm từ bảng HinhAnhSanPhams</summary>
    Task<IReadOnlyList<HinhAnhSanPham>> GetProductImagesAsync(int uidSanPham);

    /// <summary>Lấy danh sách thông số kỹ thuật của một sản phẩm từ bảng ThongSoKyThuats</summary>
    Task<IReadOnlyList<ThongSoKyThuat>> GetProductSpecificationsAsync(int uidSanPham);

    /// <summary>Lấy danh sách sản phẩm liên quan cùng danh mục (loại trừ sản phẩm hiện tại)</summary>
    Task<IReadOnlyList<SanPham>> GetRelatedProductsAsync(string maDanhMuc, int excludeUid, int limit = 4);
}
