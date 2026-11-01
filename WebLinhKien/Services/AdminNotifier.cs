using System.Text.Json;
using WebLinhKien.WebSockets;

namespace WebLinhKien.Services;

public class AdminNotifier : IAdminNotifier
{
    // Web = camelCase: { "type": "NEW_ORDER", "maDon": "...", ... }
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AdminConnectionManager _manager;
    private readonly ILogger<AdminNotifier> _logger;

    public AdminNotifier(AdminConnectionManager manager, ILogger<AdminNotifier> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public Task NotifyNewOrderAsync(string maDon, string tenKhachHang, decimal tongTien) =>
        SendAsync(new { type = "NEW_ORDER", maDon, tenKhachHang, tongTien });

    public Task NotifyLowStockAsync(int uidSanPham, string tenSanPham, int tonKho) =>
        SendAsync(new { type = "LOW_STOCK", uidSanPham, tenSanPham, tonKho });

    private async Task SendAsync(object message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);
            await _manager.BroadcastAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không gửi được thông báo tới trang quản trị.");
        }
    }
}
