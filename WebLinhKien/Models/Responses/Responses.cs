namespace WebLinhKien.Models.Responses;

/// <summary>Kết quả trả về cho trình duyệt: Status, Message, Error (+ Data khi thành công).</summary>
public class Responses
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Lỗi theo từng ô: { "tenKhachHang": ["..."], "sdt": ["..."] }</summary>
    public object? Error { get; set; }

    /// <summary>Khi đặt hàng thành công: { maDon, tongThanhToan } (dùng để hiện mã đơn và mã QR).</summary>
    public object? Data { get; set; }
}
