namespace WebLinhKien.Models.Chat;

public class ChatTurn
{
    public string Role { get; set; } = "user";   // "user" | "assistant"
    public string Text { get; set; } = string.Empty;
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatTurn> History { get; set; } = new();
}

public class ChatProductCard
{
    public int Uid { get; set; }
    public string MaSanPham { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string Gia { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Anh { get; set; }
}

public class ChatReply
{
    public string Answer { get; set; } = string.Empty;
    public List<ChatProductCard> Products { get; set; } = new();
}

/// <summary>Cấu hình chatbot, đọc từ mục "Chatbot" (khóa API đặt trong user-secrets, KHÔNG commit).</summary>
public class ChatbotOptions
{
    public string? ApiKey { get; set; }
    public string Endpoint { get; set; } = "https://api.anthropic.com/v1/messages";
    public string Model { get; set; } = "claude-haiku-5-5";
    public int MaxTokens { get; set; } = 600;
}
