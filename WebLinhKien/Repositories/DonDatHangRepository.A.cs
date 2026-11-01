using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models;
using WebLinhKien.Models.Entities;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class DonDatHangRepository : RepositoryBase, IDonDatHangRepository
{
    private const string SelectDon = @"
        Uid, MaDon, TenKhachHang, Sdt, DiaChi, MaHinhThucThanhToan, TongTienHang, PhiGiaoHang,
        TongThanhToan, GhiChu, MaTinhTrang, NgayDat, NgayCapNhat";

    public DonDatHangRepository(DapperContext context) : base(context) { }

    public async Task<PagedResult<DonDatHang>> GetPagedAsync(string? maTinhTrang, string? tuKhoa, int page, int pageSize)
    {
        var like = string.IsNullOrWhiteSpace(tuKhoa) ? null : "%" + EscapeLike(tuKhoa.Trim()) + "%";
        var tinhTrang = string.IsNullOrWhiteSpace(maTinhTrang) ? null : maTinhTrang;

        const string sql = @"
            SELECT COUNT(*)
            FROM dbo.DonDatHangs
            WHERE (@MaTinhTrang IS NULL OR MaTinhTrang = @MaTinhTrang)
              AND (@Like IS NULL OR MaDon LIKE @Like OR TenKhachHang LIKE @Like OR Sdt LIKE @Like);

            SELECT " + SelectDon + @"
            FROM dbo.DonDatHangs
            WHERE (@MaTinhTrang IS NULL OR MaTinhTrang = @MaTinhTrang)
              AND (@Like IS NULL OR MaDon LIKE @Like OR TenKhachHang LIKE @Like OR Sdt LIKE @Like)
            ORDER BY NgayDat DESC, Uid DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = Context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            MaTinhTrang = tinhTrang,
            Like = like,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<DonDatHang>()).ToList();
        return new PagedResult<DonDatHang>(items, page, pageSize, total);
    }

    public async Task<DonDatHang?> GetByUidAsync(int uid)
    {
        const string sql = "SELECT " + SelectDon + " FROM dbo.DonDatHangs WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<DonDatHang>(sql, new { Uid = uid });
    }

    public async Task<IReadOnlyList<ChiTietDonHang>> GetChiTietAsync(int uidDonHang)
    {
        const string sql = @"
            SELECT Uid, UidDonHang, UidSanPham, TenSanPham, DonGia, SoLuong, ThanhTien
            FROM dbo.ChiTietDonHangs
            WHERE UidDonHang = @UidDonHang
            ORDER BY Uid";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<ChiTietDonHang>(sql, new { UidDonHang = uidDonHang });
        return rows.ToList();
    }

    private sealed class DemRow
    {
        public string MaTinhTrang { get; set; } = string.Empty;
        public int SoDon { get; set; }
    }

    public async Task<Dictionary<string, int>> DemTheoTinhTrangAsync()
    {
        const string sql = @"
            SELECT MaTinhTrang, COUNT(*) AS SoDon
            FROM dbo.DonDatHangs
            GROUP BY MaTinhTrang";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<DemRow>(sql);
        return rows.ToDictionary(r => r.MaTinhTrang, r => r.SoDon);
    }

    public async Task<bool> UpdateTinhTrangAsync(int uid, string tinhTrangCu, string tinhTrangMoi)
    {
        const string sql = @"
            UPDATE dbo.DonDatHangs
            SET MaTinhTrang = @Moi, NgayCapNhat = SYSDATETIME()
            WHERE Uid = @Uid AND MaTinhTrang = @Cu";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, new { Uid = uid, Cu = tinhTrangCu, Moi = tinhTrangMoi }) > 0;
    }

    // Tìm kiếm LIKE: thoát các ký tự đặc biệt để người dùng gõ % hoặc _ cũng được coi như chữ thường
    private static string EscapeLike(string s) =>
        s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
