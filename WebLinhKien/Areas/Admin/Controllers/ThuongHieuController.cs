using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Areas.Admin.Controllers;

public class ThuongHieuController : AdminController
{
    private readonly IThuongHieuRepository _repo;
    private readonly ISanPhamRepository _sanPhamRepo;

    public ThuongHieuController(IThuongHieuRepository repo, ISanPhamRepository sanPhamRepo)
    {
        _repo = repo;
        _sanPhamRepo = sanPhamRepo;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new ThuongHieuListViewModel
        {
            Items = await _repo.GetAllAsync(chiHienThi: false),
            DemSanPham = await _sanPhamRepo.DemTheoThuongHieuAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new ThuongHieuForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThuongHieuForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var ma = form.MaThuongHieu.Trim().ToUpperInvariant();
        if (await _repo.GetByMaAsync(ma) is not null)
        {
            ModelState.AddModelError(nameof(form.MaThuongHieu), "Mã này đã tồn tại.");
            return View(form);
        }

        try
        {
            await _repo.InsertAsync(new ThuongHieu
            {
                MaThuongHieu = ma,
                TenThuongHieu = form.TenThuongHieu.Trim(),
                Logo = string.IsNullOrWhiteSpace(form.Logo) ? null : form.Logo.Trim(),
                HienThi = form.HienThi
            });
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(form.MaThuongHieu), "Mã này đã tồn tại.");
            return View(form);
        }

        TempData["ThanhCong"] = $"Đã thêm thương hiệu {ma}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var th = await _repo.GetByMaAsync(id);
        if (th is null) return NotFound();

        return View(new ThuongHieuForm
        {
            MaThuongHieu = th.MaThuongHieu,
            TenThuongHieu = th.TenThuongHieu,
            Logo = th.Logo,
            HienThi = th.HienThi
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ThuongHieuForm form)
    {
        form.MaThuongHieu = id;   // mã là khóa chính, không cho đổi
        ModelState.Remove(nameof(ThuongHieuForm.MaThuongHieu));
        if (!ModelState.IsValid) return View(form);

        if (await _repo.GetByMaAsync(id) is null) return NotFound();

        await _repo.UpdateAsync(new ThuongHieu
        {
            MaThuongHieu = id,
            TenThuongHieu = form.TenThuongHieu.Trim(),
            Logo = string.IsNullOrWhiteSpace(form.Logo) ? null : form.Logo.Trim(),
            HienThi = form.HienThi
        });

        TempData["ThanhCong"] = "Đã lưu thương hiệu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(string id)
    {
        var dem = (await _sanPhamRepo.DemTheoThuongHieuAsync()).GetValueOrDefault(id);
        if (dem > 0)
        {
            TempData["Loi"] = $"Không thể xóa: còn {dem} sản phẩm thuộc thương hiệu này. " +
                              "Hãy chuyển sản phẩm sang thương hiệu khác, hoặc chỉ cần ẩn thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        if (await _repo.DeleteAsync(id)) TempData["ThanhCong"] = "Đã xóa thương hiệu.";
        else TempData["Loi"] = "Không tìm thấy thương hiệu cần xóa.";
        return RedirectToAction(nameof(Index));
    }
}
