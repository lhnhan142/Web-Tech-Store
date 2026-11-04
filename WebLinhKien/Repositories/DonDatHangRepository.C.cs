using Dapper;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels;

namespace WebLinhKien.Repositories;

public partial class DonDatHangRepository
{
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
