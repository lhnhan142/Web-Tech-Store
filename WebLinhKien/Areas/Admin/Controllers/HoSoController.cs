using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Models.Requests;
using WebLinhKien.Repositories.Interfaces;
using WebLinhKien.Services;

namespace WebLinhKien.Areas.Admin.Controllers;

/// <summary>Người đăng nhập tự quản lý tài khoản của mình (Admin và NhanVien đều dùng được).</summary>
public class HoSoController : AdminController
{
    private readonly ITaiKhoanRepository _repo;
    private readonly IPasswordService _passwordService;

    public HoSoController(ITaiKhoanRepository repo, IPasswordService passwordService)
    {
        _repo = repo;
        _passwordService = passwordService;
    }

    [HttpGet]
    public IActionResult DoiMatKhau() => View(new DoiMatKhauForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauForm form)
    {
        if (!ModelState.IsValid) return View(form);

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)) return Forbid();
        var taiKhoan = await _repo.GetByUidAsync(uid);
        if (taiKhoan is null) return Forbid();

        if (!_passwordService.Verify(taiKhoan, form.MatKhauHienTai))
        {
            ModelState.AddModelError(nameof(form.MatKhauHienTai), "Mật khẩu hiện tại không đúng.");
            return View(form);
        }

        if (form.MatKhauMoi == form.MatKhauHienTai)
        {
            ModelState.AddModelError(nameof(form.MatKhauMoi), "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return View(form);
        }

        await _repo.DoiMatKhauAsync(uid, _passwordService.Hash(form.MatKhauMoi), canDoiMatKhau: false);

        TempData["ThanhCong"] = "Đã đổi mật khẩu.";
        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }
}
