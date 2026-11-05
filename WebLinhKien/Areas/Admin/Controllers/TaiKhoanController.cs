using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;
using WebLinhKien.Services;

namespace WebLinhKien.Areas.Admin.Controllers;

/// <summary>Quản lý tài khoản quản trị và nhân viên. Chỉ Admin.</summary>
[Authorize(Roles = AuthServiceExtensions.AdminRole)]
public class TaiKhoanController : AdminController
{
    private readonly ITaiKhoanRepository _repo;
    private readonly IPasswordService _passwordService;

    public TaiKhoanController(ITaiKhoanRepository repo, IPasswordService passwordService)
    {
        _repo = repo;
        _passwordService = passwordService;
    }

    private int UidHienTai => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var u) ? u : 0;

    private static bool VaiTroHopLe(string? vaiTro) =>
        vaiTro == AuthServiceExtensions.AdminRole || vaiTro == AuthServiceExtensions.NhanVienRole;

    public async Task<IActionResult> Index()
    {
        return View(new TaiKhoanListViewModel
        {
            Items = await _repo.GetAllAsync(),
            UidHienTai = UidHienTai
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new TaiKhoanForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaiKhoanForm form)
    {
        if (!VaiTroHopLe(form.VaiTro))
            ModelState.AddModelError(nameof(form.VaiTro), "Vai trò không hợp lệ.");
        if (!ModelState.IsValid) return View(form);

        var ten = form.TenDangNhap.Trim();
        if (await _repo.GetByTenDangNhapAsync(ten) is not null)
        {
            ModelState.AddModelError(nameof(form.TenDangNhap), "Tên đăng nhập này đã có người dùng.");
            return View(form);
        }

        try
        {
            await _repo.InsertAsync(new TaiKhoan
            {
                TenDangNhap = ten,
                MatKhauHash = _passwordService.Hash(form.MatKhau),
                HoTen = string.IsNullOrWhiteSpace(form.HoTen) ? null : form.HoTen.Trim(),
                HoatDong = true,
                VaiTro = form.VaiTro,
                CanDoiMatKhau = true   // mật khẩu do admin đặt, người dùng phải đổi ở lần đăng nhập đầu
            });
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(form.TenDangNhap), "Tên đăng nhập này đã có người dùng.");
            return View(form);
        }

        TempData["ThanhCong"] = $"Đã tạo tài khoản {ten}. Người dùng sẽ phải đổi mật khẩu khi đăng nhập lần đầu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tk = await _repo.GetByUidAsync(id);
        if (tk is null) return NotFound();

        ViewData["DangSua"] = true;
        ViewData["LaChinhMinh"] = tk.Uid == UidHienTai;
        return View(new TaiKhoanForm { TenDangNhap = tk.TenDangNhap, HoTen = tk.HoTen, VaiTro = tk.VaiTro });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaiKhoanForm form)
    {
        // Khi sửa chỉ đổi họ tên và vai trò; tên đăng nhập và mật khẩu không nhận từ form
        ModelState.Remove(nameof(form.TenDangNhap));
        ModelState.Remove(nameof(form.MatKhau));

        var tk = await _repo.GetByUidAsync(id);
        if (tk is null) return NotFound();

        form.TenDangNhap = tk.TenDangNhap;
        ViewData["DangSua"] = true;
        ViewData["LaChinhMinh"] = tk.Uid == UidHienTai;

        if (!VaiTroHopLe(form.VaiTro))
            ModelState.AddModelError(nameof(form.VaiTro), "Vai trò không hợp lệ.");
        if (!ModelState.IsValid) return View(form);

        if (form.VaiTro != tk.VaiTro)
        {
            // Không tự đổi vai trò của chính mình (tránh tự mất quyền admin) và luôn còn ít nhất một admin hoạt động
            if (tk.Uid == UidHienTai)
            {
                ModelState.AddModelError(nameof(form.VaiTro), "Bạn không thể tự đổi vai trò của chính mình.");
                return View(form);
            }
            if (tk.VaiTro == AuthServiceExtensions.AdminRole && tk.HoatDong && await _repo.DemAdminHoatDongAsync() <= 1)
            {
                ModelState.AddModelError(nameof(form.VaiTro), "Đây là admin hoạt động duy nhất, không thể hạ quyền.");
                return View(form);
            }
        }

        await _repo.UpdateThongTinAsync(id, string.IsNullOrWhiteSpace(form.HoTen) ? null : form.HoTen.Trim(), form.VaiTro);

        TempData["ThanhCong"] = "Đã lưu tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaMo(int id)
    {
        var tk = await _repo.GetByUidAsync(id);
        if (tk is null) return NotFound();

        if (tk.HoatDong)
        {
            if (tk.Uid == UidHienTai)
            {
                TempData["Loi"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }
            if (tk.VaiTro == AuthServiceExtensions.AdminRole && await _repo.DemAdminHoatDongAsync() <= 1)
            {
                TempData["Loi"] = "Đây là admin hoạt động duy nhất, không thể khóa.";
                return RedirectToAction(nameof(Index));
            }
        }

        await _repo.SetHoatDongAsync(id, !tk.HoatDong);
        TempData["ThanhCong"] = tk.HoatDong
            ? $"Đã khóa tài khoản {tk.TenDangNhap}. Người này bị đăng xuất ngay."
            : $"Đã mở khóa tài khoản {tk.TenDangNhap}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DatLaiMatKhau(int id)
    {
        var tk = await _repo.GetByUidAsync(id);
        if (tk is null) return NotFound();
        if (tk.Uid == UidHienTai) return RedirectToAction("DoiMatKhau", "HoSo");   // mật khẩu của mình thì tự đổi

        ViewData["TaiKhoan"] = tk;
        return View(new DatLaiMatKhauForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatLaiMatKhau(int id, DatLaiMatKhauForm form)
    {
        var tk = await _repo.GetByUidAsync(id);
        if (tk is null) return NotFound();
        if (tk.Uid == UidHienTai) return RedirectToAction("DoiMatKhau", "HoSo");

        if (!ModelState.IsValid)
        {
            ViewData["TaiKhoan"] = tk;
            return View(form);
        }

        await _repo.DoiMatKhauAsync(id, _passwordService.Hash(form.MatKhauMoi), canDoiMatKhau: true);

        TempData["ThanhCong"] = $"Đã đặt lại mật khẩu cho {tk.TenDangNhap}. Người này phải đổi mật khẩu ở lần đăng nhập tới.";
        return RedirectToAction(nameof(Index));
    }
}
