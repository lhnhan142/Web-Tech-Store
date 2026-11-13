namespace WebLinhKien.Models.ViewModels.Client;

/// <summary>Một lựa chọn trong nhóm lọc theo thông số (vd: DDR5, 32GB, RTX 40).</summary>
public sealed class SpecOption
{
    /// <summary>Mã ngắn dùng trên URL, không chứa dấu phẩy hoặc dấu hai chấm (vd: DDR5, 32, rtx40).</summary>
    public string Value { get; }

    /// <summary>Chữ hiển thị cho khách.</summary>
    public string Label { get; }

    /// <summary>Các mẫu LIKE so với cột GiaTri của bảng ThongSoKyThuats (khớp một trong các mẫu là đạt).</summary>
    public string[] Patterns { get; }

    public SpecOption(string value, string label, params string[] patterns)
    {
        Value = value;
        Label = label;
        Patterns = patterns.Length == 0 ? new[] { value } : patterns;
    }
}

/// <summary>Một nhóm lọc theo thông số (vd: "Loại RAM", "Dung lượng").</summary>
public sealed class SpecGroup
{
    /// <summary>Mã nhóm dùng trên URL (vd: ram_loai).</summary>
    public string Key { get; }

    /// <summary>Tiêu đề hiển thị.</summary>
    public string Label { get; }

    /// <summary>Tên thông số trong bảng ThongSoKyThuats (cột TenThongSo).</summary>
    public string TenThongSo { get; }

    public IReadOnlyList<SpecOption> Options { get; }

    public SpecGroup(string key, string label, string tenThongSo, params SpecOption[] options)
    {
        Key = key;
        Label = label;
        TenThongSo = tenThongSo;
        Options = options;
    }
}

/// <summary>
/// Khai báo các nhóm lọc theo thông số cho từng danh mục.
/// Muốn thêm lọc cho danh mục khác (CPU, MAINBOARD...), thêm một mục vào ByCategory bên dưới.
/// URL dùng dạng: /SanPham?danhMuc=RAM&amp;ts=ram_loai:DDR5&amp;ts=ram_dl:32
/// </summary>
public static class SpecFilterDefinitions
{
    private static readonly Dictionary<string, SpecGroup[]> ByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RAM"] = new[]
        {
            new SpecGroup("ram_loai", "Loại RAM", "Loại RAM",
                new SpecOption("DDR4", "DDR4", "DDR4"),
                new SpecOption("DDR5", "DDR5", "DDR5")),
            new SpecGroup("ram_dl", "Dung lượng", "Dung lượng",
                new SpecOption("8", "8GB", "8GB%"),
                new SpecOption("16", "16GB", "16GB%"),
                new SpecOption("32", "32GB", "32GB%"),
                new SpecOption("64", "64GB", "64GB%")),
            new SpecGroup("ram_bus", "Bus (tốc độ)", "Bus",
                new SpecOption("3200", "3200 MHz", "3200 MHz"),
                new SpecOption("3600", "3600 MHz", "3600 MHz"),
                new SpecOption("4800", "4800 MHz", "4800 MHz"),
                new SpecOption("5200", "5200 MHz", "5200 MHz"),
                new SpecOption("5600", "5600 MHz", "5600 MHz"),
                new SpecOption("6000", "6000 MHz", "6000 MHz"),
                new SpecOption("6400", "6400 MHz", "6400 MHz"))
        },
        ["VGA"] = new[]
        {
            new SpecGroup("vga_dong", "Dòng GPU", "GPU",
                new SpecOption("rtx50", "GeForce RTX 50 (RTX 5060 – 5090)", "GeForce RTX 5%"),
                new SpecOption("rtx40", "GeForce RTX 40 (RTX 4060 – 4090)", "GeForce RTX 4%"),
                new SpecOption("rtx30", "GeForce RTX 30 (RTX 3050 – 3060)", "GeForce RTX 3%"),
                new SpecOption("gtx16", "GeForce GTX 16", "GeForce GTX 16%"),
                new SpecOption("rx9", "Radeon RX 9000 (RX 9060 – 9070 XT)", "Radeon RX 9%"),
                new SpecOption("rx7", "Radeon RX 7000 (RX 7600 – 7900 XTX)", "Radeon RX 7%"),
                new SpecOption("rx6", "Radeon RX 6000 (RX 6500 – 6700 XT)", "Radeon RX 6%")),
            new SpecGroup("vga_vram", "Bộ nhớ VRAM", "Bộ nhớ",
                new SpecOption("4", "4GB", "4GB%"),
                new SpecOption("6", "6GB", "6GB%"),
                new SpecOption("8", "8GB", "8GB%"),
                new SpecOption("12", "12GB", "12GB%"),
                new SpecOption("16", "16GB", "16GB%"),
                new SpecOption("20", "20GB", "20GB%"),
                new SpecOption("24", "24GB", "24GB%"),
                new SpecOption("32", "32GB", "32GB%"))
        },
        ["SSD"] = new[]
        {
            new SpecGroup("ssd_dl", "Dung lượng", "Dung lượng",
                new SpecOption("lt1tb", "Dưới 1TB (≤ 512GB)", "120GB", "128GB", "240GB", "256GB", "480GB", "500GB", "512GB"),
                new SpecOption("1tb", "1TB", "1TB"),
                new SpecOption("2tb", "2TB", "2TB"),
                new SpecOption("4tb", "4TB", "4TB")),
            new SpecGroup("ssd_kn", "Chuẩn kết nối", "Chuẩn kết nối",
                new SpecOption("sata", "SATA", "SATA%"),
                new SpecOption("gen3", "NVMe PCIe Gen 3", "%PCIe 3.0%"),
                new SpecOption("gen4", "NVMe PCIe Gen 4", "%PCIe 4.0%"),
                new SpecOption("gen5", "NVMe PCIe Gen 5", "%PCIe 5.0%"))
        }
    };

    /// <summary>Các nhóm lọc thông số của một danh mục (rỗng nếu danh mục chưa có lọc thông số).</summary>
    public static IReadOnlyList<SpecGroup> ForCategory(string? maDanhMuc)
    {
        if (!string.IsNullOrWhiteSpace(maDanhMuc) && ByCategory.TryGetValue(maDanhMuc.Trim(), out var groups))
        {
            return groups;
        }
        return Array.Empty<SpecGroup>();
    }

    /// <summary>Kiểm tra và chuẩn hóa một lựa chọn "khóa:giá trị". Trả về null nếu không hợp lệ với danh mục.</summary>
    public static string? Canonicalize(string? maDanhMuc, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var idx = token.IndexOf(':');
        if (idx <= 0) return null;

        var key = token.Substring(0, idx).Trim();
        var value = token.Substring(idx + 1).Trim();

        foreach (var group in ForCategory(maDanhMuc))
        {
            if (!group.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var option in group.Options)
            {
                if (option.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return group.Key + ":" + option.Value;
                }
            }
        }
        return null;
    }

    /// <summary>Lấy chữ hiển thị (tên nhóm, tên lựa chọn) của một lựa chọn đã chuẩn hóa.</summary>
    public static (string Group, string Option)? Describe(string? maDanhMuc, string? token)
    {
        var canonical = Canonicalize(maDanhMuc, token);
        if (canonical == null) return null;

        var idx = canonical.IndexOf(':');
        var key = canonical.Substring(0, idx);
        var value = canonical.Substring(idx + 1);

        foreach (var group in ForCategory(maDanhMuc))
        {
            if (group.Key != key) continue;
            foreach (var option in group.Options)
            {
                if (option.Value == value) return (group.Label, option.Label);
            }
        }
        return null;
    }
}
