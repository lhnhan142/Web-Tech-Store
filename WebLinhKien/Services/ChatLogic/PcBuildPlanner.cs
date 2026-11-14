using System.Text.RegularExpressions;

namespace WebLinhKien.Services.ChatLogic;

public sealed class PartInfo
{
    public int Uid { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string Loai { get; set; } = "";          // CPU, MAINBOARD, RAM, VGA, SSD, PSU, CASE, TANNHIET
    public string? Hang { get; set; }               // mã thương hiệu
    public decimal Gia { get; set; }
    public int Ton { get; set; }
    public string? Anh { get; set; }
    public Dictionary<string, string> Specs { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class PcPick
{
    public string Role { get; set; } = "";
    public PartInfo Part { get; set; } = null!;
}

public sealed class PcPlan
{
    public bool Feasible { get; set; }
    public string Purpose { get; set; } = "";
    public decimal Budget { get; set; }
    public decimal Total { get; set; }
    // Khi Feasible = false: Items/Total là cấu hình RẺ NHẤT hiện ghép được (vượt ngân sách), để khách tham khảo.
    public decimal MinimumIgpu { get; set; }         // gaming/đồ họa: cấu hình rẻ nhất dùng đồ họa tích hợp (0 nếu không có)
    public List<PcPick> Items { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

/// <summary>
/// Ghép cấu hình PC theo ngân sách và mục đích từ sản phẩm CÒN HÀNG. Mỗi linh kiện được chọn trong phần ngân sách
/// của nó, rồi kiểm tra tương thích (socket CPU với main, loại RAM, kích thước case, độ dài card, công suất nguồn,
/// socket tản nhiệt). Tổng tiền không bao giờ vượt ngân sách; phần dư được dùng để nâng cấp tiếp.
/// Chạy hoàn toàn trong bộ nhớ, không gọi database.
/// </summary>
public static class PcBuildPlanner
{
    public static readonly string[] DisplayOrder = { "CPU", "MAINBOARD", "RAM", "VGA", "SSD", "PSU", "CASE", "TANNHIET" };
    private static readonly string[] BuildOrder = { "CPU", "MAINBOARD", "RAM", "VGA", "CASE", "PSU", "SSD", "TANNHIET" };
    private static readonly double[] Factors = { 1.0, 0.97, 0.94, 0.91, 0.88, 0.85, 0.80, 0.75, 0.70, 0.65, 0.60, 0.55, 0.50 };

    public static string RoleLabel(string role) => role switch
    {
        "CPU" => "CPU",
        "MAINBOARD" => "Mainboard",
        "RAM" => "RAM",
        "VGA" => "Card đồ họa (VGA)",
        "SSD" => "Ổ cứng SSD",
        "PSU" => "Nguồn (PSU)",
        "CASE" => "Vỏ case",
        "TANNHIET" => "Tản nhiệt",
        _ => role
    };

    // ---------- Thông số đã đọc ----------

    private sealed class Props
    {
        public string Socket = "";
        public int Tdp = 65;
        public string RamType = "";
        public string Form = "";
        public int RamGb;
        public int LenMm;
        public int PowerW;
        public int RecPsu;
        public int PsuW;
        public HashSet<string> CaseForms = new();
        public int CaseMaxVga;
        public int SsdGb;
        public bool Nvme;
        public string CoolerSockets = "";
        public bool HasIgpu = true;
    }

    private sealed class Ctx
    {
        public List<PartInfo> Parts = new();
        public Dictionary<PartInfo, Props> Info = new(ReferenceEqualityComparer.Instance);
        public string Purpose = "";
        public string? CpuBrand;
        public bool Relax;      // nới yêu cầu tối thiểu: RAM 8GB, SSD 240GB (khi ngân sách chật)
        public Props P(PartInfo p) => Info[p];
        public IEnumerable<PartInfo> Pool(string role) => Parts.Where(p => p.Loai == role);
    }

    private static string? Spec(PartInfo p, params string[] keys)
    {
        foreach (var key in keys)
            foreach (var kv in p.Specs)
            {
                var n = ChatText.Fold(kv.Key);
                if (n == key || n.StartsWith(key + " ") || n.StartsWith(key + "(")) return kv.Value;
            }
        return null;
    }

    private static int? IntOf(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var m = Regex.Match(s, @"\d{1,3}(?:\.\d{3})+|\d+");
        return m.Success ? int.Parse(m.Value.Replace(".", "")) : null;
    }

    private static int GbOf(string? spec, string name)
    {
        foreach (var text in new[] { spec, name })
        {
            if (string.IsNullOrWhiteSpace(text)) continue;
            var m = Regex.Match(ChatText.Fold(text), @"(\d+(?:[.,]\d+)?)\s*(tb|gb)");
            if (m.Success)
            {
                var v = double.Parse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                return (int)(m.Groups[2].Value == "tb" ? v * 1000 : v);
            }
        }
        return 0;
    }

    private static string SocketOf(string? spec, string name)
    {
        foreach (var text in new[] { spec, name })
        {
            if (string.IsNullOrWhiteSpace(text)) continue;
            var m = Regex.Match(ChatText.Fold(text), @"\b(am[345]|lga\s?\d{3,4})\b");
            if (m.Success) return m.Value.Replace(" ", "").ToUpperInvariant();
        }
        return "";
    }

    private static string TypeOf(string? spec, string name)
    {
        foreach (var text in new[] { spec, name })
        {
            if (string.IsNullOrWhiteSpace(text)) continue;
            var m = Regex.Match(ChatText.Fold(text), @"ddr[345]");
            if (m.Success) return m.Value.ToUpperInvariant();
        }
        return "";
    }

    private static HashSet<string> FormsOf(string? s)
    {
        var set = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(s)) return set;
        foreach (var raw in Regex.Split(ChatText.Fold(s), @"[,/;+]|\bva\b|\band\b"))
        {
            var t = raw.Trim();
            if (t.Length == 0) continue;
            if (t.Contains("itx")) set.Add("ITX");
            else if (t.Contains("micro") || t.Contains("matx") || t.Contains("m-atx")) set.Add("MATX");
            else if (t.Contains("e-atx") || t.Contains("eatx")) set.Add("EATX");
            else if (t.Contains("atx")) set.Add("ATX");
        }
        return set;
    }

    /// <summary>CPU có đồ họa tích hợp không: đuôi F là không; AM4 chỉ các mã có chữ G (5600G...); còn lại có.</summary>
    private static bool HasIgpu(string name, string socket)
    {
        var n = ChatText.Fold(name);
        var letters = Regex.Matches(n, @"\b\d{3,5}([a-z]+)\b").Select(m => m.Groups[1].Value).ToList();
        if (letters.Any(l => l.Contains('f'))) return false;
        if (socket == "AM4") return letters.Any(l => l.Contains('g'));
        return true;
    }

    private static Props Parse(PartInfo p)
    {
        var pr = new Props();
        switch (p.Loai)
        {
            case "CPU":
                pr.Socket = SocketOf(Spec(p, "socket"), p.Ten);
                pr.Tdp = IntOf(Spec(p, "tdp")) ?? 65;
                pr.HasIgpu = HasIgpu(p.Ten, pr.Socket);
                break;
            case "MAINBOARD":
                pr.Socket = SocketOf(Spec(p, "socket"), p.Ten);
                pr.RamType = TypeOf(Spec(p, "loai ram"), p.Ten);
                pr.Form = FormsOf(Spec(p, "form factor", "kich thuoc")).FirstOrDefault() ?? "";
                break;
            case "RAM":
                pr.RamType = TypeOf(Spec(p, "loai ram"), p.Ten);
                pr.RamGb = GbOf(Spec(p, "dung luong"), p.Ten);
                break;
            case "VGA":
                pr.LenMm = IntOf(Spec(p, "chieu dai")) ?? 0;
                pr.PowerW = IntOf(Spec(p, "cong suat tieu thu")) ?? 0;
                pr.RecPsu = IntOf(Spec(p, "nguon de nghi")) ?? 0;
                if (pr.PowerW == 0) pr.PowerW = pr.RecPsu > 0 ? (int)(pr.RecPsu * 0.55) : 150;
                break;
            case "PSU":
                pr.PsuW = IntOf(Spec(p, "cong suat")) ?? 0;
                if (pr.PsuW == 0)
                {
                    var m = Regex.Match(p.Ten, @"(\d{3,4})\s*W\b", RegexOptions.IgnoreCase);
                    if (m.Success) pr.PsuW = int.Parse(m.Groups[1].Value);
                }
                break;
            case "CASE":
                pr.CaseForms = FormsOf(Spec(p, "form ho tro") ?? Spec(p, "ho tro mainboard"));
                pr.CaseMaxVga = IntOf(Spec(p, "chieu dai vga toi da")) ?? 0;
                break;
            case "SSD":
                pr.SsdGb = GbOf(Spec(p, "dung luong"), p.Ten);
                pr.Nvme = ChatText.Fold(Spec(p, "chuan ket noi") ?? "").Contains("nvme") || ChatText.Fold(p.Ten).Contains("nvme");
                break;
            case "TANNHIET":
                pr.CoolerSockets = ChatText.Fold(Spec(p, "socket ho tro") ?? "").Replace(" ", "").ToUpperInvariant();
                break;
        }
        return pr;
    }

    // ---------- Kiểm tra tương thích ----------

    private static bool FormOk(string mainForm, HashSet<string> caseForms)
    {
        if (mainForm.Length == 0 || caseForms.Count == 0) return true;
        if (caseForms.Contains(mainForm)) return true;
        if (mainForm == "MATX" && caseForms.Contains("ATX")) return true;
        if (mainForm == "ITX" && (caseForms.Contains("ATX") || caseForms.Contains("MATX"))) return true;
        return false;
    }

    private static bool CaseFits(Props main, Props? vga, Props cs) =>
        FormOk(main.Form, cs.CaseForms) && (vga is null || vga.LenMm == 0 || cs.CaseMaxVga == 0 || vga.LenMm <= cs.CaseMaxVga);

    private static int NeededWatts(Props cpu, Props? vga)
    {
        var need = (int)(Math.Ceiling((cpu.Tdp + (vga?.PowerW ?? 0) + 100) * 1.3 / 50.0) * 50);
        return Math.Max(Math.Max(need, vga?.RecPsu ?? 0), 350);
    }

    private static bool Validate(Ctx c, List<PcPick> items)
    {
        PartInfo? Get(string role) => items.FirstOrDefault(i => i.Role == role)?.Part;
        var cpu = Get("CPU"); var main = Get("MAINBOARD"); var ram = Get("RAM"); var vga = Get("VGA");
        var cs = Get("CASE"); var psu = Get("PSU"); var cool = Get("TANNHIET");
        if (cpu is null || main is null || ram is null || cs is null || psu is null) return false;
        var pc = c.P(cpu); var pm = c.P(main);
        if (pc.Socket.Length > 0 && pm.Socket.Length > 0 && pc.Socket != pm.Socket) return false;
        var pr = c.P(ram);
        if (pr.RamType.Length > 0 && pm.RamType.Length > 0 && pr.RamType != pm.RamType) return false;
        var pv = vga is null ? null : c.P(vga);
        if (!CaseFits(pm, pv, c.P(cs))) return false;
        if (c.P(psu).PsuW > 0 && c.P(psu).PsuW < NeededWatts(pc, pv)) return false;
        if (cool is not null)
        {
            var sockets = c.P(cool).CoolerSockets;
            if (sockets.Length > 0 && pc.Socket.Length > 0 && !sockets.Contains(pc.Socket)) return false;
        }
        return true;
    }

    // ---------- Chọn linh kiện ----------

    private static Dictionary<string, double> Shares(string purpose, bool withVga)
    {
        if (!withVga)
            return new() { ["CPU"] = .28, ["MAINBOARD"] = .16, ["RAM"] = .14, ["CASE"] = .10, ["PSU"] = .09, ["SSD"] = .15, ["TANNHIET"] = .08 };
        return purpose == "dohoa"
            ? new() { ["CPU"] = .25, ["MAINBOARD"] = .12, ["RAM"] = .12, ["VGA"] = .25, ["CASE"] = .05, ["PSU"] = .07, ["SSD"] = .10, ["TANNHIET"] = .04 }
            : new() { ["CPU"] = .20, ["MAINBOARD"] = .11, ["RAM"] = .09, ["VGA"] = .35, ["CASE"] = .05, ["PSU"] = .07, ["SSD"] = .08, ["TANNHIET"] = .05 };
    }

    /// <summary>Món đắt nhất không vượt cap trước, rồi mới đến các món đắt hơn cap (rẻ nhất trước).</summary>
    private static IEnumerable<PartInfo> Ranked(IEnumerable<PartInfo> cands, decimal cap)
    {
        var list = cands.ToList();
        return list.Where(p => p.Gia <= cap).OrderByDescending(p => p.Gia).ThenBy(p => p.Uid)
            .Concat(list.Where(p => p.Gia > cap).OrderBy(p => p.Gia).ThenBy(p => p.Uid));
    }

    private static decimal Cap(decimal rem, Dictionary<string, double> shares, List<string> left, string role)
    {
        var sum = left.Sum(r => shares.TryGetValue(r, out var s) ? s : 0);
        return sum <= 0 ? 0 : Math.Max(0, rem) * (decimal)(shares[role] / sum);
    }

    private static List<PartInfo> CpuPool(Ctx c, bool withVga)
    {
        var cpuPool = c.Pool("CPU").Where(p => c.CpuBrand is null || string.Equals(p.Hang, c.CpuBrand, StringComparison.OrdinalIgnoreCase)).ToList();
        if (cpuPool.Count == 0) cpuPool = c.Pool("CPU").ToList();
        if (!withVga)
        {
            var integrated = cpuPool.Where(p => c.P(p).HasIgpu).ToList();
            if (integrated.Count > 0) cpuPool = integrated;
        }
        return cpuPool;
    }

    /// <summary>Lần lượt các cấu hình hợp lệ ứng với từng CPU (tối đa 8 CPU, từ CPU hợp ngân sách nhất); chưa kiểm tra tổng tiền.</summary>
    private static IEnumerable<List<PcPick>> Greedy(Ctx c, decimal eff, bool withVga)
    {
        var shares = Shares(c.Purpose, withVga);
        var allRoles = BuildOrder.Where(r => shares.ContainsKey(r)).ToList();
        var cpuCap = Cap(eff, shares, allRoles, "CPU");
        foreach (var cpu in Ranked(CpuPool(c, withVga), cpuCap).Take(8))
        {
            var items = Chain(c, eff, withVga, shares, allRoles, cpu);
            if (items is not null) yield return items;
        }
    }

    /// <summary>Cấu hình rẻ nhất ghép được (thử mọi CPU, mọi linh kiện lấy món rẻ nhất tương thích). null nếu không ghép được.</summary>
    private static List<PcPick>? MinChain(Ctx c, bool withVga)
    {
        var shares = Shares(c.Purpose, withVga);
        var allRoles = BuildOrder.Where(r => shares.ContainsKey(r)).ToList();
        List<PcPick>? best = null;
        decimal bestTotal = 0;
        foreach (var cpu in CpuPool(c, withVga))
        {
            var items = Chain(c, 0m, withVga, shares, allRoles, cpu);
            if (items is null) continue;
            var total = items.Sum(i => i.Part.Gia);
            if (best is null || total < bestTotal) { best = items; bestTotal = total; }
        }
        return best;
    }

    private static List<PcPick>? Chain(Ctx c, decimal eff, bool withVga, Dictionary<string, double> shares, List<string> allRoles, PartInfo cpu)
    {
        var pc = c.P(cpu);
        var left = allRoles.Where(r => r != "CPU").ToList();
        var rem = eff - cpu.Gia;

        // Mainboard cùng socket
        var mains = c.Pool("MAINBOARD").Where(p => pc.Socket.Length == 0 || c.P(p).Socket.Length == 0 || c.P(p).Socket == pc.Socket).ToList();
        var main = Ranked(mains, Cap(rem, shares, left, "MAINBOARD")).FirstOrDefault();
        if (main is null) return null;
        var pm = c.P(main);
        rem -= main.Gia; left.Remove("MAINBOARD");

        // RAM cùng loại với main, dung lượng tối thiểu theo mục đích (nới lỏng nếu không có)
        var minRam = c.Relax ? 8 : c.Purpose switch { "gaming" => 16, "dohoa" => 16, _ => 8 };
        var rams = c.Pool("RAM").Where(p => pm.RamType.Length == 0 || c.P(p).RamType.Length == 0 || c.P(p).RamType == pm.RamType).ToList();
        var ramsMin = rams.Where(p => c.P(p).RamGb >= minRam).ToList();
        var ram = Ranked(ramsMin.Count > 0 ? ramsMin : rams, Cap(rem, shares, left, "RAM")).FirstOrDefault();
        if (ram is null) return null;
        rem -= ram.Gia; left.Remove("RAM");

        // Card đồ họa (thử lần lượt từ món mạnh nhất trong ngân sách) rồi case vừa card, nguồn đủ công suất
        var vgaOptions = withVga ? Ranked(c.Pool("VGA"), Cap(rem, shares, left, "VGA")).Take(8).Cast<PartInfo?>().ToList() : new List<PartInfo?> { null };
        foreach (var vga in vgaOptions)
        {
            var rem2 = rem; var left2 = left.ToList();
            if (vga is not null) { rem2 -= vga.Gia; left2.Remove("VGA"); }
            var pv = vga is null ? null : c.P(vga);

            var cases = c.Pool("CASE").Where(p => CaseFits(pm, pv, c.P(p))).ToList();
            var cs = Ranked(cases, Cap(rem2, shares, left2, "CASE")).FirstOrDefault();
            if (cs is null) continue;
            rem2 -= cs.Gia; left2.Remove("CASE");

            var need = NeededWatts(pc, pv);
            var psus = c.Pool("PSU").Where(p => c.P(p).PsuW == 0 || c.P(p).PsuW >= need).ToList();
            var psu = Ranked(psus, Cap(rem2, shares, left2, "PSU")).FirstOrDefault();
            if (psu is null) continue;
            rem2 -= psu.Gia; left2.Remove("PSU");

            var minSsd = c.Relax ? 240 : c.Purpose switch { "gaming" => 500, "dohoa" => 500, _ => 240 };
            var ssds = c.Pool("SSD").ToList();
            var ssdsMin = ssds.Where(p => c.P(p).SsdGb >= minSsd).ToList();
            var ssdPool = ssdsMin.Count > 0 ? ssdsMin : ssds;
            var nvme = ssdPool.Where(p => c.P(p).Nvme).ToList();
            var ssdCap = Cap(rem2, shares, left2, "SSD");
            var ssd = Ranked(nvme.Count > 0 && nvme.Any(p => p.Gia <= ssdCap) ? nvme : ssdPool, ssdCap).FirstOrDefault();
            if (ssd is null) continue;
            rem2 -= ssd.Gia; left2.Remove("SSD");

            var items = new List<PcPick> { new() { Role = "CPU", Part = cpu }, new() { Role = "MAINBOARD", Part = main }, new() { Role = "RAM", Part = ram } };
            if (vga is not null) items.Add(new PcPick { Role = "VGA", Part = vga });
            items.Add(new PcPick { Role = "CASE", Part = cs });
            items.Add(new PcPick { Role = "PSU", Part = psu });
            items.Add(new PcPick { Role = "SSD", Part = ssd });

            // Tản nhiệt là tùy chọn: chỉ thêm nếu có món hỗ trợ socket nằm trong phần ngân sách của nó
            var coolers = c.Pool("TANNHIET").Where(p => pc.Socket.Length == 0 || c.P(p).CoolerSockets.Length == 0 || c.P(p).CoolerSockets.Contains(pc.Socket)).ToList();
            var coolCap = Cap(rem2, shares, left2, "TANNHIET");
            var cool = coolers.Where(p => p.Gia <= coolCap).OrderByDescending(p => p.Gia).ThenBy(p => p.Uid).FirstOrDefault();
            if (cool is not null) items.Add(new PcPick { Role = "TANNHIET", Part = cool });

            if (Validate(c, items)) return items;
        }
        return null;
    }

    /// <summary>Dùng phần ngân sách còn dư để nâng cấp các linh kiện không làm đổi socket/loại RAM.</summary>
    private static void Upgrade(Ctx c, List<PcPick> items, decimal budget)
    {
        var roles = new[] { "VGA", "RAM", "SSD", "TANNHIET", "CASE", "PSU" };
        for (var pass = 0; pass < 3; pass++)
        {
            var changed = false;
            foreach (var role in roles)
            {
                var total = items.Sum(i => i.Part.Gia);
                var leftover = budget - total;
                if (leftover <= 0) return;

                var current = items.FirstOrDefault(i => i.Role == role);
                if (current is null && role != "TANNHIET") continue;
                var currentPrice = current?.Part.Gia ?? 0;

                foreach (var cand in c.Pool(role).Where(p => p != current?.Part && p.Gia > currentPrice && p.Gia - currentPrice <= leftover)
                             .OrderByDescending(p => p.Gia).ThenBy(p => p.Uid))
                {
                    var trial = items.Where(i => i.Role != role).Select(i => new PcPick { Role = i.Role, Part = i.Part }).ToList();
                    trial.Add(new PcPick { Role = role, Part = cand });
                    if (!Validate(c, trial)) continue;
                    if (current is null) items.Add(new PcPick { Role = role, Part = cand });
                    else current.Part = cand;
                    changed = true;
                    break;
                }
            }
            if (!changed) return;
        }
    }

    // ---------- Điểm vào ----------

    public static PcPlan Plan(IEnumerable<PartInfo> parts, string purpose, decimal budget, string? cpuBrand = null)
    {
        var c = new Ctx { Purpose = purpose, CpuBrand = cpuBrand };
        foreach (var p in parts.Where(p => p.Ton > 0 && p.Gia > 0 && BuildOrder.Contains(p.Loai)))
        {
            c.Parts.Add(p);
            c.Info[p] = Parse(p);
        }

        var wantVga = purpose != "vanphong";
        // Thứ tự ưu tiên: card rời + RAM/SSD đủ dùng, card rời + mức cơ bản, đồ họa tích hợp + đủ dùng, tích hợp + cơ bản
        var modes = wantVga
            ? new[] { (Vga: true, Relax: false), (Vga: true, Relax: true), (Vga: false, Relax: false), (Vga: false, Relax: true) }
            : new[] { (Vga: false, Relax: false) };

        foreach (var (withVga, relax) in modes)
        {
            c.Relax = relax;
            foreach (var f in Factors)
            {
                var items = Greedy(c, budget * (decimal)f, withVga).FirstOrDefault(it => it.Sum(i => i.Part.Gia) <= budget);
                if (items is null) continue;

                Upgrade(c, items, budget);
                var plan = new PcPlan { Feasible = true, Purpose = purpose, Budget = budget, Items = Sort(items) };
                plan.Total = plan.Items.Sum(i => i.Part.Gia);
                if (wantVga && !withVga)
                    plan.Notes.Add("Ngân sách này chưa đủ để lắp card đồ họa rời nên mình chọn CPU có đồ họa tích hợp (chơi được game nhẹ). Tăng ngân sách để có card rời.");
                if (relax)
                    plan.Notes.Add("Để vừa ngân sách, RAM và SSD ở mức cơ bản (RAM 8GB trở lên, SSD từ 240GB); bạn có thể nâng cấp sau.");
                if (!withVga && !c.P(plan.Items.First(i => i.Role == "CPU").Part).HasIgpu)
                    plan.Notes.Add("CPU này không có đồ họa tích hợp, cần thêm card đồ họa để xuất hình.");
                if (plan.Items.All(i => i.Role != "TANNHIET"))
                    plan.Notes.Add("Chưa tính tản nhiệt rời trong ngân sách này; bạn có thể thêm sau.");
                return plan;
            }
        }

        // Không ghép được trong ngân sách: trả về cấu hình rẻ nhất hiện có để khách tham khảo
        c.Relax = true;
        var cheapest = (wantVga ? MinChain(c, true) : null) ?? MinChain(c, false);
        var igpu = wantVga ? MinChain(c, false) : null;
        var result = new PcPlan
        {
            Feasible = false, Purpose = purpose, Budget = budget,
            MinimumIgpu = igpu?.Sum(i => i.Part.Gia) ?? 0
        };
        if (cheapest is not null)
        {
            result.Items = Sort(cheapest);
            result.Total = result.Items.Sum(i => i.Part.Gia);
        }
        return result;
    }

    private static List<PcPick> Sort(List<PcPick> items) =>
        items.OrderBy(i => Array.IndexOf(DisplayOrder, i.Role)).ToList();
}
