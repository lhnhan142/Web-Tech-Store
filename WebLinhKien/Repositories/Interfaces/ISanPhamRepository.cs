using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels.Client;
using WebLinhKien.Models;

namespace WebLinhKien.Repositories.Interfaces;

public interface ISanPhamRepository
{
    // ===================== Dùng chung =====================

    Task<SanPham?> GetByUidAsync(int uid);
    Task<SanPham?> GetByMaAsync(string maSanPham);
    Task<IReadOnlyList<SanPham>> GetByUidsAsync(IEnumerable<int> uids);

    // ===================== Thống kê =====================

    /// <summary>Số sản phẩm theo từng danh mục (khóa không phân biệt hoa thường).</summary>
    Task<Dictionary<string, int>> DemTheoDanhMucAsync();

    /// <summary>Số sản phẩm theo từng thương hiệu (bỏ qua sản phẩm chưa có thương hiệu).</summary>
    Task<Dictionary<string, int>> DemTheoThuongHieuAsync();

    // ===================== Trang khách =====================

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

    // ===================== Trang quản trị =====================

    /// <summary>Danh sách cho trang quản trị: thấy cả sản phẩm đang ẩn, không lấy cột MoTa cho nhẹ.</summary>
    Task<PagedResult<SanPham>> GetPagedAdminAsync(string? tuKhoa, string? maDanhMuc, bool chiSapHet, int nguongSapHet, int page, int pageSize);

    /// <summary>Số sản phẩm có tồn kho từ ngưỡng trở xuống (gồm cả hết hàng).</summary>
    Task<int> DemSapHetAsync(int nguongSapHet);

    Task<bool> MaSanPhamTonTaiAsync(string maSanPham, int? boQuaUid = null);

    /// <summary>Thêm sản phẩm, thông số kỹ thuật và ảnh trong một transaction. Trả về Uid mới.</summary>
    Task<int> InsertAsync(SanPham sanPham, IReadOnlyList<ThongSoKyThuat> thongSos, IReadOnlyList<string> anhPhus);

    /// <summary>Sửa sản phẩm và thay toàn bộ thông số kỹ thuật trong một transaction. Không đụng tới ảnh.</summary>
    Task<bool> UpdateAsync(SanPham sanPham, IReadOnlyList<ThongSoKyThuat> thongSos);

    /// <summary>Xóa sản phẩm kèm ảnh và thông số (không có khóa ngoại nên xóa tay trong transaction).</summary>
    Task<bool> DeleteAsync(int uid);

    Task<int> DemTrongDonHangAsync(int uidSanPham);

    Task<IReadOnlyList<ThongSoKyThuat>> GetThongSosAsync(int uidSanPham);
    Task<IReadOnlyList<HinhAnhSanPham>> GetHinhAnhsAsync(int uidSanPham);
    Task<HinhAnhSanPham?> GetHinhAnhAsync(int uid);
    Task<int> ThemHinhAnhAsync(int uidSanPham, string duongDan);
    Task<bool> XoaHinhAnhAsync(int uid);
    Task<bool> DatAnhDaiDienAsync(int uidSanPham, string? duongDan);
}
