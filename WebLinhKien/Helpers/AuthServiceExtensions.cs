using Microsoft.AspNetCore.Authentication.Cookies;
using WebLinhKien.Services;

namespace WebLinhKien.Helpers;

public static class AuthServiceExtensions
{
    public const string AdminRole = "Admin";
    public const string NhanVienRole = "NhanVien";

    /// <summary>Dùng cho [Authorize(Roles = ...)]: cả hai vai trò đều được vào.</summary>
    public const string AdminHoacNhanVien = AdminRole + "," + NhanVienRole;

    /// <summary>
    /// Đăng ký cookie authentication, dịch vụ băm mật khẩu và việc tạo admin đầu tiên.
    /// Trong Program.cs: builder.Services.AddAdminAuthentication();
    /// </summary>
    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddHostedService<AdminSeederHostedService>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Admin/Account/Login";
                options.AccessDeniedPath = "/Admin/Account/AccessDenied";   // đã đăng nhập nhưng không đủ quyền
                options.Cookie.Name = "WebLinhKien.Admin";
                options.Cookie.HttpOnly = true;                          // JavaScript không đọc được cookie
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // chỉ gửi qua https
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                // Mỗi request kiểm tra lại database: khóa tài khoản hoặc đổi vai trò có hiệu lực ngay, không đợi cookie hết hạn.
                options.Events.OnValidatePrincipal = AdminCookieEvents.ValidateAsync;
            });

        services.AddAuthorization();
        return services;
    }
}
