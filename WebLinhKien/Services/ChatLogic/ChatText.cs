using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebLinhKien.Services.ChatLogic;

public sealed record BrandRef(string Ma, string Ten);

/// <summary>
/// Đọc câu hỏi tiếng Việt của khách: bỏ dấu, đọc mức giá (10tr, dưới 5 triệu, từ 2 đến 4 triệu...),
/// nhận diện loại linh kiện, mục đích sử dụng và hãng. Không phụ thuộc database nên chạy thử riêng được.
/// </summary>
public static class ChatText
{
    /// <summary>Min/Max là khoảng lọc giá; Target là con số khách nhắc (dùng làm ngân sách khi ghép cấu hình).</summary>
    public sealed record MoneyQuery(decimal? Min, decimal? Max, decimal Target);

    // ---------- Chuẩn hóa chữ ----------

    /// <summary>Chữ thường, bỏ dấu tiếng Việt, giữ nguyên số và dấu câu.</summary>
    public static string Fold(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var d = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Như Fold nhưng chỉ còn chữ và số, các từ cách nhau một dấu cách và có dấu cách ở hai đầu (để tìm " ram ").</summary>
    public static string Normalize(string? s)
    {
        var f = Regex.Replace(Fold(s), @"[^a-z0-9]+", " ").Trim();
        return " " + f + " ";
    }

    private static bool Has(string norm, params string[] phrases) => phrases.Any(p => norm.Contains(p));

    // ---------- Tiền ----------

    private const string UnitPattern = "trieu|tr|cu|nghin|ngan|k";
    private static readonly Regex RangeRx = new(
        $@"(?:tu\s*)?(?<a>\d+(?:[.,]\d+)?)\s*(?<ua>{UnitPattern})?\s*(?:den|toi|[-–—])\s*(?<b>\d+(?:[.,]\d+)?)\s*(?<ub>{UnitPattern})(?![a-z])",
        RegexOptions.Compiled);
    private static readonly Regex AmountRx = new(
        $@"(?<n>\d+(?:[.,]\d+)?)\s*(?<u>{UnitPattern})(?<d>\d)?(?![a-z])", RegexOptions.Compiled);
    private static readonly Regex RawRx = new(
        @"(?<![\d.,])(?<v>\d{1,3}(?:[.,]\d{3})+|\d{6,})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex BareAfterQualifierRx = new(
        @"(?<q>duoi|tren|khoang|tam|toi da|khong qua)\s*(?<n>\d{1,2}(?:[.,]\d)?)(?![\d.,]*\s*(?:gb|tb|mhz|hz|w|mm|inch|core|nhan|[a-z]))",
        RegexOptions.Compiled);

    private const decimal MinMoney = 50_000m;

    private static decimal Unit(string u) => u switch
    {
        "trieu" or "tr" or "cu" => 1_000_000m,
        _ => 1_000m
    };

    private static decimal Num(string s) =>
        decimal.Parse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);

    /// <summary>Đọc mức giá trong câu. Trả về null nếu không có con số tiền nào.</summary>
    public static MoneyQuery? ParseMoney(string? text)
    {
        var t = Fold(text);
        if (t.Length == 0) return null;

        // 1. Khoảng: "từ 2 đến 5 triệu", "2-5tr", "500k - 1,5 triệu"
        var r = RangeRx.Match(t);
        if (r.Success)
        {
            var ub = r.Groups["ub"].Value;
            var ua = r.Groups["ua"].Success ? r.Groups["ua"].Value : ub;
            var a = Num(r.Groups["a"].Value) * Unit(ua);
            var b = Num(r.Groups["b"].Value) * Unit(ub);
            if (a > b) (a, b) = (b, a);
            if (b >= MinMoney) return new MoneyQuery(a, b, b);
        }

        // 2. Một con số: "10tr", "10 triệu", "10tr5", "500k", "10.000.000"
        decimal? value = null;
        int index = -1, length = 0;
        var m = AmountRx.Match(t);
        if (m.Success)
        {
            var unit = Unit(m.Groups["u"].Value);
            value = Num(m.Groups["n"].Value) * unit;
            if (m.Groups["d"].Success && unit == 1_000_000m) value += int.Parse(m.Groups["d"].Value) * 100_000m;
            index = m.Index; length = m.Length;
        }
        else
        {
            var raw = RawRx.Match(t);
            if (raw.Success)
            {
                value = decimal.Parse(Regex.Replace(raw.Groups["v"].Value, @"[.,]", ""), CultureInfo.InvariantCulture);
                index = raw.Index; length = raw.Length;
            }
            else
            {
                var bare = BareAfterQualifierRx.Match(t);
                if (bare.Success)
                {
                    value = Num(bare.Groups["n"].Value) * 1_000_000m;   // "dưới 5" hiểu là 5 triệu
                    index = bare.Groups["n"].Index; length = bare.Groups["n"].Length;
                }
            }
        }
        if (value is null || value < MinMoney) return null;
        var v = value.Value;

        var before = t[..index];
        var after = t[(index + length)..];
        if (Regex.IsMatch(after, @"^\s*tro len")) return new MoneyQuery(v, null, v);
        if (Regex.IsMatch(after, @"^\s*tro xuong")) return new MoneyQuery(null, v, v);
        if (Regex.IsMatch(before, @"(duoi|toi da|khong qua|khong vuot|nho hon|max|<)\s*$")) return new MoneyQuery(null, v, v);
        if (Regex.IsMatch(before, @"(tren|lon hon|toi thieu|it nhat|min|>)\s*$")) return new MoneyQuery(v, null, v);
        if (Regex.IsMatch(before, @"(khoang|tam|xap xi|gan|~)\s*$")) return new MoneyQuery(Math.Round(v * 0.7m), Math.Round(v * 1.15m), v);
        return new MoneyQuery(null, v, v);   // "10tr", "có 10 triệu": coi là mức tối đa
    }

    /// <summary>Hiển thị tiền ngắn gọn: 1500000 -> "1,5 triệu", 500000 -> "500 nghìn".</summary>
    public static string FormatMoney(decimal vnd)
    {
        if (vnd >= 1_000_000m)
        {
            var tr = vnd / 1_000_000m;
            var s = tr == Math.Floor(tr) ? tr.ToString("0", CultureInfo.InvariantCulture) : tr.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
            return s + " triệu";
        }
        return (vnd / 1_000m).ToString("0", CultureInfo.InvariantCulture) + " nghìn";
    }

    /// <summary>Bỏ các cụm tiền khỏi câu để phần còn lại chỉ là từ khóa tên sản phẩm.</summary>
    private static string StripMoney(string folded)
    {
        var t = RangeRx.Replace(folded, " ");
        t = AmountRx.Replace(t, " ");
        return RawRx.Replace(t, " ");
    }

    // ---------- Ý định ----------

    public static bool WantsReset(string norm) => Has(norm, " tu van khac ", " bat dau lai ", " lam lai ", " reset ", " menu ", " quay lai menu ");
    public static bool WantsChangeBudget(string norm) => Has(norm, " doi muc gia ", " doi ngan sach ", " muc gia khac ", " ngan sach khac ");
    public static bool WantsChangePurpose(string norm) => Has(norm, " doi muc dich ", " muc dich khac ");
    public static bool WantsOtherBrand(string norm) => Has(norm, " hang khac ", " doi hang ", " bo loc hang ");
    public static bool WantsAnyPrice(string norm) => Has(norm, " muc gia bat ky ", " gia bat ky ", " khong gioi han ", " khong quan trong gia ", " bat ky gia ");
    public static bool WantsParts(string norm) => Has(norm, " linh kien ", " tim san pham ");

    /// <summary>Nói rõ là muốn ghép cả bộ máy (không nhầm với "case pc gaming").</summary>
    public static bool StrongPc(string norm) => Has(norm, " cau hinh ", " build ", " lap rap ", " lap pc ", " bo pc ", " may bo ",
        " full bo ", " tron bo ", " may tinh de ban ", " tu van pc ", " tu van may ", " dung pc ");
    public static bool WeakPc(string norm) => Has(norm, " pc ", " may tinh ", " computer ");

    public static string? DetectPurpose(string norm)
    {
        if (Has(norm, " gaming ", " game ", " gamer ", "choi game", " fps ", " valorant ", " lol ", " pubg ", " esport ", " lien minh "))
            return "gaming";
        if (Has(norm, "do hoa", " render ", "edit video", "dung phim", "thiet ke", " photoshop ", " premiere ", " blender ", " 3d "))
            return "dohoa";
        if (Has(norm, "van phong", " office ", "hoc tap", " hoc ", "lam viec", "ke toan", "word excel", "hoc online", " sinh vien ", "cong viec", " hoc sinh "))
            return "vanphong";
        return null;
    }

    private static readonly (string Ma, string[] Phrases)[] CategoryRules =
    {
        ("VGA", new[] { "card man hinh", "card do hoa", " vga ", " gpu ", " rtx ", " gtx ", " geforce ", " radeon " }),
        ("TANNHIET", new[] { "tan nhiet", "quat cpu", " aio ", " cooler " }),
        ("CASE", new[] { " case ", "vo may", "thung may", "vo case" }),
        ("PSU", new[] { " nguon ", " psu ", "nguon may tinh" }),
        ("SSD", new[] { " ssd ", "o cung", " nvme " }),
        ("RAM", new[] { " ram ", "bo nho ram", "bo nho trong" }),
        ("MAINBOARD", new[] { " main ", " mainboard ", "bo mach chu", " mobo ", "bo mach" }),
        ("CPU", new[] { " cpu ", "vi xu ly", " ryzen ", " chip ", " i3 ", " i5 ", " i7 ", " i9 ", "core ultra" }),
        ("PHUKIEN", new[] { " chuot ", "ban phim", "tai nghe", "man hinh", " webcam ", "lot chuot", "phu kien", " loa ", " mouse ", " keyboard " })
    };

    public static string? DetectCategory(string norm)
    {
        // "Cooler Master" là tên hãng, không phải tản nhiệt
        var n = norm.Replace(" cooler master ", " ").Replace(" coolermaster ", " ");
        foreach (var (ma, phrases) in CategoryRules)
            if (Has(n, phrases)) return ma;
        return null;
    }

    private static string Alnum(string s) => Regex.Replace(Fold(s), @"[^a-z0-9]", "");

    /// <summary>Tìm hãng trong câu. Tên ngắn (tối đa 3 ký tự như MSI, AMD, LG) phải đứng riêng thành một từ.</summary>
    public static string? DetectBrand(string norm, IEnumerable<BrandRef> brands)
    {
        var noSpace = norm.Replace(" ", "");
        string? best = null;
        var bestLen = 0;
        foreach (var b in brands)
        {
            foreach (var key in new[] { Alnum(b.Ten), Alnum(b.Ma) }.Distinct())
            {
                if (key.Length < 2) continue;
                var hit = key.Length <= 3 ? norm.Contains(" " + key + " ") : noSpace.Contains(key);
                if (hit && key.Length > bestLen) { best = b.Ma; bestLen = key.Length; }
            }
        }
        return best;
    }

    /// <summary>Từ khóa mô tả sản phẩm (ddr5, 4060, i5, 16gb...). Chỉ lấy từ có chữ số, đã bỏ các cụm tiền.</summary>
    public static string[] Keywords(string? message)
    {
        var norm = Normalize(StripMoney(Fold(message)));
        return norm.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2 && t.Any(char.IsDigit))
            .Distinct().Take(4).ToArray();
    }
}
