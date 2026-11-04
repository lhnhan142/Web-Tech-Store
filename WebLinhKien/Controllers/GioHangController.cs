using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.Responses;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Controllers;

/// <summary>
/// Sửa và xóa sản phẩm trong giỏ hàng (session). Trang đặt hàng (/DatHang) gọi các endpoint này mỗi khi khách bấm + / − / Xóa,
/// để giỏ hàng trong session luôn khớp với màn hình. "Thêm vào giỏ" nằm ở SanPhamController.ThemVaoGio (thành viên B).
/// </summary>
[Route("GioHang")]
public class GioHangController : Controller
{
    private readonly ISanPhamRepository _sanPhamRepo;

    public GioHangController(ISanPhamRepository sanPhamRepo) => _sanPhamRepo = sanPhamRepo;

    // GET /GioHang → trang giỏ hàng chính là trang đặt hàng
    [HttpGet("")]
    public IActionResult Index() => RedirectToAction("Index", "DatHang");

    // POST /GioHang/CapNhat  { uidSanPham, soLuong }
    [HttpPost("CapNhat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhat([FromBody] GioHangItemRequest? req)
    {
        if (req is null || req.UidSanPham <= 0) return Tra(400, "Dữ liệu không hợp lệ.");
        if (GioHangSession.SoLuong(HttpContext.Session, req.UidSanPham) == 0)
            return Tra(404, "Sản phẩm không có trong giỏ hàng.");

        var sp = await _sanPhamRepo.GetByUidAsync(req.UidSanPham);
        if (sp is null || !sp.HienThi || sp.TonKho <= 0)
        {
            GioHangSession.DatSoLuong(HttpContext.Session, req.UidSanPham, 0);
            return Tra(409, "Sản phẩm đã hết hàng nên được bỏ khỏi giỏ.");
        }

        var soLuong = Math.Clamp(req.SoLuong, 1, Math.Min(WebConstants.SoLuongToiDa, sp.TonKho));
        GioHangSession.DatSoLuong(HttpContext.Session, sp.Uid, soLuong);
        return Tra(200, "Đã cập nhật giỏ hàng.");
    }

    // POST /GioHang/Xoa  { uidSanPham }
    [HttpPost("Xoa")]
    [ValidateAntiForgeryToken]
    public IActionResult Xoa([FromBody] GioHangItemRequest? req)
    {
        if (req is null || req.UidSanPham <= 0) return Tra(400, "Dữ liệu không hợp lệ.");
        GioHangSession.DatSoLuong(HttpContext.Session, req.UidSanPham, 0);
        return Tra(200, "Đã xóa khỏi giỏ hàng.");
    }

    private IActionResult Tra(int status, string message) =>
        StatusCode(status, new Responses
        {
            Status = status,
            Message = message,
            Data = new { count = GioHangSession.Dem(HttpContext.Session) }
        });
}
