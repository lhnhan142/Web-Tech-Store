using System.Text;
using Dapper;
using WebLinhKien.Data;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels.Client;
using WebLinhKien.Models;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public class SanPhamRepository : RepositoryBase, ISanPhamRepository
{
    // ===================== Dùng chung: đọc theo khóa =====================

    // Danh sách cột dùng chung cho các method lấy sản phẩm đầy đủ.
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

    // ===================== Thống kê: đếm theo danh mục/thương hiệu =====================

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

    public async Task<IReadOnlyList<ThuongHieuFilterItem>> GetBrandsByCategoryAsync(string maDanhMuc)
    {
        if (string.IsNullOrWhiteSpace(maDanhMuc))
            return Array.Empty<ThuongHieuFilterItem>();

        const string sql = @"
            SELECT 
                t.MaThuongHieu, 
                t.TenThuongHieu, 
                COUNT(s.Uid) AS SoLuongSanPham
            FROM dbo.ThuongHieus t
            INNER JOIN dbo.SanPhams s ON t.MaThuongHieu = s.MaThuongHieu
            WHERE s.MaDanhMuc = @MaDanhMuc
              AND s.HienThi = 1
              AND t.HienThi = 1
            GROUP BY t.MaThuongHieu, t.TenThuongHieu
            ORDER BY t.TenThuongHieu ASC;";

        using var conn = Context.CreateConnection();
        var rows = (await conn.QueryAsync<ThuongHieuFilterItem>(sql, new { MaDanhMuc = maDanhMuc.Trim() })).ToList();
        return rows.OrderBy(r => r.TenThuongHieu).ToList();
    }

    private async Task<Dictionary<string, int>> DemAsync(string sql)
    {
        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<DemRow>(sql);
        return rows.ToDictionary(r => r.Ma, r => r.SoSanPham, StringComparer.OrdinalIgnoreCase);
    }

    // ===================== Trang khách: nổi bật, lọc, tìm kiếm, liên quan =====================

    public async Task<IReadOnlyList<SanPham>> GetFeaturedProductsAsync(int limit = 8)
    {
        var sql = $@"
            SELECT TOP (@Limit) {SelectColumns}
            FROM dbo.SanPhams
            WHERE HienThi = 1 AND NoiBat = 1
            ORDER BY NgayTao DESC, Uid DESC;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { Limit = limit });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SanPham>> GetNewArrivalsAsync(int limit = 8)
    {
        var sql = $@"
            SELECT TOP (@Limit) {SelectColumns}
            FROM dbo.SanPhams
            WHERE HienThi = 1
            ORDER BY NgayTao DESC, Uid DESC;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { Limit = limit });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SanPham>> GetBestDealsAsync(int limit = 8)
    {
        var sql = $@"
            SELECT TOP (@Limit) {SelectColumns}
            FROM dbo.SanPhams
            WHERE HienThi = 1 AND GiaGoc > GiaBan AND GiaBan > 0
            ORDER BY (GiaGoc - GiaBan) DESC, Uid DESC;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { Limit = limit });
        return rows.ToList();
    }

    // Điều kiện lọc chung (từ khóa, danh mục, thương hiệu, giá, tồn kho). Bảng SanPhams có bí danh "sp".
    private const string FilterWhere = @"
            sp.HienThi = 1
              AND (@Like IS NULL OR sp.MaSanPham LIKE @Like OR sp.TenSanPham LIKE @Like)
              AND (@DanhMuc IS NULL OR sp.MaDanhMuc = @DanhMuc)
              AND (@ThuongHieu IS NULL OR sp.MaThuongHieu = @ThuongHieu)
              AND (@MinPrice IS NULL OR sp.GiaBan >= @MinPrice)
              AND (@MaxPrice IS NULL OR sp.GiaBan <= @MaxPrice)
              AND (@ConHang IS NULL OR (@ConHang = 1 AND sp.TonKho > 0) OR (@ConHang = 0))";

    private static DynamicParameters CreateFilterParameters(ProductFilterRequest filter)
    {
        var likePattern = string.IsNullOrWhiteSpace(filter.Q)
            ? null
            : "%" + SqlHelper.EscapeLike(filter.Q.Trim()) + "%";

        return new DynamicParameters(new
        {
            Like = likePattern,
            DanhMuc = filter.DanhMuc,
            ThuongHieu = filter.ThuongHieu,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            ConHang = filter.ConHang
        });
    }

    // Sinh điều kiện lọc theo thông số: giữa các nhóm là AND, trong một nhóm là OR.
    // skipKey: bỏ qua một nhóm (dùng khi đếm số lượng cho chính nhóm đó).
    private static string BuildSpecSql(ProductFilterRequest filter, DynamicParameters parameters, string? skipKey)
    {
        var sb = new StringBuilder();
        var groupIndex = 0;

        foreach (var group in SpecFilterDefinitions.ForCategory(filter.DanhMuc))
        {
            groupIndex++;
            if (skipKey != null && group.Key == skipKey) continue;

            var chosen = filter.GetSpecValues(group.Key).ToList();
            var options = group.Options
                .Where(o => chosen.Any(c => c.Equals(o.Value, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (options.Count == 0) continue;

            parameters.Add("sn" + groupIndex, group.TenThongSo);

            var likes = new List<string>();
            var n = 0;
            foreach (var option in options)
            {
                foreach (var pattern in option.Patterns)
                {
                    var name = "sv" + groupIndex + "_" + n++;
                    parameters.Add(name, pattern);
                    likes.Add("t.GiaTri LIKE @" + name);
                }
            }

            sb.Append(" AND EXISTS (SELECT 1 FROM dbo.ThongSoKyThuats t WHERE t.UidSanPham = sp.Uid AND t.TenThongSo = @sn")
              .Append(groupIndex)
              .Append(" AND (")
              .Append(string.Join(" OR ", likes))
              .Append("))");
        }

        return sb.ToString();
    }

    public async Task<PagedResult<SanPham>> GetPagedProductsAsync(ProductFilterRequest filter)
    {
        filter.Normalize();

        var orderByClause = filter.Sort switch
        {
            "gia-tang" => "GiaBan ASC, Uid DESC",
            "gia-giam" => "GiaBan DESC, Uid DESC",
            "moi-nhat" => "NgayTao DESC, Uid DESC",
            "ten-az"   => "TenSanPham ASC, Uid DESC",
            _          => "NoiBat DESC, NgayTao DESC, Uid DESC"
        };

        var parameters = CreateFilterParameters(filter);
        var specSql = BuildSpecSql(filter, parameters, null);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);
        parameters.Add("PageSize", filter.PageSize);

        var sql = $@"
            SELECT COUNT(1)
            FROM dbo.SanPhams sp
            WHERE {FilterWhere}{specSql};

            SELECT {SelectColumns}
            FROM dbo.SanPhams sp
            WHERE {FilterWhere}{specSql}
            ORDER BY {orderByClause}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = Context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, parameters);

        var totalItems = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<SanPham>()).ToList();

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);
        if (totalPages > 0 && filter.Page > totalPages)
        {
            filter.Page = totalPages;
            return await GetPagedProductsAsync(filter);
        }

        return new PagedResult<SanPham>(items, filter.Page, filter.PageSize, totalItems);
    }

    // Đếm số sản phẩm cho từng lựa chọn lọc thông số. Mỗi nhóm được đếm theo các bộ lọc KHÁC
    // (không tính lựa chọn của chính nhóm đó) để khách thấy chọn thêm một mục sẽ ra bao nhiêu sản phẩm.
    public async Task<Dictionary<string, int>> GetSpecFacetsAsync(ProductFilterRequest filter)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        filter.Normalize();

        var groups = SpecFilterDefinitions.ForCategory(filter.DanhMuc);
        if (groups.Count == 0) return result;

        using var conn = Context.CreateConnection();
        foreach (var group in groups)
        {
            var parameters = CreateFilterParameters(filter);
            var otherSpecs = BuildSpecSql(filter, parameters, group.Key);
            parameters.Add("fTen", group.TenThongSo);

            var inner = new List<string>();
            var outer = new List<string>();
            for (var i = 0; i < group.Options.Count; i++)
            {
                var likes = new List<string>();
                var patterns = group.Options[i].Patterns;
                for (var j = 0; j < patterns.Length; j++)
                {
                    var name = "fp" + i + "_" + j;
                    parameters.Add(name, patterns[j]);
                    likes.Add("t.GiaTri LIKE @" + name);
                }

                // SQL Server không cho SUM bọc trực tiếp EXISTS, nên tính cờ 0/1 ở truy vấn con rồi mới cộng ở ngoài
                inner.Add("CASE WHEN EXISTS (SELECT 1 FROM dbo.ThongSoKyThuats t WHERE t.UidSanPham = sp.Uid AND t.TenThongSo = @fTen AND ("
                          + string.Join(" OR ", likes) + ")) THEN 1 ELSE 0 END AS F" + i);
                outer.Add("COALESCE(SUM(x.F" + i + "), 0) AS C" + i);
            }

            var sql = "SELECT " + string.Join(", ", outer)
                      + " FROM (SELECT " + string.Join(", ", inner)
                      + " FROM dbo.SanPhams sp WHERE " + FilterWhere + otherSpecs + ") x;";
            var row = (IDictionary<string, object>)await conn.QuerySingleAsync(sql, parameters);

            for (var i = 0; i < group.Options.Count; i++)
            {
                var value = row["C" + i];
                result[group.Key + ":" + group.Options[i].Value] = value == null || value is DBNull ? 0 : Convert.ToInt32(value);
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<SanPham>> SearchSuggestionsAsync(string keyword, int limit = 6)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Array.Empty<SanPham>();

        var like = "%" + SqlHelper.EscapeLike(keyword.Trim()) + "%";
        var sql = $@"
            SELECT TOP (@Limit) {SelectColumns}
            FROM dbo.SanPhams
            WHERE HienThi = 1
              AND (MaSanPham LIKE @Like OR TenSanPham LIKE @Like)
            ORDER BY NoiBat DESC, NgayTao DESC;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { Like = like, Limit = limit });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<HinhAnhSanPham>> GetProductImagesAsync(int uidSanPham)
    {
        const string sql = @"
            SELECT Uid, UidSanPham, DuongDan, Ordering
            FROM dbo.HinhAnhSanPhams
            WHERE UidSanPham = @UidSanPham
            ORDER BY Ordering ASC, Uid ASC;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<HinhAnhSanPham>(sql, new { UidSanPham = uidSanPham });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ThongSoKyThuat>> GetProductSpecificationsAsync(int uidSanPham)
    {
        const string sql = @"
            SELECT Uid, UidSanPham, TenThongSo, GiaTri, Ordering
            FROM dbo.ThongSoKyThuats
            WHERE UidSanPham = @UidSanPham
            ORDER BY Ordering ASC, Uid ASC;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<ThongSoKyThuat>(sql, new { UidSanPham = uidSanPham });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SanPham>> GetRelatedProductsAsync(string maDanhMuc, int excludeUid, int limit = 4)
    {
        var sql = $@"
            SELECT TOP (@Limit) {SelectColumns}
            FROM dbo.SanPhams
            WHERE HienThi = 1
              AND MaDanhMuc = @MaDanhMuc
              AND Uid <> @ExcludeUid
            ORDER BY NoiBat DESC, NgayTao DESC;";

        using var conn = Context.CreateConnection();
        var rows = await conn.QueryAsync<SanPham>(sql, new { MaDanhMuc = maDanhMuc, ExcludeUid = excludeUid, Limit = limit });
        return rows.ToList();
    }



    // ===================== Trang quản trị: danh sách, thêm, sửa, xóa =====================

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


    public async Task<PagedResult<SanPham>> GetPagedAdminAsync(
        string? tuKhoa, string? maDanhMuc, bool chiSapHet, int nguongSapHet, int page, int pageSize)
    {
        var like = string.IsNullOrWhiteSpace(tuKhoa) ? null : "%" + SqlHelper.EscapeLike(tuKhoa.Trim()) + "%";
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
