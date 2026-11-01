using WebLinhKien.Repositories;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Data;

public static class DataServiceExtensions
{
    /// <summary>
    /// Đăng ký DapperContext và toàn bộ repository dùng chung.
    /// Trong Program.cs chỉ cần: builder.Services.AddDataLayer();
    /// Repository mới (của B, C, D) thì thêm một dòng AddScoped ở đây.
    /// </summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services)
    {
        services.AddSingleton<DapperContext>();

        services.AddScoped<IDonDatHangRepository, DonDatHangRepository>();
        services.AddScoped<IDanhMucRepository, DanhMucRepository>();
        services.AddScoped<IThuongHieuRepository, ThuongHieuRepository>();
        services.AddScoped<ISanPhamRepository, SanPhamRepository>();
        services.AddScoped<ITinhTrangDonHangRepository, TinhTrangDonHangRepository>();
        services.AddScoped<ICaiDatRepository, CaiDatRepository>();
        services.AddScoped<ITaiKhoanRepository, TaiKhoanRepository>();

        return services;
    }
}
