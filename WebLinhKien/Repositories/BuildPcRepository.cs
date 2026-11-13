using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public class BuildPcRepository : RepositoryBase, IBuildPcRepository
{
    // Không lấy MoTa (HTML dài) vì trang Build PC chỉ cần thông tin ngắn.
    private const string CotSanPham = @"
        Uid, MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
        AnhDaiDien, BaoHanhThang, NoiBat, HienThi";

    private const string CotThongSo = @"
        t.Uid, t.UidSanPham, t.TenThongSo, t.GiaTri, t.Ordering";

    public BuildPcRepository(DapperContext context) : base(context) { }

    public async Task<IReadOnlyList<SanPham>> GetByCategoriesAsync(IReadOnlyCollection<string> maDanhMuc)
    {
        if (maDanhMuc.Count == 0) return Array.Empty<SanPham>();

        var sql = $@"
            SELECT {CotSanPham}
            FROM dbo.SanPhams
            WHERE HienThi = 1 AND MaDanhMuc IN @DanhMucs
            ORDER BY MaDanhMuc, GiaBan, Uid;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { DanhMucs = maDanhMuc });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ThongSoKyThuat>> GetThongSoTheoDanhMucAsync(IReadOnlyCollection<string> maDanhMuc)
    {
        if (maDanhMuc.Count == 0) return Array.Empty<ThongSoKyThuat>();

        var sql = $@"
            SELECT {CotThongSo}
            FROM dbo.ThongSoKyThuats t
            INNER JOIN dbo.SanPhams s ON s.Uid = t.UidSanPham
            WHERE s.HienThi = 1 AND s.MaDanhMuc IN @DanhMucs
            ORDER BY t.UidSanPham, t.Ordering, t.Uid;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<ThongSoKyThuat>(sql, new { DanhMucs = maDanhMuc });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SanPham>> GetByUidsAsync(IReadOnlyCollection<int> uids)
    {
        if (uids.Count == 0) return Array.Empty<SanPham>();

        var sql = $@"
            SELECT {CotSanPham}
            FROM dbo.SanPhams
            WHERE HienThi = 1 AND Uid IN @Uids;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { Uids = uids });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ThongSoKyThuat>> GetThongSoTheoUidsAsync(IReadOnlyCollection<int> uids)
    {
        if (uids.Count == 0) return Array.Empty<ThongSoKyThuat>();

        var sql = $@"
            SELECT {CotThongSo}
            FROM dbo.ThongSoKyThuats t
            WHERE t.UidSanPham IN @Uids
            ORDER BY t.UidSanPham, t.Ordering, t.Uid;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<ThongSoKyThuat>(sql, new { Uids = uids });
        return rows.ToList();
    }
}
