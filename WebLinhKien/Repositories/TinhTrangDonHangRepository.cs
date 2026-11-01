using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class TinhTrangDonHangRepository : RepositoryBase, ITinhTrangDonHangRepository
{
    public TinhTrangDonHangRepository(DapperContext context) : base(context) { }

    public async Task<IReadOnlyList<TinhTrangDonHang>> GetAllAsync()
    {
        const string sql = @"
            SELECT MaTinhTrang, TenTinhTrang, Ordering
            FROM dbo.TinhTrangDonHangs
            ORDER BY Ordering";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<TinhTrangDonHang>(sql);
        return rows.ToList();
    }
}
