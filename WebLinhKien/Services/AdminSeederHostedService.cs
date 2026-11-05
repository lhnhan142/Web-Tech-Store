using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Services;

/// <summary>
/// Chạy một lần khi ứng dụng khởi động: nếu bảng TaiKhoans còn trống thì tạo tài khoản admin
/// đầu tiên từ cấu hình AdminSeed (đặt trong User Secrets, KHÔNG đặt trong appsettings.json).
/// Đã có tài khoản thì không làm gì, nên chạy lại bao nhiêu lần cũng an toàn.
/// </summary>
public class AdminSeederHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminSeederHostedService> _logger;

    public AdminSeederHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<AdminSeederHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITaiKhoanRepository>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            if (await repo.AnyAsync()) return;

            var tenDangNhap = _config["AdminSeed:TenDangNhap"];
            var matKhau = _config["AdminSeed:MatKhau"];

            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrEmpty(matKhau))
            {
                _logger.LogWarning(
                    "Chưa có tài khoản admin và thiếu cấu hình AdminSeed:TenDangNhap / AdminSeed:MatKhau " +
                    "trong User Secrets, nên chưa tạo được tài khoản đầu tiên.");
                return;
            }

            if (matKhau.Length < 8)
            {
                _logger.LogWarning("AdminSeed:MatKhau phải có ít nhất 8 ký tự. Chưa tạo tài khoản admin.");
                return;
            }

            await repo.InsertAsync(new TaiKhoan
            {
                TenDangNhap = tenDangNhap.Trim(),
                MatKhauHash = passwordService.Hash(matKhau),
                HoTen = _config["AdminSeed:HoTen"] ?? "Quản trị viên",
                HoatDong = true,
                VaiTro = WebLinhKien.Helpers.AuthServiceExtensions.AdminRole
            });

            _logger.LogInformation("Đã tạo tài khoản admin đầu tiên: {TenDangNhap}", tenDangNhap.Trim());
        }
        catch (Exception ex)
        {
            // Không làm sập ứng dụng nếu database chưa sẵn sàng; chỉ ghi log.
            _logger.LogError(ex, "Không tạo được tài khoản admin mặc định.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
