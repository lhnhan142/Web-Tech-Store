using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class SanPhamRepository : RepositoryBase, ISanPhamRepository
{
    // Danh sách cột dùng chung để các method (kể cả file partial của B, D) lấy giống nhau.
    protected const string SelectColumns = @"
        Uid, MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
        AnhDaiDien, MoTaNgan, MoTa, BaoHanhThang, NoiBat, HienThi, NgayTao, NgayCapNhat";

    public SanPhamRepository(DapperContext context) : base(context) { }

    public async Task<SanPham?> GetByUidAsync(int uid)
    {
        const string sql = "SELECT " + SelectColumns + " FROM dbo.SanPhams WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SanPham>(sql, new { Uid = uid });
    }

    public async Task<SanPham?> GetByMaAsync(string maSanPham)
    {
        const string sql = "SELECT " + SelectColumns + " FROM dbo.SanPhams WHERE MaSanPham = @MaSanPham";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<SanPham>(sql, new { MaSanPham = maSanPham });
    }

    public async Task<IReadOnlyList<SanPham>> GetByUidsAsync(IEnumerable<int> uids)
    {
        var list = uids.Distinct().ToList();
        if (list.Count == 0) return Array.Empty<SanPham>();

        const string sql = "SELECT " + SelectColumns + " FROM dbo.SanPhams WHERE Uid IN @Uids";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { Uids = list });   // Dapper tự mở rộng IN
        return rows.ToList();
    }
}
