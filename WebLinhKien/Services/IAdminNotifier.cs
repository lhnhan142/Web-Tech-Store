namespace WebLinhKien.Services;

/// <summary>
/// Gửi thông báo thời gian thực tới các trang quản trị đang mở.
/// Các thành viên khác chỉ cần inject interface này, không cần biết WebSocket chạy thế nào.
/// Các hàm không ném lỗi ra ngoài: nếu gửi thất bại chỉ ghi log, để việc đặt hàng không bị hỏng.
/// </summary>
public interface IAdminNotifier
{
    /// <summary>Báo có đơn mới. Gọi SAU KHI đơn đã lưu và transaction đã commit.</summary>
    Task NotifyNewOrderAsync(string maDon, string tenKhachHang, decimal tongTien);

    /// <summary>Báo sản phẩm sắp hết hàng.</summary>
    Task NotifyLowStockAsync(int uidSanPham, string tenSanPham, int tonKho);
}
