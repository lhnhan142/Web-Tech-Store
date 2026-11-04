using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebLinhKien.Helpers;
using WebLinhKien.Models;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.Responses;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;
using WebLinhKien.Services;

namespace WebLinhKien.Controllers;

[Route("DatHang")]
public class DatHangController : Controller
{
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly ICaiDatRepository _caiDatRepo;
    private readonly IValidator<DatHangRequest> _validator;
    private readonly IDatHangService _datHangService;

    public DatHangController(
        ISanPhamRepository sanPhamRepo,
        ICaiDatRepository caiDatRepo,
        IValidator<DatHangRequest> validator,
        IDatHangService datHangService)
    {
        _sanPhamRepo = sanPhamRepo;
        _caiDatRepo = caiDatRepo;
        _validator = validator;
        _datHangService = datHangService;
    }

    // GET /DatHang - trang đặt hàng, hiển thị giỏ hàng trong session (tên, giá lấy từ database)
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var gio = GioHangSession.Doc(HttpContext.Session);
        var sanPhams = await _sanPhamRepo.GetByUidsAsync(gio.Select(g => g.UidSanPham));

        var items = new List<GioHangItemViewModel>();
        foreach (var g in gio)
        {
            var sp = sanPhams.FirstOrDefault(s => s.Uid == g.UidSanPham);
            if (sp is null || !sp.HienThi || sp.TonKho <= 0) continue;   // bỏ sản phẩm đã ẩn / hết hàng

            items.Add(new GioHangItemViewModel
            {
                Uid = sp.Uid,
                Ten = sp.TenSanPham,
                Gia = sp.GiaBan,
                SoLuong = Math.Clamp(g.SoLuong, 1, Math.Min(WebConstants.SoLuongToiDa, sp.TonKho)),
                TonKho = sp.TonKho,
                Icon = LayIcon(sp.MaDanhMuc)
            });
        }

        var vm = new DatHangViewModel
        {
            Items = items,
            PhiGiaoHang = await _caiDatRepo.GetDecimalAsync(CaiDatKeys.PhiGiaoHang, 30000),
            NguongMienPhi = await _caiDatRepo.GetDecimalAsync(CaiDatKeys.NguongMienPhiGiaoHang, 500000)
        };
        return View(vm);
    }

    // POST /DatHang - nhận JSON từ dat-hang.js (kèm header RequestVerificationToken)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("dat-hang")]
    public async Task<IActionResult> Gui([FromBody] DatHangRequest? request)
    {
        if (request is null)
            return BadRequest(new Responses { Status = 400, Message = "Dữ liệu gửi lên không hợp lệ." });

        Chuanhoa(request);

        var ketQua = await _validator.ValidateAsync(request);
        if (!ketQua.IsValid)
        {
            // Gom lỗi theo từng ô; khóa viết thường chữ đầu (TenKhachHang -> tenKhachHang) để JS khớp với id ô nhập
            var loi = ketQua.Errors
                .GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new Responses
            {
                Status = 400,
                Message = ketQua.Errors[0].ErrorMessage,   // lỗi đầu tiên, giống README
                Error = loi
            });
        }

        // Tải sản phẩm, kiểm tra tồn kho, tính lại tiền, lưu đơn + trừ kho (một transaction), báo cho admin
        var kq = await _datHangService.DatHangAsync(request);
        if (kq.Status != StatusCodes.Status200OK)
            return StatusCode(kq.Status, new Responses { Status = kq.Status, Message = kq.Message });

        GioHangSession.Xoa(HttpContext.Session);

        // Khách chuyển khoản: trả kèm thông tin ngân hàng và đường dẫn ảnh QR do server tạo
        string? qrUrl = null;
        if (kq.NganHang is not null)
            qrUrl = Url.Action(nameof(QR), "DatHang", new { maDon = kq.MaDon, sdt = request.Sdt });

        return Ok(new Responses
        {
            Status = 200,
            Message = kq.Message,
            Data = new
            {
                maDon = kq.MaDon,
                tongThanhToan = kq.TongThanhToan,
                qrUrl,
                nganHang = kq.NganHang is null ? null : new
                {
                    ten = kq.NganHang.Ten,
                    soTaiKhoan = kq.NganHang.SoTaiKhoan,
                    chuTaiKhoan = kq.NganHang.ChuTaiKhoan
                }
            }
        });
    }

    // GET /DatHang/QR?maDon=...&sdt=... → ảnh PNG mã QR chuyển khoản (phải khớp cả mã đơn và số điện thoại)
    [HttpGet("QR")]
    [EnableRateLimiting("dat-hang")]
    public async Task<IActionResult> QR(string? maDon, string? sdt)
    {
        if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(sdt)) return NotFound();

        var png = await _datHangService.TaoQrPngAsync(maDon.Trim(), sdt.Trim());
        if (png is null) return NotFound();

        Response.Headers.CacheControl = "no-store";
        return File(png, "image/png");
    }

    private static void Chuanhoa(DatHangRequest r)
    {
        r.TenKhachHang = r.TenKhachHang?.Trim() ?? string.Empty;
        r.Sdt = r.Sdt?.Trim() ?? string.Empty;
        r.DiaChi = r.DiaChi?.Trim() ?? string.Empty;
        r.GhiChu = string.IsNullOrWhiteSpace(r.GhiChu) ? null : r.GhiChu.Trim();
        r.MaHinhThucThanhToan = r.MaHinhThucThanhToan?.Trim() ?? string.Empty;
        r.Items ??= new List<DatHangItemRequest>();
    }

    private static string LayIcon(string maDanhMuc) => maDanhMuc switch
    {
        "CPU" => "fa-microchip",
        "MAINBOARD" => "fa-server",
        "RAM" => "fa-memory",
        "VGA" => "fa-display",
        "SSD" => "fa-hard-drive",
        "PSU" => "fa-bolt",
        "CASE" => "fa-desktop",
        "PHUKIEN" => "fa-computer-mouse",
        _ => "fa-box"
    };
}
