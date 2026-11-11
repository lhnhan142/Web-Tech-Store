using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class ThuongHieuRepository : RepositoryBase, IThuongHieuRepository
{
    public ThuongHieuRepository(DapperContext context) : base(context) { }

    public async Task<IReadOnlyList<ThuongHieu>> GetAllAsync(bool chiHienThi = true)
    {
        const string sql = @"
            SELECT MaThuongHieu, TenThuongHieu, Logo, HienThi
            FROM dbo.ThuongHieus
            WHERE (@ChiHienThi = 0 OR HienThi = 1)
            ORDER BY TenThuongHieu";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<ThuongHieu>(sql, new { ChiHienThi = chiHienThi });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ThuongHieu>> GetByDanhMucAsync(string maDanhMuc)
    {
        const string sql = @"
        SELECT DISTINCT th.MaThuongHieu, th.TenThuongHieu, th.Logo, th.HienThi
        FROM dbo.ThuongHieus th
        JOIN dbo.SanPhams sp ON sp.MaThuongHieu = th.MaThuongHieu
        WHERE sp.MaDanhMuc = @MaDanhMuc AND sp.HienThi = 1 AND th.HienThi = 1
        ORDER BY th.TenThuongHieu";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<ThuongHieu>(sql, new { MaDanhMuc = maDanhMuc });
        return rows.ToList();
    }

    public async Task<ThuongHieu?> GetByMaAsync(string maThuongHieu)
    {
        const string sql = @"
            SELECT MaThuongHieu, TenThuongHieu, Logo, HienThi
            FROM dbo.ThuongHieus
            WHERE MaThuongHieu = @MaThuongHieu";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ThuongHieu>(sql, new { MaThuongHieu = maThuongHieu });
    }

    public async Task<bool> InsertAsync(ThuongHieu thuongHieu)
    {
        const string sql = @"
            INSERT INTO dbo.ThuongHieus (MaThuongHieu, TenThuongHieu, Logo, HienThi)
            VALUES (@MaThuongHieu, @TenThuongHieu, @Logo, @HienThi)";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, thuongHieu) > 0;
    }

    public async Task<bool> UpdateAsync(ThuongHieu thuongHieu)
    {
        const string sql = @"
            UPDATE dbo.ThuongHieus
            SET TenThuongHieu = @TenThuongHieu, Logo = @Logo, HienThi = @HienThi
            WHERE MaThuongHieu = @MaThuongHieu";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, thuongHieu) > 0;
    }

    public async Task<bool> DeleteAsync(string maThuongHieu)
    {
        const string sql = "DELETE FROM dbo.ThuongHieus WHERE MaThuongHieu = @MaThuongHieu";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, new { MaThuongHieu = maThuongHieu }) > 0;
    }
}
