using System.Text.RegularExpressions;
using WebLinhKien.Models.Chat;

namespace WebLinhKien.Services.ChatLogic;

public enum FlowKind { Menu, AskPurpose, AskBudget, BuildPc, AskCategory, AskPrice, SearchParts, FreeForm }

public sealed class FlowDecision
{
    public FlowKind Kind { get; init; }
    public ChatState State { get; init; } = new();
    public string[] Keywords { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Luồng hỏi đáp từng bước: chọn ghép cấu hình PC hoặc tìm linh kiện, rồi hỏi mục đích, mức giá, hãng.
/// Chỉ quyết định bước kế tiếp và soạn câu hỏi/nút bấm; việc truy vấn database do ChatService làm.
/// </summary>
public static class ChatFlow
{
    public static readonly Dictionary<string, (string Ten, decimal[] Bac)> LoaiMeta = new()
    {
        ["CPU"] = ("CPU", new[] { 3_000_000m, 6_000_000m, 10_000_000m }),
        ["MAINBOARD"] = ("mainboard", new[] { 2_000_000m, 4_000_000m, 7_000_000m }),
        ["RAM"] = ("RAM", new[] { 1_500_000m, 3_000_000m, 6_000_000m }),
        ["VGA"] = ("card đồ họa (VGA)", new[] { 5_000_000m, 10_000_000m, 20_000_000m }),
        ["SSD"] = ("ổ cứng SSD", new[] { 1_000_000m, 2_000_000m, 4_000_000m }),
        ["PSU"] = ("nguồn máy tính (PSU)", new[] { 1_000_000m, 2_000_000m, 3_000_000m }),
        ["CASE"] = ("vỏ case", new[] { 1_000_000m, 2_000_000m, 3_000_000m }),
        ["TANNHIET"] = ("tản nhiệt", new[] { 500_000m, 1_500_000m, 3_000_000m }),
        ["PHUKIEN"] = ("phụ kiện", new[] { 500_000m, 1_000_000m, 3_000_000m })
    };

    private static readonly string[] Purposes = { "gaming", "vanphong", "dohoa" };
    private const decimal MaxBudget = 500_000_000m;

    public static string PurposeLabel(string? p) => p switch
    {
        "gaming" => "gaming",
        "dohoa" => "đồ họa",
        _ => "văn phòng"
    };

    // ---------- Kiểm tra trạng thái do trình duyệt gửi lên ----------

    public static ChatState Sanitize(ChatState? s, IReadOnlyList<BrandRef> brands)
    {
        var r = new ChatState();
        if (s is null) return r;
        r.Mode = s.Mode is "pc" or "part" ? s.Mode : null;
        r.Purpose = s.Purpose is not null && Purposes.Contains(s.Purpose) ? s.Purpose : null;
        r.Budget = s.Budget is > 0 ? Math.Min(s.Budget.Value, MaxBudget) : null;
        r.CpuBrand = s.CpuBrand is not null && (s.CpuBrand.Equals("INTEL", StringComparison.OrdinalIgnoreCase) || s.CpuBrand.Equals("AMD", StringComparison.OrdinalIgnoreCase))
            ? s.CpuBrand.ToUpperInvariant() : null;
        r.Loai = s.Loai is not null && LoaiMeta.ContainsKey(s.Loai) ? s.Loai : null;
        r.MinPrice = s.MinPrice is >= 0 ? Math.Min(s.MinPrice.Value, MaxBudget) : null;
        r.MaxPrice = s.MaxPrice is > 0 ? Math.Min(s.MaxPrice.Value, MaxBudget) : null;
        r.PriceAny = s.PriceAny;
        r.Brand = s.Brand is not null && brands.Any(b => b.Ma.Equals(s.Brand, StringComparison.OrdinalIgnoreCase))
            ? brands.First(b => b.Ma.Equals(s.Brand, StringComparison.OrdinalIgnoreCase)).Ma : null;
        return r;
    }

    // ---------- Quyết định bước tiếp theo ----------

    public static FlowDecision Decide(ChatState? incoming, string? message, IReadOnlyList<BrandRef> brands)
    {
        var st = Sanitize(incoming, brands);
        var norm = ChatText.Normalize(message);
        if (ChatText.WantsReset(norm)) return new FlowDecision { Kind = FlowKind.Menu, State = new ChatState() };

        var purpose = ChatText.DetectPurpose(norm);
        var money = ChatText.ParseMoney(message);
        var cat = ChatText.DetectCategory(norm);
        var brand = ChatText.DetectBrand(norm, brands);
        var changeBudget = ChatText.WantsChangeBudget(norm);
        var changePurpose = ChatText.WantsChangePurpose(norm);
        var otherBrand = ChatText.WantsOtherBrand(norm);
        var anyPrice = ChatText.WantsAnyPrice(norm);
        var keywords = ChatText.Keywords(message);

        // Đang chờ khách trả lời giá: gõ trơn "12" thì hiểu là 12 triệu
        if (money is null && st.Mode is not null)
        {
            var bare = Regex.Match(message ?? "", @"^\s*(\d{1,3}(?:[.,]\d)?)\s*$");
            if (bare.Success)
            {
                var v = decimal.Parse(bare.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture) * 1_000_000m;
                money = new ChatText.MoneyQuery(null, v, v);
            }
        }

        string? mode = null;
        if (ChatText.StrongPc(norm)) mode = "pc";
        else if (cat is not null || ChatText.WantsParts(norm)) mode = "part";
        else if (purpose is not null || ChatText.WeakPc(norm)) mode = st.Mode == "part" && purpose is null ? "part" : "pc";
        else if (st.Mode is not null && (money is not null || brand is not null || changeBudget || changePurpose || otherBrand || anyPrice)) mode = st.Mode;
        else if (st.Mode == "part" && st.Loai is not null && keywords.Length > 0) mode = "part";

        if (mode is null) return new FlowDecision { Kind = FlowKind.FreeForm, State = st, Keywords = keywords };

        // Đổi chủ đề (từ ghép PC sang tìm RAM...) thì bỏ các điều kiện cũ
        if (mode != st.Mode) st = new ChatState { Mode = mode };

        if (mode == "pc")
        {
            if (changePurpose) st.Purpose = null;
            if (changeBudget) st.Budget = null;
            if (purpose is not null) st.Purpose = purpose;
            if (money is not null) st.Budget = Math.Min(money.Target, MaxBudget);
            if (brand is not null && (brand.Equals("INTEL", StringComparison.OrdinalIgnoreCase) || brand.Equals("AMD", StringComparison.OrdinalIgnoreCase)))
                st.CpuBrand = brand.ToUpperInvariant();

            var kind = st.Purpose is null ? FlowKind.AskPurpose : st.Budget is null ? FlowKind.AskBudget : FlowKind.BuildPc;
            return new FlowDecision { Kind = kind, State = st };
        }

        // mode == "part"
        if (otherBrand) st.Brand = null;
        if (changeBudget) { st.MinPrice = null; st.MaxPrice = null; st.PriceAny = false; }
        if (cat is not null && cat != st.Loai) st = new ChatState { Mode = "part", Loai = cat };   // đổi loại: bỏ giá và hãng cũ
        if (anyPrice) { st.PriceAny = true; st.MinPrice = null; st.MaxPrice = null; }
        if (money is not null) { st.MinPrice = money.Min; st.MaxPrice = money.Max; st.PriceAny = false; }
        if (brand is not null) st.Brand = brand;

        FlowKind next;
        if (st.Loai is null) next = FlowKind.AskCategory;
        else if (!st.PriceAny && st.MinPrice is null && st.MaxPrice is null) next = FlowKind.AskPrice;
        else next = FlowKind.SearchParts;
        return new FlowDecision { Kind = next, State = st, Keywords = keywords };
    }

    // ---------- Câu hỏi và nút bấm ----------

    private static ChatChoice C(string label, string? text = null) => new() { Label = label, Text = text ?? label };

    public static ChatReply MenuReply() => new()
    {
        Answer = "Chào bạn! Mình có thể giúp bạn:\n• Ghép cấu hình PC đầy đủ theo mức giá và nhu cầu (văn phòng, gaming...)\n• Tìm linh kiện (CPU, RAM, VGA...) theo mức giá và hãng\nBạn muốn tư vấn gì?",
        Choices = { C("Tư vấn cấu hình PC"), C("Tìm linh kiện theo giá và hãng") },
        State = new ChatState()
    };

    public static ChatReply AskPurposeReply(ChatState st) => new()
    {
        Answer = "Bạn định dùng máy để làm gì ạ?",
        Choices =
        {
            C("Văn phòng, học tập", "Máy văn phòng, học tập"),
            C("Gaming", "Gaming chơi game"),
            C("Đồ họa, dựng phim", "Đồ họa, dựng phim")
        },
        State = st
    };

    public static ChatReply AskBudgetReply(ChatState st)
    {
        var budgets = st.Purpose switch
        {
            "gaming" => new[] { 10, 15, 20, 30, 40 },
            "dohoa" => new[] { 15, 20, 30, 40, 50 },
            _ => new[] { 6, 8, 10, 12, 15 }
        };
        var reply = new ChatReply
        {
            Answer = $"Bạn dự định chi khoảng bao nhiêu cho máy {PurposeLabel(st.Purpose)}? Bạn bấm một mức bên dưới hoặc tự gõ (ví dụ \"12 triệu\").",
            State = st
        };
        foreach (var b in budgets) reply.Choices.Add(C($"Khoảng {b} triệu"));
        return reply;
    }

    public static ChatReply AskCategoryReply(ChatState st) => new()
    {
        Answer = "Bạn muốn tìm linh kiện nào ạ?",
        Choices =
        {
            C("CPU"), C("Mainboard"), C("RAM"), C("Card đồ họa (VGA)", "Card đồ họa VGA"), C("Ổ cứng SSD"),
            C("Nguồn (PSU)", "Nguồn máy tính PSU"), C("Vỏ case"), C("Tản nhiệt"), C("Phụ kiện")
        },
        State = st
    };

    public static ChatReply AskPriceReply(ChatState st, string? brandName)
    {
        var meta = LoaiMeta[st.Loai!];
        var b = meta.Bac;
        var hang = string.IsNullOrEmpty(brandName) ? "" : $" hãng {brandName}";
        var reply = new ChatReply
        {
            Answer = $"Bạn muốn tìm {meta.Ten}{hang} trong tầm giá nào? Bạn bấm một mức bên dưới hoặc tự gõ khoảng giá (ví dụ \"từ 2 đến 4 triệu\"). Gõ tên hãng nếu muốn lọc theo hãng.",
            State = st
        };
        reply.Choices.Add(C($"Dưới {ChatText.FormatMoney(b[0])}"));
        reply.Choices.Add(C($"Từ {ChatText.FormatMoney(b[0])} đến {ChatText.FormatMoney(b[1])}"));
        reply.Choices.Add(C($"Từ {ChatText.FormatMoney(b[1])} đến {ChatText.FormatMoney(b[2])}"));
        reply.Choices.Add(C($"Trên {ChatText.FormatMoney(b[2])}"));
        reply.Choices.Add(C("Mức giá bất kỳ"));
        return reply;
    }
}
