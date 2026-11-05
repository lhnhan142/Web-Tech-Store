using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Requests;
using WebLinhKien.Repositories.Interfaces;
using WebLinhKien.Services;

namespace WebLinhKien.Areas.Admin.Controllers;

[Area("Admin")]
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly ITaiKhoanRepository _taiKhoanRepo;
    private readonly IPasswordService _passwordService;

    public AccountController(ITaiKhoanRepository taiKhoanRepo, IPasswordService passwordService)
    {
        _taiKhoanRepo = taiKhoanRepo;
        _passwordService = passwordService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToLocal(returnUrl);
        return View(new LoginRequest { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        if (!ModelState.IsValid) return View(model);

        var taiKhoan = await _taiKhoanRepo.GetByTenDangNhapAsync(model.TenDangNhap.Trim());
        var matKhauDung = _passwordService.Verify(taiKhoan, model.MatKhau);

        // Cùng một thông báo cho "sai tên" và "sai mật khẩu" để không lộ tài khoản nào tồn tại.
        if (!matKhauDung || taiKhoan is null)
        {
            ModelState.AddModelError(string.Empty, "Sai tên đăng nhập hoặc mật khẩu.");
            return View(model);
        }

        if (!taiKhoan.HoatDong)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, taiKhoan.Uid.ToString()),
            new(ClaimTypes.Name, taiKhoan.TenDangNhap),
            new("HoTen", taiKhoan.HoTen ?? taiKhoan.TenDangNhap),
            new(ClaimTypes.Role, taiKhoan.VaiTro)   // Admin hoặc NhanVien, đọc từ database
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = model.GhiNho });

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // Đã đăng nhập nhưng không đủ quyền (ví dụ nhân viên vào trang Cài đặt): AccessDeniedPath trỏ về đây.
    [HttpGet]
    public IActionResult AccessDenied() => View();

    // Chỉ chuyển hướng tới địa chỉ nội bộ, chặn tấn công open redirect qua returnUrl.
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }
}
