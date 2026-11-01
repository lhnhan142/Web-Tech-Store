using System.Text.Json;
using WebLinhKien.Models;

namespace WebLinhKien.Helpers;

/// <summary>
/// Đọc/ghi giỏ hàng trong session (chỉ lưu Uid và số lượng; tên, giá luôn lấy lại từ database).
/// Thành viên C mở rộng thêm các hàm thêm, sửa, xóa sản phẩm khi làm chức năng giỏ hàng.
/// </summary>
public static class GioHangSession
{
    private const string Key = "GIO_HANG";

    public static List<GioHangItem> Doc(ISession session)
    {
        var json = session.GetString(Key);
        if (string.IsNullOrEmpty(json)) return new List<GioHangItem>();

        try { return JsonSerializer.Deserialize<List<GioHangItem>>(json) ?? new List<GioHangItem>(); }
        catch (JsonException) { return new List<GioHangItem>(); }
    }

    public static void Ghi(ISession session, List<GioHangItem> gio) =>
        session.SetString(Key, JsonSerializer.Serialize(gio));

    public static void Xoa(ISession session) => session.Remove(Key);
}
