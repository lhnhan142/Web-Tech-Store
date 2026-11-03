using Dapper;

namespace WebLinhKien.Repositories;

public partial class SanPhamRepository
{
    private sealed class DemRow
    {
        public string Ma { get; set; } = string.Empty;
        public int SoSanPham { get; set; }
    }

    public async Task<Dictionary<string, int>> DemTheoDanhMucAsync()
    {
        const string sql = @"
            SELECT MaDanhMuc AS Ma, COUNT(*) AS SoSanPham
            FROM dbo.SanPhams
            GROUP BY MaDanhMuc";
        return await DemAsync(sql);
    }

    public async Task<Dictionary<string, int>> DemTheoThuongHieuAsync()
    {
        const string sql = @"
            SELECT MaThuongHieu AS Ma, COUNT(*) AS SoSanPham
            FROM dbo.SanPhams
            WHERE MaThuongHieu IS NOT NULL
            GROUP BY MaThuongHieu";
        return await DemAsync(sql);
    }

    private async Task<Dictionary<string, int>> DemAsync(string sql)
    {
        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<DemRow>(sql);
        return rows.ToDictionary(r => r.Ma, r => r.SoSanPham, StringComparer.OrdinalIgnoreCase);
    }
}
