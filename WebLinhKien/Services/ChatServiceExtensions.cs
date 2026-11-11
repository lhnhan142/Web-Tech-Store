using System.Threading.RateLimiting;
using WebLinhKien.Models.Chat;

namespace WebLinhKien.Services;

public static class ChatServiceExtensions
{
    /// <summary>
    /// Đăng ký chatbot. Trong Program.cs chỉ cần: builder.Services.AddChatbot(builder.Configuration);
    /// Khóa API: dotnet user-secrets set "Chatbot:ApiKey" "..."  (không đưa vào appsettings, không commit).
    /// </summary>
    public static IServiceCollection AddChatbot(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ChatbotOptions>(config.GetSection("Chatbot"));
        services.AddHttpClient<IChatService, ChatService>(c => c.Timeout = TimeSpan.FromSeconds(25));

        // Giới hạn 8 câu hỏi mỗi phút cho mỗi IP để tránh lạm dụng khóa API
        services.AddRateLimiter(o => o.AddPolicy("chat", ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1) })));
        return services;
    }
}
