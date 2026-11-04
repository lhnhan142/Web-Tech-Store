using Microsoft.Data.SqlClient;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Services;

/// <summary>
/// Nghiệp vụ đặt hàng: tải sản phẩm từ database, kiểm tra tồn kho, TÍNH LẠI giá và phí giao hàng ở server,
/// lưu đơn + trừ kho trong một transaction, rồi báo cho trang quản trị (WebSocket).
/// </summary>
public class DatHangService : IDatHangService
{
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly ICaiDatRepository _caiDatRepo;
    private readonly IDonDatHangRepository _donRepo;
    private readonly IAdminNotifier _notifier;
    private readonly ILogger<DatHangService> _logger;

    public DatHangService(
        ISanPhamRepository sanPhamRepo,
        ICaiDatRepository caiDatRepo,
        IDonDatHangRepository donRepo,
        IAdminNotifier notifier,
        ILogger<DatHangService> logger)
    {
        _sanPhamRepo = sanPhamRepo;
        _caiDatRepo = caiDatRepo;
        _donRepo = donRepo;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<DatHangKetQua> DatHangAsync(DatHangRequest request)
    {
        // 1. Tên, giá, tồn kho luôn lấy từ database, không tin dữ liệu trình duyệt gửi lên
        var sanPhams = await _sanPhamRepo.GetByUidsAsync(request.Items.Select(i => i.UidSanPham));

        var dong = new List<DongHangMoi>();
        var thieuHang = new List<string>();
        foreach (var item in request.Items)
        {
            var sp = sanPhams.FirstOrDefault(s => s.Uid == item.UidSanPham);
            if (sp is null || !sp.HienThi)
                return Loi(400, "Có sản phẩm không còn được bán. Vui lòng tải lại trang để cập nhật giỏ hàng.");

            if (sp.TonKho < item.SoLuong) thieuHang.Add(sp.TenSanPham);
            dong.Add(new DongHangMoi { UidSanPham = sp.Uid, TenSanPham = sp.TenSanPham, DonGia = sp.GiaBan, SoLuong = item.SoLuong });
        }
        if (thieuHang.Count > 0)
            return Loi(409, "Không đủ hàng trong kho: " + string.Join(", ", thieuHang) + ".");

        // 2. Tính tiền ở server (phí giao hàng và ngưỡng miễn phí lấy từ CaiDats)
        var tongTienHang = dong.Sum(d => d.DonGia * d.SoLuong);
        var phiGoc = await _caiDatRepo.GetDecimalAsync(CaiDatKeys.PhiGiaoHang, 30000);
        var nguong = await _caiDatRepo.GetDecimalAsync(CaiDatKeys.NguongMienPhiGiaoHang, 500000);
        var phiGiao = tongTienHang >= nguong ? 0 : phiGoc;
        var tongThanhToan = tongTienHang + phiGiao;

        // 3. Lưu đơn + trừ kho trong một transaction. MaDon có ràng buộc UNIQUE nên nếu trùng (hiếm) thì thử lại.
        string? maDon = null;
        TaoDonKetQua? kq = null;
        for (var lan = 1; lan <= 3 && kq is null; lan++)
        {
            maDon = TaoMaDon();
            var don = new DonHangMoi
            {
                MaDon = maDon,
                TenKhachHang = request.TenKhachHang,
                Sdt = request.Sdt,
                DiaChi = request.DiaChi,
                MaHinhThucThanhToan = request.MaHinhThucThanhToan,
                TongTienHang = tongTienHang,
                PhiGiaoHang = phiGiao,
                TongThanhToan = tongThanhToan,
                GhiChu = request.GhiChu
            };

            try
            {
                kq = await _donRepo.TaoDonAsync(don, dong);
            }
            catch (SqlException ex) when ((ex.Number == 2601 || ex.Number == 2627) && lan < 3)
            {
                _logger.LogWarning("Trùng mã đơn {MaDon}, thử lại lần {Lan}", maDon, lan);
                await Task.Delay(5);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không lưu được đơn hàng {MaDon}", maDon);
                return Loi(500, "Hệ thống chưa lưu được đơn hàng. Bạn vui lòng thử lại sau ít phút.");
            }
        }

        if (kq is null || maDon is null)
            return Loi(500, "Hệ thống chưa lưu được đơn hàng. Bạn vui lòng thử lại sau ít phút.");
        if (!kq.Ok)
            return Loi(409, $"Sản phẩm \"{kq.SanPhamThieu}\" vừa hết hàng hoặc không đủ số lượng. Có thể vừa có khách khác đặt trước bạn.");

        // 4. Báo cho trang quản trị. Lỗi ở bước này KHÔNG được làm hỏng đơn đã lưu.
        await ThongBaoAsync(maDon, request.TenKhachHang, tongThanhToan, dong, kq);

        var chuyenKhoan = request.MaHinhThucThanhToan == WebConstants.HinhThucChuyenKhoan;
        var nganHang = chuyenKhoan ? await DocNganHangAsync() : null;

        var message = $"Đặt hàng thành công! Mã đơn của bạn là {maDon}. ";
        message += chuyenKhoan && nganHang is null
            ? "Cửa hàng sẽ liên hệ để hướng dẫn bạn thanh toán."
            : "Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.";

        return new DatHangKetQua
        {
            Status = 200,
            Message = message,
            MaDon = maDon,
            TongThanhToan = tongThanhToan,
            NganHang = nganHang
        };
    }

    public async Task<byte[]?> TaoQrPngAsync(string maDon, string sdt)
    {
        var don = await _donRepo.GetByMaDonVaSdtAsync(maDon, sdt);
        if (don is null || don.MaHinhThucThanhToan != WebConstants.HinhThucChuyenKhoan) return null;

        var nganHang = await DocNganHangAsync();
        if (nganHang is null) return null;

        try
        {
            var noiDung = VietQr.TaoNoiDung(nganHang.MaNganHang, nganHang.SoTaiKhoan, (long)decimal.Round(don.TongThanhToan), don.MaDon);
            return VietQr.TaoPng(noiDung);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không tạo được mã QR cho đơn {MaDon}", maDon);
            return null;
        }
    }

    // ---------- hàm phụ ----------

    private async Task ThongBaoAsync(string maDon, string tenKhachHang, decimal tong,
        List<DongHangMoi> dong, TaoDonKetQua kq)
    {
        try
        {
            await _notifier.NotifyNewOrderAsync(maDon, tenKhachHang, tong);

            var nguongSapHet = await _caiDatRepo.GetIntAsync(CaiDatKeys.NguongSapHetHang, 5);
            foreach (var d in dong)
            {
                if (kq.TonKhoSau.TryGetValue(d.UidSanPham, out var con) && con <= nguongSapHet)
                    await _notifier.NotifyLowStockAsync(d.UidSanPham, d.TenSanPham, con);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được thông báo cho admin (đơn {MaDon} vẫn đã lưu)", maDon);
        }
    }

    private async Task<NganHangInfo?> DocNganHangAsync()
    {
        var ma = (await _caiDatRepo.GetGiaTriAsync(VietQr.KeyMaNganHang))?.Trim();
        var so = (await _caiDatRepo.GetGiaTriAsync(VietQr.KeySoTaiKhoan))?.Trim();
        if (string.IsNullOrEmpty(ma) || string.IsNullOrEmpty(so)) return null;

        var ten = (await _caiDatRepo.GetGiaTriAsync(VietQr.KeyTenNganHang))?.Trim();
        var chu = (await _caiDatRepo.GetGiaTriAsync(VietQr.KeyChuTaiKhoan))?.Trim();
        return new NganHangInfo(string.IsNullOrEmpty(ten) ? ma : ten, so, chu ?? string.Empty, ma);
    }

    /// <summary>Dạng DH + yyMMddHHmmssfff, ví dụ DH261101103015123 (khớp README).</summary>
    private static string TaoMaDon() => "DH" + DateTime.Now.ToString("yyMMddHHmmssfff");

    private static DatHangKetQua Loi(int status, string message) => new() { Status = status, Message = message };
}
