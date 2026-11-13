using WebLinhKien.Data;
using WebLinhKien.Helpers;
using WebLinhKien.WebSockets;
using FluentValidation;
using WebLinhKien.Validators;
using WebLinhKien.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace WebLinhKien
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDataLayer();
            builder.Services.AddAdminAuthentication();
            builder.Services.AddAdminWebSockets();
            builder.Services.AddValidatorsFromAssemblyContaining<DatHangRequestValidator>();
            builder.Services.AddScoped<IDatHangService, DatHangService>();
            builder.Services.AddChatbot(builder.Configuration);   // D: chatbot tư vấn (khóa API trong user-secrets)

            builder.Services.AddScoped<IBuildPcService, BuildPcService>();

            // Giới hạn tần suất theo IP cho đặt hàng và lấy mã QR (chống spam đơn hàng): 10 lần mỗi phút
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("dat-hang", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
            });
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            // MapStaticAssets chỉ phục vụ file có sẵn lúc build; ảnh admin tải lên lúc chạy (wwwroot/uploads) cần UseStaticFiles
            app.UseStaticFiles();
            app.UseRouting();

            app.UseRateLimiter();
            app.UseSession();
            app.UseAuthentication();
            app.UseBatDoiMatKhau();   // tài khoản mới / vừa đặt lại mật khẩu phải đổi mật khẩu trước
            app.UseAuthorization();
            app.UseAdminWebSockets();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
