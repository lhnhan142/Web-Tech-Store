using System.Globalization;

namespace WebLinhKien.Helpers;

public static class FormatExtensions
{
    private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>1180000 -> "1.180.000 đ"</summary>
    public static string ToVnd(this decimal value) => value.ToString("N0", ViVn) + " đ";

    /// <summary>"01/11/2026 14:05"</summary>
    public static string ToNgayGio(this DateTime value) => value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
}
