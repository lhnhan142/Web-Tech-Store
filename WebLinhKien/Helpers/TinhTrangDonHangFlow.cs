namespace WebLinhKien.Helpers;

/// <summary>
/// Quy định đơn hàng được chuyển từ tình trạng nào sang tình trạng nào.
/// Muốn đổi luồng chỉ cần sửa bảng TiepTheo bên dưới.
/// </summary>
public static class TinhTrangDonHangFlow
{
    public const string Moi = "MOI";
    public const string DangXuLy = "DANG_XU_LY";
    public const string DangGiaoHang = "DANG_GIAO_HANG";
    public const string DaNhan = "DA_NHAN";
    public const string HoanTra = "HOAN_TRA";
    public const string DoiHang = "DOI_HANG";

    private static readonly Dictionary<string, string[]> TiepTheo = new()
    {
        [Moi]          = new[] { DangXuLy },
        [DangXuLy]     = new[] { DangGiaoHang, HoanTra },
        [DangGiaoHang] = new[] { DaNhan, HoanTra },
        [DaNhan]       = new[] { HoanTra, DoiHang },
        [DoiHang]      = new[] { DangGiaoHang, DaNhan },
        [HoanTra]      = Array.Empty<string>()
    };

    public static IReadOnlyList<string> CacTinhTrangTiepTheo(string maHienTai) =>
        TiepTheo.TryGetValue(maHienTai, out var ds) ? ds : Array.Empty<string>();

    public static bool HopLe(string tu, string den) => CacTinhTrangTiepTheo(tu).Contains(den);

    /// <summary>Class CSS (trong admin.css) cho nhãn màu của từng tình trạng, ví dụ "ad-badge ad-badge--moi".</summary>
    public static string BadgeClass(string ma) =>
        "ad-badge ad-badge--" + (string.IsNullOrEmpty(ma) ? "none" : ma.ToLowerInvariant());
}
