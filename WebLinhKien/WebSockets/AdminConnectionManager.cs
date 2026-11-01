using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace WebLinhKien.WebSockets;

/// <summary>
/// Giữ danh sách kết nối WebSocket của các trang admin đang mở (Singleton) và gửi tin tới tất cả.
/// </summary>
public class AdminConnectionManager
{
    // Một WebSocket chỉ cho phép MỘT lệnh gửi tại một thời điểm, nên mỗi kết nối có một khóa gửi riêng.
    private sealed class Connection
    {
        public WebSocket Socket { get; }
        public SemaphoreSlim SendLock { get; } = new(1, 1);
        public Connection(WebSocket socket) => Socket = socket;
    }

    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(3);

    private readonly ConcurrentDictionary<Guid, Connection> _connections = new();
    private readonly ILogger<AdminConnectionManager> _logger;

    public AdminConnectionManager(ILogger<AdminConnectionManager> logger) => _logger = logger;

    public int ConnectionCount => _connections.Count;

    public Guid Add(WebSocket socket)
    {
        var id = Guid.NewGuid();
        _connections[id] = new Connection(socket);
        return id;
    }

    public void Remove(Guid id) => _connections.TryRemove(id, out _);

    /// <summary>Gửi một tin văn bản (JSON) tới mọi kết nối đang mở. Kết nối lỗi sẽ bị loại bỏ.</summary>
    public async Task BroadcastAsync(string json)
    {
        if (_connections.IsEmpty) return;

        var bytes = Encoding.UTF8.GetBytes(json);
        var tasks = _connections.Select(kv => SendAsync(kv.Key, kv.Value, bytes)).ToList();
        await Task.WhenAll(tasks);
    }

    private async Task SendAsync(Guid id, Connection connection, byte[] bytes)
    {
        if (connection.Socket.State != WebSocketState.Open)
        {
            Remove(id);
            return;
        }

        // Giới hạn 3 giây để một trình duyệt treo không làm chậm cả luồng đặt hàng.
        using var cts = new CancellationTokenSource(SendTimeout);
        try
        {
            await connection.SendLock.WaitAsync(cts.Token);
            try
            {
                await connection.Socket.SendAsync(bytes, WebSocketMessageType.Text, true, cts.Token);
            }
            finally
            {
                connection.SendLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gửi thông báo tới kết nối admin {ConnectionId} thất bại, bỏ kết nối này.", id);
            Remove(id);
            try { connection.Socket.Abort(); } catch { /* đã đóng */ }
        }
    }
}
