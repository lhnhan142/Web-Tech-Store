using WebLinhKien.Models.Requests;

namespace WebLinhKien.Services;

public record NganHangInfo(string Ten, string SoTaiKhoan, string ChuTaiKhoan, string MaNganHang);

/// <summary>Kết quả đặt hàng. Status theo mã HTTP: 200 thành công, 400 dữ liệu sai, 409 hết hàng, 500 lỗi hệ thống.</summary>
public class DatHangKetQua
{
    public int Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? MaDon { get; init; }
    public decimal? TongThanhToan { get; init; }
    /// <summary>Chỉ có khi chọn CHUYEN_KHOAN và cửa hàng đã cấu hình ngân hàng trong CaiDats.</summary>
    public NganHangInfo? NganHang { get; init; }
}

public interface IDatHangService
{
    /// <summary>Request đã được validate (FluentValidation) và chuẩn hóa ở controller.</summary>
    Task<DatHangKetQua> DatHangAsync(DatHangRequest request);

    /// <summary>Ảnh PNG mã QR chuyển khoản của đơn; null nếu đơn không hợp lệ hoặc chưa cấu hình ngân hàng.</summary>
    Task<byte[]?> TaoQrPngAsync(string maDon, string sdt);
}
