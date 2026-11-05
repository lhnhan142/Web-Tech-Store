using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class TaiKhoanRepository : RepositoryBase, ITaiKhoanRepository
{
    private const string Cot = "Uid, TenDangNhap, MatKhauHash, HoTen, HoatDong, VaiTro, CanDoiMatKhau, NgayTao";

    public TaiKhoanRepository(DapperContext context) : base(context) { }

    public async Task<TaiKhoan?> GetByTenDangNhapAsync(string tenDangNhap)
    {
        const string sql = "SELECT " + Cot + " FROM dbo.TaiKhoans WHERE TenDangNhap = @TenDangNhap";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TaiKhoan>(sql, new { TenDangNhap = tenDangNhap });
    }

    public async Task<bool> AnyAsync()
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.TaiKhoans) THEN 1 ELSE 0 END";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql) == 1;
    }

    public async Task<int> InsertAsync(TaiKhoan taiKhoan)
    {
        const string sql = @"
            INSERT INTO dbo.TaiKhoans (TenDangNhap, MatKhauHash, HoTen, HoatDong, VaiTro, CanDoiMatKhau)
            OUTPUT INSERTED.Uid
            VALUES (@TenDangNhap, @MatKhauHash, @HoTen, @HoatDong, @VaiTro, @CanDoiMatKhau)";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, taiKhoan);
    }

    public async Task<TaiKhoan?> GetByUidAsync(int uid)
    {
        const string sql = "SELECT " + Cot + " FROM dbo.TaiKhoans WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TaiKhoan>(sql, new { Uid = uid });
    }

    public async Task<IReadOnlyList<TaiKhoan>> GetAllAsync()
    {
        const string sql = "SELECT " + Cot + " FROM dbo.TaiKhoans ORDER BY VaiTro, TenDangNhap";

        using var conn = Context.CreateConnection();
        return (await conn.QueryAsync<TaiKhoan>(sql)).ToList();
    }

    public async Task<bool> UpdateThongTinAsync(int uid, string? hoTen, string vaiTro)
    {
        const string sql = "UPDATE dbo.TaiKhoans SET HoTen = @HoTen, VaiTro = @VaiTro WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, new { Uid = uid, HoTen = hoTen, VaiTro = vaiTro }) > 0;
    }

    public async Task<bool> SetHoatDongAsync(int uid, bool hoatDong)
    {
        const string sql = "UPDATE dbo.TaiKhoans SET HoatDong = @HoatDong WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, new { Uid = uid, HoatDong = hoatDong }) > 0;
    }

    public async Task<bool> DoiMatKhauAsync(int uid, string matKhauHash, bool canDoiMatKhau)
    {
        const string sql = "UPDATE dbo.TaiKhoans SET MatKhauHash = @Hash, CanDoiMatKhau = @Can WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, new { Uid = uid, Hash = matKhauHash, Can = canDoiMatKhau }) > 0;
    }

    public async Task<int> DemAdminHoatDongAsync()
    {
        const string sql = "SELECT COUNT(*) FROM dbo.TaiKhoans WHERE VaiTro = N'Admin' AND HoatDong = 1";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql);
    }
}
