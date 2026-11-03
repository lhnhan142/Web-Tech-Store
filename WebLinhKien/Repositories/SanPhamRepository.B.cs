using Dapper;
using WebLinhKien.Models;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels.Client;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Repositories;

public partial class SanPhamRepository : RepositoryBase, ISanPhamRepository
{
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

        var likePattern = string.IsNullOrWhiteSpace(filter.Q)
            ? null
            : "%" + EscapeLike(filter.Q.Trim()) + "%";

        var sql = $@"
            SELECT COUNT(1)
            FROM dbo.SanPhams
            WHERE HienThi = 1
              AND (@Like IS NULL OR MaSanPham LIKE @Like OR TenSanPham LIKE @Like)
              AND (@DanhMuc IS NULL OR MaDanhMuc = @DanhMuc)
              AND (@ThuongHieu IS NULL OR MaThuongHieu = @ThuongHieu)
              AND (@MinPrice IS NULL OR GiaBan >= @MinPrice)
              AND (@MaxPrice IS NULL OR GiaBan <= @MaxPrice)
              AND (@ConHang IS NULL OR (@ConHang = 1 AND TonKho > 0) OR (@ConHang = 0));

            SELECT {SelectColumns}
            FROM dbo.SanPhams
            WHERE HienThi = 1
              AND (@Like IS NULL OR MaSanPham LIKE @Like OR TenSanPham LIKE @Like)
              AND (@DanhMuc IS NULL OR MaDanhMuc = @DanhMuc)
              AND (@ThuongHieu IS NULL OR MaThuongHieu = @ThuongHieu)
              AND (@MinPrice IS NULL OR GiaBan >= @MinPrice)
              AND (@MaxPrice IS NULL OR GiaBan <= @MaxPrice)
              AND (@ConHang IS NULL OR (@ConHang = 1 AND TonKho > 0) OR (@ConHang = 0))
            ORDER BY {orderByClause}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var parameters = new
        {
            Like = likePattern,
            DanhMuc = filter.DanhMuc,
            ThuongHieu = filter.ThuongHieu,
            MinPrice = filter.MinPrice,
            MaxPrice = filter.MaxPrice,
            ConHang = filter.ConHang,
            Offset = (filter.Page - 1) * filter.PageSize,
            PageSize = filter.PageSize
        };

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

    public async Task<IReadOnlyList<SanPham>> SearchSuggestionsAsync(string keyword, int limit = 6)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Array.Empty<SanPham>();

        var like = "%" + EscapeLike(keyword.Trim()) + "%";
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

    private static string EscapeLike(string s) =>
        s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
