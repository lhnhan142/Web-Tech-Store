using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels;

namespace WebLinhKien.Repositories.Interfaces;

/// <summary>Phần của C: THÊM đơn hàng (transaction) và tra đơn để tạo mã QR.</summary>
public partial interface IDonDatHangRepository
{
    /// <summary>
    /// Trong MỘT transaction: trừ tồn kho từng sản phẩm (chỉ trừ khi còn đủ hàng), thêm DonDatHangs và ChiTietDonHangs.
    /// Không đủ hàng thì rollback và trả TaoDonKetQua.ThieuHang. Trùng MaDon sẽ ném SqlException (2601/2627).
    /// </summary>
    Task<TaoDonKetQua> TaoDonAsync(DonHangMoi don, IReadOnlyList<DongHangMoi> dong);

    /// <summary>Tìm đơn theo mã đơn VÀ số điện thoại (để người ngoài không dò được đơn của người khác).</summary>
    Task<DonDatHang?> GetByMaDonVaSdtAsync(string maDon, string sdt);
}
