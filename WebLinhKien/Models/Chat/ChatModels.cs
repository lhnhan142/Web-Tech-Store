namespace WebLinhKien.Models.Chat;

public class ChatTurn
{
    public string Role { get; set; } = "user";   // "user" | "assistant"
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Trạng thái cuộc tư vấn, do trình duyệt giữ và gửi lại mỗi lần hỏi (server không lưu phiên).
/// Mọi giá trị đều được kiểm tra lại ở server (ChatFlow.Sanitize) vì có thể bị sửa tay.
/// </summary>
public class ChatState
{
    public string? Mode { get; set; }          // "pc" = ghép cấu hình, "part" = tìm linh kiện
    public string? Purpose { get; set; }       // pc: "gaming" | "vanphong" | "dohoa"
    public decimal? Budget { get; set; }       // pc: ngân sách tối đa (đồng)
    public string? CpuBrand { get; set; }      // pc: "INTEL" | "AMD" nếu khách chỉ định
    public string? Loai { get; set; }          // part: mã danh mục (CPU, RAM, ...)
    public decimal? MinPrice { get; set; }     // part: giá từ
    public decimal? MaxPrice { get; set; }     // part: giá đến
    public bool PriceAny { get; set; }         // part: khách chọn "mức giá bất kỳ"
    public string? Brand { get; set; }         // part: mã thương hiệu
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatTurn> History { get; set; } = new();
    public ChatState? State { get; set; }
}

public class ChatProductCard
{
    public int Uid { get; set; }
    public string MaSanPham { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string Gia { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Anh { get; set; }
    public string? Hang { get; set; }
}

/// <summary>Nút gợi ý dưới tin nhắn của bot. Label hiện trên nút, Text là câu gửi đi khi bấm.</summary>
public class ChatChoice
{
    public string Label { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ChatBuildItem
{
    public string Role { get; set; } = string.Empty;     // CPU, MAINBOARD, ...
    public string Label { get; set; } = string.Empty;    // tên hiển thị của vai trò
    public ChatProductCard Card { get; set; } = new();
}

public class ChatBuild
{
    public string Title { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public string Budget { get; set; } = string.Empty;
    public string Remaining { get; set; } = string.Empty;
    public List<ChatBuildItem> Items { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public class ChatReply
{
    public string Answer { get; set; } = string.Empty;
    public List<ChatProductCard> Products { get; set; } = new();
    public List<ChatChoice> Choices { get; set; } = new();
    public ChatBuild? Build { get; set; }
    public ChatState? State { get; set; }
}

/// <summary>Danh sách sản phẩm (Uid) của một cấu hình để thêm cả bộ vào giỏ hàng.</summary>
public class ChatCartRequest
{
    public List<int> Uids { get; set; } = new();
}

/// <summary>Cấu hình chatbot, đọc từ mục "Chatbot" (khóa API đặt trong user-secrets, KHÔNG commit).</summary>
public class ChatbotOptions
{
    public string? ApiKey { get; set; }
    public string Endpoint { get; set; } = "https://api.anthropic.com/v1/messages";
    public string Model { get; set; } = "claude-haiku-5-5";
    public int MaxTokens { get; set; } = 600;
}
