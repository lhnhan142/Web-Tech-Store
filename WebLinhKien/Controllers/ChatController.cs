using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebLinhKien.Helpers;
using WebLinhKien.Models;
using WebLinhKien.Models.Chat;
using WebLinhKien.Repositories.Interfaces;
using WebLinhKien.Services;

namespace WebLinhKien.Controllers;

[Route("Chat")]
public class ChatController : Controller
{
    private const int MaxCartItems = 12;

    private readonly IChatService _chat;
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly ILogger<ChatController> _log;

    public ChatController(IChatService chat, ISanPhamRepository sanPhamRepo, ILogger<ChatController> log)
    {
        _chat = chat;
        _sanPhamRepo = sanPhamRepo;
        _log = log;
    }

    // POST /Chat/Hoi  { "message": "...", "history": [{ "role": "user", "text": "..." }] }
    [HttpPost("Hoi")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("chat")]
    public async Task<IActionResult> Hoi([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Vui lòng nhập câu hỏi." });

        try
        {
            return Json(await _chat.AskAsync(request, ct));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            // Ghi lỗi thật vào log (cửa sổ Output / console) để dễ tìm, không đưa chi tiết cho khách
            _log.LogError(ex, "Chatbot lỗi khi xử lý câu hỏi.");
            return StatusCode(500, new { error = "Chatbot đang gặp sự cố, vui lòng thử lại sau." });
        }
    }
    // POST /Chat/ThemBoVaoGio  { "uids": [12, 40, ...] }
    // Thêm cả bộ linh kiện của một cấu hình vào giỏ (mỗi món 1 cái). Giá và tồn kho luôn kiểm lại ở đây,
    // món nào đã hết hàng hoặc bị ẩn thì bỏ qua và báo lại. Bấm nhiều lần không làm tăng số lượng.
    [HttpPost("ThemBoVaoGio")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("chat")]
    public async Task<IActionResult> ThemBoVaoGio([FromBody] ChatCartRequest request)
    {
        var uids = (request?.Uids ?? new List<int>()).Where(u => u > 0).Distinct().Take(MaxCartItems).ToList();
        if (uids.Count == 0) return BadRequest(new { success = false, message = "Chưa có sản phẩm nào để thêm." });

        var gio = GioHangSession.Doc(HttpContext.Session);
        var added = 0;
        var skipped = new List<string>();
        foreach (var uid in uids)
        {
            var sp = await _sanPhamRepo.GetByUidAsync(uid);
            if (sp is null || !sp.HienThi || sp.TonKho <= 0)
            {
                skipped.Add(sp?.TenSanPham ?? "Một sản phẩm");
                continue;
            }
            if (gio.All(g => g.UidSanPham != uid)) gio.Add(new GioHangItem { UidSanPham = uid, SoLuong = 1 });
            added++;
        }
        GioHangSession.Ghi(HttpContext.Session, gio);

        return Json(new
        {
            success = added > 0,
            added,
            skipped,
            totalCount = gio.Sum(g => g.SoLuong),
            redirectUrl = Url.Content("~/DatHang")
        });
    }
}
