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
    Task<Dictionary<string, int>> DemTheoDanhMucAsync();
    Task<Dictionary<string, int>> DemTheoThuongHieuAsync();

    // ===================== Trang khách =====================
    Task<IReadOnlyList<SanPham>> GetFeaturedProductsAsync(int limit = 8);
    Task<IReadOnlyList<SanPham>> GetNewArrivalsAsync(int limit = 8);
    Task<IReadOnlyList<SanPham>> GetBestDealsAsync(int limit = 8);
    Task<IReadOnlyList<ThuongHieuFilterItem>> GetBrandsByCategoryAsync(string maDanhMuc);
    Task<PagedResult<SanPham>> GetPagedProductsAsync(ProductFilterRequest filter);

    // Sửa lại: Dùng ProductFilterRequest và có thêm tham số admin
    Task<IReadOnlyDictionary<string, int>> GetSpecFacetsAsync(ProductFilterRequest filter, bool admin = false, int nguong = 0);

    Task<IReadOnlyList<SanPham>> SearchSuggestionsAsync(string keyword, int limit = 6);
    Task<IReadOnlyList<HinhAnhSanPham>> GetProductImagesAsync(int uidSanPham);
    Task<IReadOnlyList<ThongSoKyThuat>> GetProductSpecificationsAsync(int uidSanPham);
    Task<IReadOnlyList<SanPham>> GetRelatedProductsAsync(string maDanhMuc, int excludeUid, int limit = 4);

    // ===================== Trang quản trị =====================

    // Sửa lại: Dùng ProductFilterRequest cho gọn
    Task<PagedResult<SanPham>> GetPagedAdminAsync(ProductFilterRequest filter, int nguong, int pageSize);
    Task<(int SoSapHet, int SoHetHang)> DemKhoAsync(ProductFilterRequest filter, int nguong);
    Task<bool> MaSanPhamTonTaiAsync(string maSanPham, int? boQuaUid = null);
    Task<int> InsertAsync(SanPham sanPham, IReadOnlyList<ThongSoKyThuat> thongSos, IReadOnlyList<string> anhPhus);
    Task<bool> UpdateAsync(SanPham sanPham, IReadOnlyList<ThongSoKyThuat> thongSos);
    Task<bool> DeleteAsync(int uid);
    Task<int> DemTrongDonHangAsync(int uidSanPham);
    Task<IReadOnlyList<ThongSoKyThuat>> GetThongSosAsync(int uidSanPham);
    Task<IReadOnlyList<HinhAnhSanPham>> GetHinhAnhsAsync(int uidSanPham);
    Task<HinhAnhSanPham?> GetHinhAnhAsync(int uid);
    Task<int> ThemHinhAnhAsync(int uidSanPham, string duongDan);
    Task<bool> XoaHinhAnhAsync(int uid);
    Task<bool> DatAnhDaiDienAsync(int uidSanPham, string? duongDan);
}