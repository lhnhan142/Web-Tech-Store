using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Areas.Admin.Controllers;

public class DonHangController : AdminController
{
    private const int PageSize = 15;

    private readonly IDonDatHangRepository _donRepo;
    private readonly ITinhTrangDonHangRepository _tinhTrangRepo;

    public DonHangController(IDonDatHangRepository donRepo, ITinhTrangDonHangRepository tinhTrangRepo)
    {
        _donRepo = donRepo;
        _tinhTrangRepo = tinhTrangRepo;
    }

    // GET /Admin/DonHang?tinhTrang=MOI&q=...&page=1
    public async Task<IActionResult> Index(string? tinhTrang, string? q, int page = 1)
    {
        page = Math.Max(1, page);

        var tinhTrangs = await _tinhTrangRepo.GetAllAsync();
        if (!string.IsNullOrEmpty(tinhTrang) && !tinhTrangs.Any(t => t.MaTinhTrang == tinhTrang))
            tinhTrang = null;   // mã lạ trên đường dẫn: coi như "Tất cả"

        var ketQua = await _donRepo.GetPagedAsync(tinhTrang, q, page, PageSize);
        if (ketQua.TotalPages > 0 && page > ketQua.TotalPages)   // trang vượt quá: về trang cuối
        {
            page = ketQua.TotalPages;
            ketQua = await _donRepo.GetPagedAsync(tinhTrang, q, page, PageSize);
        }

        var vm = new DonHangListViewModel
        {
            Items = ketQua.Items,
            Page = page,
            TotalPages = ketQua.TotalPages,
            TotalItems = ketQua.TotalItems,
            MaTinhTrang = tinhTrang,
            Q = q,
            TinhTrangs = tinhTrangs,
            DemTheoTinhTrang = await _donRepo.DemTheoTinhTrangAsync()
        };

        // admin-don-hang.js gọi lại chính địa chỉ này khi có đơn mới và chỉ cần phần danh sách
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_DanhSachDon", vm);

        return View(vm);
    }

    // GET /Admin/DonHang/ChiTiet/5
    public async Task<IActionResult> ChiTiet(int id)
    {
        var don = await _donRepo.GetByUidAsync(id);
        if (don is null) return NotFound();

        var tinhTrangs = await _tinhTrangRepo.GetAllAsync();
        var tiepTheo = TinhTrangDonHangFlow.CacTinhTrangTiepTheo(don.MaTinhTrang);

        var vm = new DonHangChiTietViewModel
        {
            Don = don,
            ChiTiet = await _donRepo.GetChiTietAsync(id),
            TinhTrangHienTai = tinhTrangs.FirstOrDefault(t => t.MaTinhTrang == don.MaTinhTrang),
            TinhTrangTiepTheo = tinhTrangs.Where(t => tiepTheo.Contains(t.MaTinhTrang)).ToList()
        };
        return View(vm);
    }

    // POST /Admin/DonHang/DoiTinhTrang/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTinhTrang(int id, string maTinhTrang)
    {
        var don = await _donRepo.GetByUidAsync(id);
        if (don is null) return NotFound();

        // Luôn kiểm tra ở server, không tin nút bấm ở trình duyệt
        if (!TinhTrangDonHangFlow.HopLe(don.MaTinhTrang, maTinhTrang))
        {
            TempData["Loi"] = "Không thể chuyển đơn sang tình trạng này.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var thanhCong = await _donRepo.UpdateTinhTrangAsync(id, don.MaTinhTrang, maTinhTrang);
        if (thanhCong)
            TempData["ThanhCong"] = "Đã cập nhật tình trạng đơn hàng.";
        else
            TempData["Loi"] = "Đơn vừa được người khác thay đổi. Vui lòng kiểm tra lại tình trạng hiện tại.";

        return RedirectToAction(nameof(ChiTiet), new { id });
    }
}
