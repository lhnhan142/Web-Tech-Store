using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;
using WebLinhKien.Models;
using WebLinhKien.Models.ViewModels.BuildPc;
using WebLinhKien.Models.ViewModels.Client;
using WebLinhKien.Services;

namespace WebLinhKien.Controllers;

[Route("BuildPc")]
public class BuildPcController : Controller
{
    private readonly IBuildPcService _buildPcService;
    private readonly ILogger<BuildPcController> _logger;

    public BuildPcController(IBuildPcService buildPcService, ILogger<BuildPcController> logger)
    {
        _buildPcService = buildPcService;
        _logger = logger;
    }

    // GET /BuildPc
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var vm = await _buildPcService.LayTrangAsync();
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải dữ liệu trang Build PC.");
            return View(new BuildPcIndexViewModel());
        }
    }

    // POST /BuildPc/KiemTra
    [HttpPost("KiemTra")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KiemTra([FromBody] BuildPcChonRequest request)
    {
        var ketQua = await _buildPcService.KiemTraAsync(request);
        return Json(new
        {
            success = ketQua.HopLe,
            hopLe = ketQua.HopLe,
            message = ketQua.HopLe
                ? "Cấu hình hợp lệ và sẵn sàng thêm vào giỏ hàng."
                : "Cấu hình chưa hợp lệ, vui lòng kiểm tra các cảnh báo bên dưới.",
            tongTien = ketQua.TongTien,
            tongTienText = ketQua.TongTienText,
            soMon = ketQua.SoMon,
            thieu = ketQua.Thieu,
            loi = ketQua.Loi,
            canhBao = ketQua.CanhBao,
            kiemTraPhuKien = ketQua.KiemTraPhuKien,
            congSuatUocTinhW = ketQua.CongSuatUocTinhW,
            congSuatPsuW = ketQua.CongSuatPsuW
        });
    }

    // POST /BuildPc/GoiY
    [HttpPost("GoiY")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoiY([FromBody] BuildPcGoiYRequest request)
    {
        var ketQua = await _buildPcService.GoiYTheoNganSachAsync(request);
        return Json(new
        {
            success = ketQua.ThanhCong,
            message = ketQua.ThongBao,
            nganSach = ketQua.NganSach,
            nganSachText = ketQua.NganSach.ToVnd(),
            data = ketQua.PhuongAns.Select(pa => new
            {
                ten = pa.Ten,
                moTa = pa.MoTa,
                tongTien = pa.TongTien,
                tongTienText = pa.TongTienText,
                conLai = pa.ConLai,
                conLaiText = pa.ConLaiText,
                canhBao = pa.CanhBao,
                items = pa.LinhKiens.Select(x => new
                {
                    uid = x.Uid,
                    ten = x.TenSanPham,
                    loai = x.MaDanhMuc,
                    tenLoai = TenLoai(x.MaDanhMuc),
                    icon = IconLoai(x.MaDanhMuc),
                    gia = x.GiaBan,
                    giaText = x.GiaBanText,
                    anh = x.AnhDaiDien,
                    thongSo = x.ThongSoChinh.Select(t => new { ten = t.Ten, giaTri = t.GiaTri })
                })
            })
        });
    }

    // POST /BuildPc/ThemCaBo
    [HttpPost("ThemCaBo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemCaBo([FromBody] BuildPcChonRequest request)
    {
        var ketQua = await _buildPcService.KiemTraAsync(request);
        if (!ketQua.HopLe)
        {
            return Json(new
            {
                success = false,
                message = "Cấu hình chưa hợp lệ nên chưa thể thêm vào giỏ hàng.",
                thieu = ketQua.Thieu,
                loi = ketQua.Loi,
                canhBao = ketQua.CanhBao
            });
        }

        var gio = GioHangSession.Doc(HttpContext.Session);
        var soMonThem = 0;

        foreach (var sp in ketQua.LinhKiens.Concat(ketQua.PhuKiens))
        {
            var soLuongToiDa = Math.Min(WebConstants.SoLuongToiDa, sp.TonKho);
            if (soLuongToiDa <= 0) continue;

            var item = gio.FirstOrDefault(g => g.UidSanPham == sp.Uid);
            if (item == null)
            {
                gio.Add(new GioHangItem { UidSanPham = sp.Uid, SoLuong = 1 });
                soMonThem++;
            }
            else if (item.SoLuong < soLuongToiDa)
            {
                item.SoLuong++;
                soMonThem++;
            }
        }

        GioHangSession.Ghi(HttpContext.Session, gio);
        var tongSoLuong = GioHangSession.Dem(HttpContext.Session);

        return Json(new
        {
            success = true,
            message = soMonThem > 0
                ? $"Đã thêm {soMonThem} sản phẩm của bộ PC vào giỏ hàng."
                : "Các sản phẩm của bộ PC đã đạt số lượng tối đa trong giỏ hàng.",
            addedCount = soMonThem,
            totalCount = tongSoLuong,
            tongTien = ketQua.TongTien,
            tongTienText = ketQua.TongTienText,
            canhBao = ketQua.CanhBao,
            redirectUrl = Url.Content("~/DatHang")
        });
    }

    private static string TenLoai(string maDanhMuc) => maDanhMuc.ToUpperInvariant() switch
    {
        "TANNHIET" => "Tản nhiệt",
        _ => CategoryIconHelper.GetCategoryName(maDanhMuc)
    };

    private static string IconLoai(string maDanhMuc) => maDanhMuc.ToUpperInvariant() switch
    {
        "TANNHIET" => "fa-fan",
        _ => CategoryIconHelper.GetIcon(maDanhMuc)
    };
}
