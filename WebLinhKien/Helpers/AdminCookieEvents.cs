using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Helpers;

/// <summary>
/// Kiểm tra cookie đăng nhập với database ở MỖI request, để:
/// - tài khoản bị khóa (HoatDong = 0) hoặc bị xóa bị đăng xuất ngay;
/// - đổi vai trò có hiệu lực ngay (cookie cũ mang vai trò cũ thì bị bắt đăng nhập lại).
/// Một câu SELECT theo khóa chính, rất nhẹ so với lợi ích về an toàn.
/// </summary>
public static class AdminCookieEvents
{
    private const string KeyCanDoiMatKhau = "AdminCanDoiMatKhau";

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var vaiTroTrongCookie = principal?.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
        {
            await TuChoiAsync(context);
            return;
        }

        var repo = context.HttpContext.RequestServices.GetRequiredService<ITaiKhoanRepository>();
        var taiKhoan = await repo.GetByUidAsync(uid);

        if (taiKhoan is null || !taiKhoan.HoatDong || taiKhoan.VaiTro != vaiTroTrongCookie)
        {
            await TuChoiAsync(context);
            return;
        }

        context.HttpContext.Items[KeyCanDoiMatKhau] = taiKhoan.CanDoiMatKhau;
    }

    private static async Task TuChoiAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Tài khoản mới tạo hoặc vừa được đặt lại mật khẩu (CanDoiMatKhau = 1) chỉ được vào trang đổi mật khẩu
    /// và đăng xuất; vào trang quản trị khác thì bị chuyển sang trang đổi mật khẩu.
    /// Đặt SAU UseAuthentication() và TRƯỚC UseAuthorization().
    /// </summary>
    public static IApplicationBuilder UseBatDoiMatKhau(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.Items.TryGetValue(KeyCanDoiMatKhau, out var v) && v is true)
            {
                var path = context.Request.Path;
                var duocPhep = path.StartsWithSegments("/Admin/HoSo") || path.StartsWithSegments("/Admin/Account");
                if (path.StartsWithSegments("/Admin") && !duocPhep)
                {
                    context.Response.Redirect("/Admin/HoSo/DoiMatKhau");
                    return;
                }
            }
            await next();
        });
    }
}
