using System.Globalization;

namespace WebLinhKien.Helpers;

public enum CaiDatLoai { Text, Money, Int, MoneyList }

/// <summary>
/// Quy tắc kiểm tra cho từng khóa trong bảng CaiDats. Thêm khóa mới (CaiDatKeys) thì thêm một dòng ở đây.
/// Khóa chưa khai báo được coi là văn bản tối đa 500 ký tự.
/// </summary>
public static class CaiDatRules
{
    public sealed record Rule(
        string Nhan,
        CaiDatLoai Loai,
        long Min,
        long Max,
        string? MoTa = null,
        string? MacDinh = null);

    private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

    private static readonly Dictionary<string, Rule> Rules = new()
    {
        [CaiDatKeys.PhiGiaoHang]           = new("Phí giao hàng (đ)", CaiDatLoai.Money, 0, 10_000_000),
        [CaiDatKeys.NguongMienPhiGiaoHang] = new("Ngưỡng miễn phí giao hàng (đ)", CaiDatLoai.Money, 0, 1_000_000_000),
        [CaiDatKeys.NguongSapHetHang]      = new("Ngưỡng cảnh báo sắp hết hàng (số lượng)", CaiDatLoai.Int, 0, 1000),
        [CaiDatKeys.BuildPcNganSachMacDinh] = new(
            "Build PC – ngân sách mặc định (đ)",
            CaiDatLoai.Money,
            1_000_000,
            1_000_000_000,
            "Số tiền điền sẵn ở tab Gợi ý theo ngân sách.",
            "20000000"),
        [CaiDatKeys.BuildPcNganSachGoiY] = new(
            "Build PC – các mức ngân sách gợi ý (đ)",
            CaiDatLoai.MoneyList,
            1_000_000,
            1_000_000_000,
            "Các nút chọn nhanh, cách nhau bằng dấu phẩy; từ 2 đến 5 mức. Ví dụ: 15000000,20000000,30000000",
            "15000000,20000000,30000000")
    };

    public static Rule Get(string khoa) =>
        Rules.TryGetValue(khoa, out var rule) ? rule : new Rule(khoa, CaiDatLoai.Text, 1, 500);

    /// <summary>Khóa đã được khai báo quy tắc (được phép lưu dù chưa có dòng trong database).</summary>
    public static bool DaBiet(string khoa) => Rules.ContainsKey(khoa);

    /// <summary>Trả về thông báo lỗi, hoặc null nếu giá trị hợp lệ.</summary>
    public static string? Validate(string khoa, string? giaTri)
    {
        var rule = Get(khoa);
        var v = giaTri?.Trim() ?? string.Empty;
        if (v.Length == 0) return "Vui lòng nhập giá trị.";

        if (rule.Loai == CaiDatLoai.Text)
        {
            return v.Length < rule.Min || v.Length > rule.Max
                ? $"Phải từ {rule.Min} đến {rule.Max} ký tự."
                : null;
        }

        if (rule.Loai == CaiDatLoai.MoneyList)
        {
            var parts = v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || parts.Length > 5)
                return "Cần từ 2 đến 5 mức, cách nhau bằng dấu phẩy.";

            foreach (var part in parts)
            {
                if (part.Length > 12 || !part.All(char.IsAsciiDigit))
                    return "Mỗi mức chỉ nhập số nguyên không âm, không dùng dấu chấm.";
                var n = long.Parse(part, CultureInfo.InvariantCulture);
                if (n < rule.Min || n > rule.Max)
                    return $"Mỗi mức phải từ {rule.Min.ToString("N0", ViVn)} đến {rule.Max.ToString("N0", ViVn)}.";
            }

            return null;
        }

        // Money / Int: chỉ chữ số, để các nơi khác đọc ra số (decimal.TryParse) luôn thành công
        if (v.Length > 12 || !v.All(char.IsAsciiDigit))
            return "Chỉ nhập số nguyên không âm, không dùng dấu chấm hoặc dấu phẩy.";

        var so = long.Parse(v, CultureInfo.InvariantCulture);
        return so < rule.Min || so > rule.Max
            ? $"Giá trị phải từ {rule.Min.ToString("N0", ViVn)} đến {rule.Max.ToString("N0", ViVn)}."
            : null;
    }
}
