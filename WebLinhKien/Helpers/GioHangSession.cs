using System.Text.Json;
using WebLinhKien.Models;

namespace WebLinhKien.Helpers;

/// <summary>
/// Đọc/ghi giỏ hàng trong session (chỉ lưu Uid và số lượng; tên, giá luôn lấy lại từ database).
/// Phần sửa, xóa sản phẩm do thành viên C bổ sung. Việc kiểm tra tồn kho làm ở GioHangController và SanPhamController.ThemVaoGio.
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

    /// <summary>Xóa toàn bộ giỏ hàng (sau khi đặt hàng thành công).</summary>
    public static void Xoa(ISession session) => session.Remove(Key);

    /// <summary>Tổng số lượng sản phẩm trong giỏ, hiện trên icon giỏ hàng.</summary>
    public static int Dem(ISession session) => Doc(session).Sum(g => g.SoLuong);

    /// <summary>Số lượng hiện có của một sản phẩm trong giỏ (0 nếu chưa có).</summary>
    public static int SoLuong(ISession session, int uidSanPham) =>
        Doc(session).FirstOrDefault(g => g.UidSanPham == uidSanPham)?.SoLuong ?? 0;

    /// <summary>Đặt số lượng của một sản phẩm; soLuong &lt;= 0 thì bỏ sản phẩm khỏi giỏ.</summary>
    public static void DatSoLuong(ISession session, int uidSanPham, int soLuong)
    {
        var gio = Doc(session);
        gio.RemoveAll(g => g.UidSanPham == uidSanPham);
        if (soLuong > 0) gio.Add(new GioHangItem { UidSanPham = uidSanPham, SoLuong = soLuong });
        Ghi(session, gio);
    }
}
