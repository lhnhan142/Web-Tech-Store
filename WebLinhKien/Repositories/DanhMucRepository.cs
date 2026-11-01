using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class DanhMucRepository : RepositoryBase, IDanhMucRepository
{
    public DanhMucRepository(DapperContext context) : base(context) { }

    public async Task<IReadOnlyList<DanhMuc>> GetAllAsync(bool chiHienThi = true)
    {
        const string sql = @"
            SELECT MaDanhMuc, TenDanhMuc, Ordering, HienThi
            FROM dbo.DanhMucs
            WHERE (@ChiHienThi = 0 OR HienThi = 1)
            ORDER BY Ordering, TenDanhMuc";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<DanhMuc>(sql, new { ChiHienThi = chiHienThi });
        return rows.ToList();
    }

    public async Task<DanhMuc?> GetByMaAsync(string maDanhMuc)
    {
        const string sql = @"
            SELECT MaDanhMuc, TenDanhMuc, Ordering, HienThi
            FROM dbo.DanhMucs
            WHERE MaDanhMuc = @MaDanhMuc";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<DanhMuc>(sql, new { MaDanhMuc = maDanhMuc });
    }

    public async Task<bool> InsertAsync(DanhMuc danhMuc)
    {
        const string sql = @"
            INSERT INTO dbo.DanhMucs (MaDanhMuc, TenDanhMuc, Ordering, HienThi)
            VALUES (@MaDanhMuc, @TenDanhMuc, @Ordering, @HienThi)";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, danhMuc) > 0;
    }

    public async Task<bool> UpdateAsync(DanhMuc danhMuc)
    {
        const string sql = @"
            UPDATE dbo.DanhMucs
            SET TenDanhMuc = @TenDanhMuc, Ordering = @Ordering, HienThi = @HienThi
            WHERE MaDanhMuc = @MaDanhMuc";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, danhMuc) > 0;
    }

    public async Task<bool> DeleteAsync(string maDanhMuc)
    {
        const string sql = "DELETE FROM dbo.DanhMucs WHERE MaDanhMuc = @MaDanhMuc";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, new { MaDanhMuc = maDanhMuc }) > 0;
    }
}
