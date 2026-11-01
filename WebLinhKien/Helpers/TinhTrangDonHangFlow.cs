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

    /// <summary>Class Tailwind cho nhãn màu của từng tình trạng.</summary>
    public static string BadgeClass(string ma)
    {
        const string co = "inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold ";
        return co + ma switch
        {
            Moi          => "bg-amber-100 text-amber-800",
            DangXuLy     => "bg-blue-100 text-blue-800",
            DangGiaoHang => "bg-indigo-100 text-indigo-800",
            DaNhan       => "bg-green-100 text-green-800",
            HoanTra      => "bg-red-100 text-red-800",
            DoiHang      => "bg-purple-100 text-purple-800",
            _            => "bg-slate-100 text-slate-700"
        };
    }
}
