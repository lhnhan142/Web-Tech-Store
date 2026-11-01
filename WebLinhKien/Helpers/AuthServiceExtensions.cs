using Microsoft.AspNetCore.Authentication.Cookies;
using WebLinhKien.Services;

namespace WebLinhKien.Helpers;

public static class AuthServiceExtensions
{
    public const string AdminRole = "Admin";

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
                options.AccessDeniedPath = "/Admin/Account/Login";
                options.Cookie.Name = "WebLinhKien.Admin";
                options.Cookie.HttpOnly = true;                          // JavaScript không đọc được cookie
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // chỉ gửi qua https
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        services.AddAuthorization();
        return services;
    }
}
