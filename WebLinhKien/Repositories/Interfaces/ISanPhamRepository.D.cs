using WebLinhKien.Models;
using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

/// <summary>Phần của D: danh sách quản trị, thêm, sửa, xóa sản phẩm cùng thông số kỹ thuật và ảnh.</summary>
public partial interface ISanPhamRepository
{
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
