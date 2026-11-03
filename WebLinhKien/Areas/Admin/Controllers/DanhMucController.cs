using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Areas.Admin.Controllers;

public class DanhMucController : AdminController
{
    private readonly IDanhMucRepository _repo;
    private readonly ISanPhamRepository _sanPhamRepo;

    public DanhMucController(IDanhMucRepository repo, ISanPhamRepository sanPhamRepo)
    {
        _repo = repo;
        _sanPhamRepo = sanPhamRepo;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new DanhMucListViewModel
        {
            Items = await _repo.GetAllAsync(chiHienThi: false),   // admin thấy cả danh mục đang ẩn
            DemSanPham = await _sanPhamRepo.DemTheoDanhMucAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new DanhMucForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DanhMucForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var ma = form.MaDanhMuc.Trim().ToUpperInvariant();
        if (await _repo.GetByMaAsync(ma) is not null)
        {
            ModelState.AddModelError(nameof(form.MaDanhMuc), "Mã này đã tồn tại.");
            return View(form);
        }

        try
        {
            await _repo.InsertAsync(new DanhMuc
            {
                MaDanhMuc = ma,
                TenDanhMuc = form.TenDanhMuc.Trim(),
                Ordering = form.Ordering,
                HienThi = form.HienThi
            });
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)   // trùng khóa do hai người thêm cùng lúc
        {
            ModelState.AddModelError(nameof(form.MaDanhMuc), "Mã này đã tồn tại.");
            return View(form);
        }

        TempData["ThanhCong"] = $"Đã thêm danh mục {ma}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var dm = await _repo.GetByMaAsync(id);
        if (dm is null) return NotFound();

        return View(new DanhMucForm
        {
            MaDanhMuc = dm.MaDanhMuc,
            TenDanhMuc = dm.TenDanhMuc,
            Ordering = dm.Ordering,
            HienThi = dm.HienThi
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, DanhMucForm form)
    {
        // Mã là khóa chính, không cho đổi: luôn lấy từ đường dẫn, bỏ qua giá trị gửi lên
        form.MaDanhMuc = id;
        ModelState.Remove(nameof(DanhMucForm.MaDanhMuc));
        if (!ModelState.IsValid) return View(form);

        if (await _repo.GetByMaAsync(id) is null) return NotFound();

        await _repo.UpdateAsync(new DanhMuc
        {
            MaDanhMuc = id,
            TenDanhMuc = form.TenDanhMuc.Trim(),
            Ordering = form.Ordering,
            HienThi = form.HienThi
        });

        TempData["ThanhCong"] = "Đã lưu danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(string id)
    {
        // Không có khóa ngoại trong database nên phải tự chặn: xóa danh mục còn sản phẩm sẽ làm sản phẩm mồ côi
        var dem = (await _sanPhamRepo.DemTheoDanhMucAsync()).GetValueOrDefault(id);
        if (dem > 0)
        {
            TempData["Loi"] = $"Không thể xóa: còn {dem} sản phẩm thuộc danh mục này. " +
                              "Hãy chuyển sản phẩm sang danh mục khác, hoặc chỉ cần ẩn danh mục.";
            return RedirectToAction(nameof(Index));
        }

        if (await _repo.DeleteAsync(id)) TempData["ThanhCong"] = "Đã xóa danh mục.";
        else TempData["Loi"] = "Không tìm thấy danh mục cần xóa.";
        return RedirectToAction(nameof(Index));
    }
}
