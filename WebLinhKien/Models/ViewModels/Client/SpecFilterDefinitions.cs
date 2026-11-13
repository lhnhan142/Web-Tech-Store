using System.Globalization;

namespace WebLinhKien.Models.ViewModels.Client;

public sealed class SpecOption
{
    public string Value { get; }
    public string Label { get; }
    public string[] Patterns { get; }

    public SpecOption(string value, string label, params string[] patterns)
    {
        Value = value;
        Label = label;
        Patterns = patterns.Length == 0 ? new[] { value } : patterns;
    }
}

public sealed class SpecGroup
{
    public string Key { get; }
    public string Label { get; }
    public string TenThongSo { get; }
    public IReadOnlyList<SpecOption> Options { get; }

    /// <summary>Tên "thông số" đặc biệt: nhóm này lọc theo TÊN SẢN PHẨM thay vì bảng ThongSoKyThuats.</summary>
    public const string TheoTenSanPham = "@TenSanPham";

    public bool LocTheoTenSanPham => TenThongSo == TheoTenSanPham;

    public SpecGroup(string key, string label, string tenThongSo, params SpecOption[] options)
    {
        Key = key;
        Label = label;
        TenThongSo = tenThongSo;
        Options = options;
    }
}

public static class SpecFilterDefinitions
{
    private static readonly Dictionary<string, SpecGroup[]> ByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CPU"] = new[]
        {
            new SpecGroup("cpu_gen", "Thế hệ CPU", SpecGroup.TheoTenSanPham,
                new SpecOption("intel-core-ultra-2", "Intel Core Ultra (Series 2)", "%Ultra% 2%"),
                new SpecOption("intel-14", "Intel Core Gen 14", "%-14%", "% 14%"),
                new SpecOption("intel-13", "Intel Core Gen 13", "%-13%", "% 13%"),
                new SpecOption("intel-12", "Intel Core Gen 12", "%-12%", "% 12%"),
                new SpecOption("intel-11", "Intel Core Gen 11", "%-11%", "% 11%"),
                new SpecOption("intel-10", "Intel Core Gen 10", "%-10%", "% 10%"),
                new SpecOption("intel-pentium-celeron", "Intel Pentium / Celeron", "%Pentium%", "%Celeron%"),

                new SpecOption("amd-9000", "AMD Ryzen 9000", "% 9[0-9][0-9][0-9]%"),
                new SpecOption("amd-8000", "AMD Ryzen 8000", "% 8[0-9][0-9][0-9]%"),
                new SpecOption("amd-7000", "AMD Ryzen 7000", "% 7[0-9][0-9][0-9]%"),
                new SpecOption("amd-5000", "AMD Ryzen 5000", "% 5[0-9][0-9][0-9]%"),
                new SpecOption("amd-4000", "AMD Ryzen 4000", "% 4[0-9][0-9][0-9]%"),
                new SpecOption("amd-3000", "AMD Ryzen 3000", "% 3[0-9][0-9][0-9]%"),
                new SpecOption("amd-athlon", "AMD Athlon", "%Athlon%")
            )
        },

        ["RAM"] = new[]
        {
            new SpecGroup("ram_loai", "Loại RAM", "Loại RAM",
                new SpecOption("DDR4", "DDR4", "%DDR4%"),
                new SpecOption("DDR5", "DDR5", "%DDR5%")),
            new SpecGroup("ram_dl", "Dung lượng", "Dung lượng",
                new SpecOption("8", "8GB", "%8GB%"),
                new SpecOption("16", "16GB", "%16GB%"),
                new SpecOption("32", "32GB", "%32GB%"),
                new SpecOption("64", "64GB", "%64GB%")),
            new SpecGroup("ram_bus", "Bus (tốc độ)", "Bus",
                new SpecOption("3200", "3200 MHz", "%3200%"),
                new SpecOption("3600", "3600 MHz", "%3600%"),
                new SpecOption("4800", "4800 MHz", "%4800%"),
                new SpecOption("5200", "5200 MHz", "%5200%"),
                new SpecOption("5600", "5600 MHz", "%5600%"),
                new SpecOption("6000", "6000 MHz", "%6000%"),
                new SpecOption("6400", "6400 MHz", "%6400%"))
        },

        ["VGA"] = new[]
        {
            new SpecGroup("vga_dong", "Dòng GPU", "GPU",
                new SpecOption("rtx50", "GeForce RTX 50 (RTX 5060 – 5090)", "%RTX 5%"),
                new SpecOption("rtx40", "GeForce RTX 40 (RTX 4060 – 4090)", "%RTX 4%"),
                new SpecOption("rtx30", "GeForce RTX 30 (RTX 3050 – 3060)", "%RTX 3%"),
                new SpecOption("gtx16", "GeForce GTX 16", "%GTX 16%"),
                new SpecOption("rx9", "Radeon RX 9000 (RX 9060 – 9070 XT)", "%RX 9%"),
                new SpecOption("rx7", "Radeon RX 7000 (RX 7600 – 7900 XTX)", "%RX 7%"),
                new SpecOption("rx6", "Radeon RX 6000 (RX 6500 – 6700 XT)", "%RX 6%")),
            new SpecGroup("vga_vram", "Bộ nhớ VRAM", "Bộ nhớ",
                new SpecOption("4", "4GB", "%4GB%"),
                new SpecOption("6", "6GB", "%6GB%"),
                new SpecOption("8", "8GB", "%8GB%"),
                new SpecOption("12", "12GB", "%12GB%"),
                new SpecOption("16", "16GB", "%16GB%"),
                new SpecOption("20", "20GB", "%20GB%"),
                new SpecOption("24", "24GB", "%24GB%"),
                new SpecOption("32", "32GB", "%32GB%"))
        },

        ["SSD"] = new[]
        {
            new SpecGroup("ssd_dl", "Dung lượng", "Dung lượng",
                new SpecOption("lt1tb", "Dưới 1TB (≤ 512GB)", "%120GB%", "%128GB%", "%240GB%", "%256GB%", "%480GB%", "%500GB%", "%512GB%"),
                new SpecOption("1tb", "1TB", "%1TB%"),
                new SpecOption("2tb", "2TB", "%2TB%"),
                new SpecOption("4tb", "4TB", "%4TB%")),
            new SpecGroup("ssd_kn", "Chuẩn kết nối", "Chuẩn kết nối",
                new SpecOption("sata", "SATA", "%SATA%"),
                // Bổ sung tùy chọn này để bắt được các ổ NVMe M.2 không ghi rõ Gen
                new SpecOption("nvme-chung", "NVMe M.2 (Tất cả Gen)", "%NVMe%", "%M.2%"),
                new SpecOption("gen3", "NVMe PCIe Gen 3", "%PCIe 3%", "%Gen 3%", "%Gen3%"),
                new SpecOption("gen4", "NVMe PCIe Gen 4", "%PCIe 4%", "%Gen 4%", "%Gen4%"),
                new SpecOption("gen5", "NVMe PCIe Gen 5", "%PCIe 5%", "%Gen 5%", "%Gen5%"))
        }
    };

    public static IReadOnlyList<SpecGroup> ForCategory(string? maDanhMuc)
    {
        if (!string.IsNullOrWhiteSpace(maDanhMuc) && ByCategory.TryGetValue(maDanhMuc.Trim(), out var groups))
            return groups;
        return Array.Empty<SpecGroup>();
    }

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
                    return group.Key + ":" + option.Value;
            }
        }
        return null;
    }

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