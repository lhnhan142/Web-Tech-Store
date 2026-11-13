using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebLinhKien.Models.Chat;
using WebLinhKien.Services;

namespace WebLinhKien.Controllers;

[Route("Chat")]
public class ChatController : Controller
{
    private readonly IChatService _chat;
    private readonly ILogger<ChatController> _log;

    public ChatController(IChatService chat, ILogger<ChatController> log)
    {
        _chat = chat;
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
}
