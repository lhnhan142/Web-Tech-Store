using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Models;

namespace WebLinhKien.Repositories.Interfaces;

public interface IDonDatHangRepository
{
    // ===================== Đọc đơn và đổi tình trạng =====================

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

    // ===================== Tạo đơn và tra đơn =====================

    /// <summary>
    /// Trong MỘT transaction: trừ tồn kho từng sản phẩm (chỉ trừ khi còn đủ hàng), thêm DonDatHangs và ChiTietDonHangs.
    /// Không đủ hàng thì rollback và trả TaoDonKetQua.ThieuHang. Trùng MaDon sẽ ném SqlException (2601/2627).
    /// </summary>
    Task<TaoDonKetQua> TaoDonAsync(DonHangMoi don, IReadOnlyList<DongHangMoi> dong);

    /// <summary>Tìm đơn theo mã đơn VÀ số điện thoại (để người ngoài không dò được đơn của người khác).</summary>
    Task<DonDatHang?> GetByMaDonVaSdtAsync(string maDon, string sdt);
}
