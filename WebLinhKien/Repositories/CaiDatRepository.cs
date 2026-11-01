using System.Globalization;
using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class CaiDatRepository : RepositoryBase, ICaiDatRepository
{
    public CaiDatRepository(DapperContext context) : base(context) { }

    public async Task<IReadOnlyList<CaiDat>> GetAllAsync()
    {
        const string sql = "SELECT Uid, Khoa, GiaTri, MoTa FROM dbo.CaiDats ORDER BY Khoa";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<CaiDat>(sql);
        return rows.ToList();
    }

    public async Task<string?> GetGiaTriAsync(string khoa)
    {
        const string sql = "SELECT GiaTri FROM dbo.CaiDats WHERE Khoa = @Khoa";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<string>(sql, new { Khoa = khoa });
    }

    public async Task<int> GetIntAsync(string khoa, int macDinh = 0)
    {
        var s = await GetGiaTriAsync(khoa);
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : macDinh;
    }

    public async Task<decimal> GetDecimalAsync(string khoa, decimal macDinh = 0)
    {
        var s = await GetGiaTriAsync(khoa);
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : macDinh;
    }

    public async Task SetAsync(string khoa, string giaTri)
    {
        const string sql = @"
            UPDATE dbo.CaiDats SET GiaTri = @GiaTri WHERE Khoa = @Khoa;
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.CaiDats (Khoa, GiaTri) VALUES (@Khoa, @GiaTri);";

        using var conn = Context.CreateConnection();
        await conn.ExecuteAsync(sql, new { Khoa = khoa, GiaTri = giaTri });
    }
}
