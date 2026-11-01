using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

/// <summary>
/// Chỉ chứa các method dùng chung (đọc theo khóa). B (danh sách, tìm kiếm, lọc)
/// và D (thêm, sửa, xóa) thêm method riêng ở file ISanPhamRepository.B.cs / .D.cs
/// vì interface và class đều khai báo "partial".
/// </summary>
public partial interface ISanPhamRepository
{
    Task<SanPham?> GetByUidAsync(int uid);
    Task<SanPham?> GetByMaAsync(string maSanPham);
    Task<IReadOnlyList<SanPham>> GetByUidsAsync(IEnumerable<int> uids);
}
