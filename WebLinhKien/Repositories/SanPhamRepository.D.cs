using Dapper;
using WebLinhKien.Models;
using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories;

public partial class SanPhamRepository
{
    // Danh sách không cần cột MoTa (HTML dài)
    private const string CotDanhSach = @"
        Uid, MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
        AnhDaiDien, MoTaNgan, BaoHanhThang, NoiBat, HienThi, NgayTao, NgayCapNhat";

    private const string InsertSanPhamSql = @"
        INSERT INTO dbo.SanPhams
            (MaSanPham, TenSanPham, MaDanhMuc, MaThuongHieu, GiaGoc, GiaBan, TonKho,
             AnhDaiDien, MoTaNgan, MoTa, BaoHanhThang, NoiBat, HienThi)
        OUTPUT INSERTED.Uid
        VALUES
            (@MaSanPham, @TenSanPham, @MaDanhMuc, @MaThuongHieu, @GiaGoc, @GiaBan, @TonKho,
             @AnhDaiDien, @MoTaNgan, @MoTa, @BaoHanhThang, @NoiBat, @HienThi)";

    private const string InsertThongSoSql = @"
        INSERT INTO dbo.ThongSoKyThuats (UidSanPham, TenThongSo, GiaTri, Ordering)
        VALUES (@UidSanPham, @TenThongSo, @GiaTri, @Ordering)";

    // Giống DonDatHangRepository: thoát ký tự đặc biệt để gõ % hoặc _ vẫn tìm như chữ thường
    private static string EscapeLike(string s) =>
        s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    public async Task<PagedResult<SanPham>> GetPagedAdminAsync(
        string? tuKhoa, string? maDanhMuc, bool chiSapHet, int nguongSapHet, int page, int pageSize)
    {
        var like = string.IsNullOrWhiteSpace(tuKhoa) ? null : "%" + EscapeLike(tuKhoa.Trim()) + "%";
        var danhMuc = string.IsNullOrWhiteSpace(maDanhMuc) ? null : maDanhMuc;

        const string dieuKien = @"
            WHERE (@Like IS NULL OR TenSanPham LIKE @Like OR MaSanPham LIKE @Like)
              AND (@MaDanhMuc IS NULL OR MaDanhMuc = @MaDanhMuc)
              AND (@ChiSapHet = 0 OR TonKho <= @Nguong)";

        const string sql =
            "SELECT COUNT(*) FROM dbo.SanPhams " + dieuKien + ";" +
            "SELECT " + CotDanhSach + " FROM dbo.SanPhams " + dieuKien + @"
            ORDER BY NgayTao DESC, Uid DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = Context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            Like = like,
            MaDanhMuc = danhMuc,
            ChiSapHet = chiSapHet,
            Nguong = nguongSapHet,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<SanPham>()).ToList();
        return new PagedResult<SanPham>(items, page, pageSize, total);
    }

    public async Task<int> DemSapHetAsync(int nguongSapHet)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.SanPhams WHERE TonKho <= @Nguong";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { Nguong = nguongSapHet });
    }

    public async Task<bool> MaSanPhamTonTaiAsync(string maSanPham, int? boQuaUid = null)
    {
        const string sql = @"
            SELECT COUNT(1) FROM dbo.SanPhams
            WHERE MaSanPham = @MaSanPham AND (@BoQuaUid IS NULL OR Uid <> @BoQuaUid)";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { MaSanPham = maSanPham, BoQuaUid = boQuaUid }) > 0;
    }

    public async Task<int> InsertAsync(SanPham sanPham, IReadOnlyList<ThongSoKyThuat> thongSos, IReadOnlyList<string> anhPhus)
    {
        using var conn = Context.CreateConnection();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();   // không Commit thì tự rollback khi Dispose

        var uid = await conn.ExecuteScalarAsync<int>(InsertSanPhamSql, sanPham, tx);

        if (thongSos.Count > 0)
        {
            await conn.ExecuteAsync(InsertThongSoSql,
                thongSos.Select(t => new { UidSanPham = uid, t.TenThongSo, t.GiaTri, t.Ordering }), tx);
        }

        var stt = 0;
        foreach (var duongDan in anhPhus)
        {
            await conn.ExecuteAsync(
                "INSERT INTO dbo.HinhAnhSanPhams (UidSanPham, DuongDan, Ordering) VALUES (@UidSanPham, @DuongDan, @Ordering)",
                new { UidSanPham = uid, DuongDan = duongDan, Ordering = stt++ }, tx);
        }

        await tx.CommitAsync();
        return uid;
    }

    public async Task<bool> UpdateAsync(SanPham sanPham, IReadOnlyList<ThongSoKyThuat> thongSos)
    {
        const string sql = @"
            UPDATE dbo.SanPhams
            SET MaSanPham = @MaSanPham, TenSanPham = @TenSanPham, MaDanhMuc = @MaDanhMuc,
                MaThuongHieu = @MaThuongHieu, GiaGoc = @GiaGoc, GiaBan = @GiaBan, TonKho = @TonKho,
                AnhDaiDien = @AnhDaiDien, MoTaNgan = @MoTaNgan, MoTa = @MoTa, BaoHanhThang = @BaoHanhThang,
                NoiBat = @NoiBat, HienThi = @HienThi, NgayCapNhat = SYSDATETIME()
            WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var daSua = await conn.ExecuteAsync(sql, sanPham, tx) > 0;
        if (!daSua) return false;

        await conn.ExecuteAsync("DELETE FROM dbo.ThongSoKyThuats WHERE UidSanPham = @Uid", new { sanPham.Uid }, tx);
        if (thongSos.Count > 0)
        {
            await conn.ExecuteAsync(InsertThongSoSql,
                thongSos.Select(t => new { UidSanPham = sanPham.Uid, t.TenThongSo, t.GiaTri, t.Ordering }), tx);
        }

        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int uid)
    {
        using var conn = Context.CreateConnection();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        await conn.ExecuteAsync("DELETE FROM dbo.HinhAnhSanPhams WHERE UidSanPham = @Uid", new { Uid = uid }, tx);
        await conn.ExecuteAsync("DELETE FROM dbo.ThongSoKyThuats WHERE UidSanPham = @Uid", new { Uid = uid }, tx);
        var daXoa = await conn.ExecuteAsync("DELETE FROM dbo.SanPhams WHERE Uid = @Uid", new { Uid = uid }, tx) > 0;

        await tx.CommitAsync();
        return daXoa;
    }

    public async Task<int> DemTrongDonHangAsync(int uidSanPham)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.ChiTietDonHangs WHERE UidSanPham = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { Uid = uidSanPham });
    }

    public async Task<IReadOnlyList<ThongSoKyThuat>> GetThongSosAsync(int uidSanPham)
    {
        const string sql = @"
            SELECT Uid, UidSanPham, TenThongSo, GiaTri, Ordering
            FROM dbo.ThongSoKyThuats WHERE UidSanPham = @Uid ORDER BY Ordering, Uid";

        using var conn = Context.CreateConnection();
        return (await conn.QueryAsync<ThongSoKyThuat>(sql, new { Uid = uidSanPham })).ToList();
    }

    public async Task<IReadOnlyList<HinhAnhSanPham>> GetHinhAnhsAsync(int uidSanPham)
    {
        const string sql = @"
            SELECT Uid, UidSanPham, DuongDan, Ordering
            FROM dbo.HinhAnhSanPhams WHERE UidSanPham = @Uid ORDER BY Ordering, Uid";

        using var conn = Context.CreateConnection();
        return (await conn.QueryAsync<HinhAnhSanPham>(sql, new { Uid = uidSanPham })).ToList();
    }

    public async Task<HinhAnhSanPham?> GetHinhAnhAsync(int uid)
    {
        const string sql = "SELECT Uid, UidSanPham, DuongDan, Ordering FROM dbo.HinhAnhSanPhams WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<HinhAnhSanPham>(sql, new { Uid = uid });
    }

    public async Task<int> ThemHinhAnhAsync(int uidSanPham, string duongDan)
    {
        const string sql = @"
            INSERT INTO dbo.HinhAnhSanPhams (UidSanPham, DuongDan, Ordering)
            OUTPUT INSERTED.Uid
            SELECT @UidSanPham, @DuongDan, ISNULL(MAX(Ordering), -1) + 1
            FROM dbo.HinhAnhSanPhams WHERE UidSanPham = @UidSanPham";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { UidSanPham = uidSanPham, DuongDan = duongDan });
    }

    public async Task<bool> XoaHinhAnhAsync(int uid)
    {
        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM dbo.HinhAnhSanPhams WHERE Uid = @Uid", new { Uid = uid }) > 0;
    }

    public async Task<bool> DatAnhDaiDienAsync(int uidSanPham, string? duongDan)
    {
        const string sql = "UPDATE dbo.SanPhams SET AnhDaiDien = @DuongDan, NgayCapNhat = SYSDATETIME() WHERE Uid = @Uid";

        using var conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, new { Uid = uidSanPham, DuongDan = duongDan }) > 0;
    }
}
