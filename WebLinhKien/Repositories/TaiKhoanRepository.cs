using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class TaiKhoanRepository : RepositoryBase, ITaiKhoanRepository
{
    public TaiKhoanRepository(DapperContext context) : base(context) { }

    public async Task<TaiKhoan?> GetByTenDangNhapAsync(string tenDangNhap)
    {
        const string sql = @"
            SELECT Uid, TenDangNhap, MatKhauHash, HoTen, HoatDong, NgayTao
            FROM dbo.TaiKhoans
            WHERE TenDangNhap = @TenDangNhap";

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
            INSERT INTO dbo.TaiKhoans (TenDangNhap, MatKhauHash, HoTen, HoatDong)
            OUTPUT INSERTED.Uid
            VALUES (@TenDangNhap, @MatKhauHash, @HoTen, @HoatDong)";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, taiKhoan);
    }
}
