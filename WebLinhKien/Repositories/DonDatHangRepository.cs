using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Models;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public class DonDatHangRepository : RepositoryBase, IDonDatHangRepository
{
    // ===================== Đọc đơn và đổi tình trạng =====================

    private const string SelectDon = @"
        Uid, MaDon, TenKhachHang, Sdt, DiaChi, MaHinhThucThanhToan, TongTienHang, PhiGiaoHang,
        TongThanhToan, GhiChu, MaTinhTrang, NgayDat, NgayCapNhat";

    public DonDatHangRepository(DapperContext context) : base(context) { }

    public async Task<PagedResult<DonDatHang>> GetPagedAsync(string? maTinhTrang, string? tuKhoa, int page, int pageSize)
    {
        var like = string.IsNullOrWhiteSpace(tuKhoa) ? null : "%" + SqlHelper.EscapeLike(tuKhoa.Trim()) + "%";
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



    // ===================== Tạo đơn (transaction) và tra đơn theo mã + SĐT =====================

    public async Task<TaoDonKetQua> TaoDonAsync(DonHangMoi don, IReadOnlyList<DongHangMoi> dong)
    {
        await using var conn = Context.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var tonKhoSau = new Dictionary<int, int>();

        // Trừ kho nguyên tử: câu UPDATE chỉ thành công khi còn đủ hàng, nên hai khách đặt cùng lúc
        // không thể làm tồn kho bị âm. Duyệt theo Uid tăng dần để mọi giao dịch khóa cùng một thứ tự (tránh deadlock).
        const string truKho = @"
            UPDATE dbo.SanPhams
            SET TonKho = TonKho - @SoLuong, NgayCapNhat = SYSDATETIME()
            OUTPUT INSERTED.TonKho
            WHERE Uid = @Uid AND TonKho >= @SoLuong;";

        foreach (var d in dong.OrderBy(x => x.UidSanPham))
        {
            var con = await conn.QuerySingleOrDefaultAsync<int?>(truKho, new { Uid = d.UidSanPham, d.SoLuong }, tx);
            if (con is null)
            {
                await tx.RollbackAsync();
                return TaoDonKetQua.ThieuHang(d.TenSanPham);
            }
            tonKhoSau[d.UidSanPham] = con.Value;
        }

        const string themDon = @"
            INSERT INTO dbo.DonDatHangs
                (MaDon, TenKhachHang, Sdt, DiaChi, MaHinhThucThanhToan, TongTienHang, PhiGiaoHang, TongThanhToan, GhiChu, MaTinhTrang)
            OUTPUT INSERTED.Uid
            VALUES
                (@MaDon, @TenKhachHang, @Sdt, @DiaChi, @MaHinhThucThanhToan, @TongTienHang, @PhiGiaoHang, @TongThanhToan, @GhiChu, @MaTinhTrang);";

        var uidDon = await conn.ExecuteScalarAsync<int>(themDon, new
        {
            don.MaDon, don.TenKhachHang, don.Sdt, don.DiaChi, don.MaHinhThucThanhToan,
            don.TongTienHang, don.PhiGiaoHang, don.TongThanhToan, don.GhiChu,
            MaTinhTrang = TinhTrangDonHangFlow.Moi
        }, tx);

        const string themChiTiet = @"
            INSERT INTO dbo.ChiTietDonHangs (UidDonHang, UidSanPham, TenSanPham, DonGia, SoLuong)
            VALUES (@UidDonHang, @UidSanPham, @TenSanPham, @DonGia, @SoLuong);";

        await conn.ExecuteAsync(themChiTiet,
            dong.Select(d => new { UidDonHang = uidDon, d.UidSanPham, d.TenSanPham, d.DonGia, d.SoLuong }), tx);

        await tx.CommitAsync();
        return TaoDonKetQua.Thanh(tonKhoSau);
    }

    public async Task<DonDatHang?> GetByMaDonVaSdtAsync(string maDon, string sdt)
    {
        const string sql = "SELECT " + SelectDon + " FROM dbo.DonDatHangs WHERE MaDon = @MaDon AND Sdt = @Sdt";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<DonDatHang>(sql, new { MaDon = maDon, Sdt = sdt });
    }
}
