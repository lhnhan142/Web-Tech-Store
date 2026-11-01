using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;
using WebLinhKien.Models;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.Responses;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Controllers;

[Route("DatHang")]
public class DatHangController : Controller
{
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly ICaiDatRepository _caiDatRepo;
    private readonly IValidator<DatHangRequest> _validator;

    public DatHangController(
        ISanPhamRepository sanPhamRepo,
        ICaiDatRepository caiDatRepo,
        IValidator<DatHangRequest> validator)
    {
        _sanPhamRepo = sanPhamRepo;
        _caiDatRepo = caiDatRepo;
        _validator = validator;
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

        // TODO (bước tiếp theo, phần của thành viên C): gọi dịch vụ đặt hàng để
        //   1. tải sản phẩm từ database, kiểm tra tồn kho, TÍNH LẠI giá và tổng tiền ở server;
        //   2. lưu DonDatHangs + ChiTietDonHangs và trừ TonKho trong MỘT transaction;
        //   3. GioHangSession.Xoa(HttpContext.Session);
        //   4. gọi IAdminNotifier.NotifyNewOrderAsync(...) sau khi commit;
        //   5. trả Status = 200 kèm Data = { maDon, tongThanhToan }.
        return StatusCode(StatusCodes.Status501NotImplemented, new Responses
        {
            Status = 501,
            Message = "Máy chủ chưa cài đặt chức năng lưu đơn hàng."
        });
    }

    // GET /DatHang/ThemMau - CHỈ ĐỂ THỬ khi phát triển: cho 2 sản phẩm seed vào giỏ rồi mở trang đặt hàng.
    // Xóa action này khi đã có chức năng giỏ hàng thật.
    [HttpGet("ThemMau")]
    public async Task<IActionResult> ThemMau([FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment()) return NotFound();

        var gio = new List<GioHangItem>();
        foreach (var ma in new[] { "MOUSE-LOGI-G102", "RAM-KING-16G" })
        {
            var sp = await _sanPhamRepo.GetByMaAsync(ma);
            if (sp is not null) gio.Add(new GioHangItem { UidSanPham = sp.Uid, SoLuong = 1 });
        }
        GioHangSession.Ghi(HttpContext.Session, gio);
        return RedirectToAction(nameof(Index));
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
