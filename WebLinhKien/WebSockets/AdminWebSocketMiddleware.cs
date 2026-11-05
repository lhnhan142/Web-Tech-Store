using System.Net.WebSockets;
using WebLinhKien.Helpers;

namespace WebLinhKien.WebSockets;

/// <summary>
/// Xử lý đường dẫn /ws/admin. Chỉ nhận người đã đăng nhập admin (dùng cookie đăng nhập có sẵn).
/// Phải đặt SAU UseAuthentication() để context.User đã có thông tin.
/// </summary>
public class AdminWebSocketMiddleware
{
    public const string WebSocketPath = "/ws/admin";

    private readonly RequestDelegate _next;
    private readonly AdminConnectionManager _manager;
    private readonly ILogger<AdminWebSocketMiddleware> _logger;

    public AdminWebSocketMiddleware(
        RequestDelegate next,
        AdminConnectionManager manager,
        ILogger<AdminWebSocketMiddleware> logger)
    {
        _next = next;
        _manager = manager;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.Equals(WebSocketPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true ||
            !(context.User.IsInRole(AuthServiceExtensions.AdminRole) || context.User.IsInRole(AuthServiceExtensions.NhanVienRole)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Chặn trang web khác (cùng trình duyệt) mở kết nối bằng cookie của admin.
        if (!IsSameOrigin(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var id = _manager.Add(socket);
        _logger.LogInformation("Admin {Name} kết nối WebSocket. Đang mở: {Count}",
            context.User.Identity.Name, _manager.ConnectionCount);

        try
        {
            await ReceiveLoopAsync(socket, context.RequestAborted);
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        finally
        {
            _manager.Remove(id);
            _logger.LogInformation("Admin {Name} ngắt WebSocket. Đang mở: {Count}",
                context.User.Identity.Name, _manager.ConnectionCount);
        }
    }

    // Server chỉ đẩy tin xuống; vẫn phải đọc để xử lý khung đóng kết nối. Mọi dữ liệu client gửi lên bị bỏ qua.
    private static async Task ReceiveLoopAsync(WebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[1024];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", ct);
                break;
            }
        }
    }

    private static bool IsSameOrigin(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin)) return true; // client không phải trình duyệt

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && string.Equals(uri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }
}
