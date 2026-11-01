using WebLinhKien.Models;
using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

/// <summary>
/// Phần của A: ĐỌC đơn hàng và đổi tình trạng (trang quản trị).
/// Thành viên C thêm phần THÊM ĐƠN (transaction) ở file IDonDatHangRepository.C.cs
/// vì interface và class đều khai báo "partial".
/// </summary>
public partial interface IDonDatHangRepository
{
    Task<PagedResult<DonDatHang>> GetPagedAsync(string? maTinhTrang, string? tuKhoa, int page, int pageSize);
    Task<DonDatHang?> GetByUidAsync(int uid);
    Task<IReadOnlyList<ChiTietDonHang>> GetChiTietAsync(int uidDonHang);

    /// <summary>Số đơn theo từng tình trạng, dùng cho các tab trên danh sách.</summary>
    Task<Dictionary<string, int>> DemTheoTinhTrangAsync();

    /// <summary>
    /// Đổi tình trạng, CHỈ khi đơn vẫn đang ở tình trạng cũ (tránh hai admin sửa cùng lúc).
    /// Trả về false nếu đơn đã bị người khác đổi.
    /// </summary>
    Task<bool> UpdateTinhTrangAsync(int uid, string tinhTrangCu, string tinhTrangMoi);
}
