using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.Requests;
using WebLinhKien.Models.ViewModels.Client;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;
using WebLinhKien.Services;

namespace WebLinhKien.Areas.Admin.Controllers;

public class SanPhamController : AdminController
{
    private const int PageSize = 15;
    private const string ThuMucAnhSanPham = "san-pham";
    private const string ThuMucAnhMoTa = "mo-ta";

    private readonly ISanPhamRepository _repo;
    private readonly IDanhMucRepository _danhMucRepo;
    private readonly IThuongHieuRepository _thuongHieuRepo;
    private readonly ICaiDatRepository _caiDatRepo;
    private readonly IAdminNotifier _notifier;
    private readonly IWebHostEnvironment _env;

    public SanPhamController(
        ISanPhamRepository repo,
        IDanhMucRepository danhMucRepo,
        IThuongHieuRepository thuongHieuRepo,
        ICaiDatRepository caiDatRepo,
        IAdminNotifier notifier,
        IWebHostEnvironment env)
    {
        _repo = repo;
        _danhMucRepo = danhMucRepo;
        _thuongHieuRepo = thuongHieuRepo;
        _caiDatRepo = caiDatRepo;
        _notifier = notifier;
        _env = env;
    }

    // GET /Admin/SanPham?q=...&danhMuc=CPU&thuongHieu=INTEL&kho=sap-het&page=1&ts=ram-ddr5
    // kho: "sap-het" (còn 1..ngưỡng) | "het-hang" (tồn 0) | bỏ trống (tất cả)
    public async Task<IActionResult> Index([FromQuery] ProductFilterRequest request)
    {
        request ??= new ProductFilterRequest();
        request.Normalize(); // Chẩn hóa: Trim khoảng trắng, loại bỏ query độc hại, gán giới hạn min/max

        var nguong = await LayNguongAsync();

        if (request.DanhMuc is null)
            request.ThuongHieu = null; // hãng chỉ có nghĩa khi đã chọn loại

        // Phân trang với PageSize cố định của Admin (15), không bị ép theo chuẩn 12/24/36 của Client
        var ketQua = await _repo.GetPagedAdminAsync(request, nguong, PageSize);

        if (ketQua.TotalPages > 0 && request.Page > ketQua.TotalPages)
        {
            request.Page = ketQua.TotalPages;
            ketQua = await _repo.GetPagedAdminAsync(request, nguong, PageSize);
        }

        var (soSapHet, soHetHang) = await _repo.DemKhoAsync(request, nguong);

        var facets = request.DanhMuc is null
            ? new Dictionary<string, int>()
            : await _repo.GetSpecFacetsAsync(request, admin: true, nguong: nguong);

        var vm = new SanPhamListViewModel
        {
            Items = ketQua.Items,
            Page = request.Page,
            TotalPages = ketQua.TotalPages,
            TotalItems = ketQua.TotalItems,
            Q = request.Q,
            MaDanhMuc = request.DanhMuc,
            MaThuongHieu = request.ThuongHieu,
            Kho = request.Kho,
            NguongSapHet = nguong,
            SoSapHet = soSapHet,
            SoHetHang = soHetHang,
            DanhMucs = await _danhMucRepo.GetAllAsync(chiHienThi: false),
            ThuongHieus = await _thuongHieuRepo.GetAllAsync(chiHienThi: false),
            HangTheoLoai = request.DanhMuc is null
                ? Array.Empty<ThuongHieuFilterItem>()
                : await _repo.GetBrandsByCategoryAsync(request.DanhMuc),
            Ts = request.Ts,
            SpecFacets = facets
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await NapLookupsAsync(null);
        return View(new SanPhamForm { ThongSos = new List<ThongSoForm> { new(), new(), new() } });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Create(SanPhamForm form)
    {
        ChuanHoa(form);
        await KiemTraNghiepVuAsync(form, null);

        if (!ModelState.IsValid)
        {
            await NapLookupsAsync(null);
            return View(form);
        }

        var daLuu = new List<string>();   // file đã ghi xuống đĩa, xóa lại nếu lưu database lỗi
        var (anhDaiDien, anhPhus, loiAnh) = await LuuAnhFormAsync(form, daLuu);
        if (loiAnh is not null)
        {
            XoaFiles(daLuu);
            ModelState.AddModelError(nameof(form.AnhFiles), loiAnh);
            await NapLookupsAsync(null);
            return View(form);
        }

        try
        {
            var sp = TaoEntity(form, anhDaiDien);
            var uid = await _repo.InsertAsync(sp, TaoThongSos(form), anhPhus);
            await BaoSapHetAsync(uid, sp.TenSanPham, sp.TonKho, tonKhoCu: null);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)   // hai người thêm cùng mã một lúc
        {
            XoaFiles(daLuu);
            ModelState.AddModelError(nameof(form.MaSanPham), "Mã sản phẩm này đã tồn tại.");
            await NapLookupsAsync(null);
            return View(form);
        }
        catch
        {
            XoaFiles(daLuu);
            throw;
        }

        TempData["ThanhCong"] = $"Đã thêm sản phẩm {form.MaSanPham}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var sp = await _repo.GetByUidAsync(id);
        if (sp is null) return NotFound();

        var thongSos = await _repo.GetThongSosAsync(id);
        var form = new SanPhamForm
        {
            Uid = sp.Uid,
            MaSanPham = sp.MaSanPham,
            TenSanPham = sp.TenSanPham,
            MaDanhMuc = sp.MaDanhMuc,
            MaThuongHieu = sp.MaThuongHieu,
            GiaGoc = sp.GiaGoc,
            GiaBan = sp.GiaBan,
            TonKho = sp.TonKho,
            MoTaNgan = sp.MoTaNgan,
            MoTa = sp.MoTa,
            BaoHanhThang = sp.BaoHanhThang,
            NoiBat = sp.NoiBat,
            HienThi = sp.HienThi,
            ThongSos = thongSos.Select(t => new ThongSoForm { TenThongSo = t.TenThongSo, GiaTri = t.GiaTri }).ToList()
        };
        if (form.ThongSos.Count == 0) form.ThongSos.Add(new ThongSoForm());

        await NapLookupsAsync(sp);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Edit(int id, SanPhamForm form)
    {
        var cu = await _repo.GetByUidAsync(id);
        if (cu is null) return NotFound();

        form.Uid = id;
        ChuanHoa(form);
        await KiemTraNghiepVuAsync(form, id);

        if (!ModelState.IsValid)
        {
            await NapLookupsAsync(cu);
            return View(form);
        }

        var daLuu = new List<string>();
        var (anhMoi, anhPhus, loiAnh) = await LuuAnhFormAsync(form, daLuu);
        if (loiAnh is not null)
        {
            XoaFiles(daLuu);
            ModelState.AddModelError(nameof(form.AnhFiles), loiAnh);
            await NapLookupsAsync(cu);
            return View(form);
        }

        // Ảnh đại diện: có file mới thì thay, không thì giữ ảnh cũ (luôn lấy từ database)
        var sp = TaoEntity(form, anhMoi ?? cu.AnhDaiDien);
        sp.Uid = id;

        try
        {
            if (!await _repo.UpdateAsync(sp, TaoThongSos(form)))
            {
                XoaFiles(daLuu);
                return NotFound();
            }

            foreach (var duongDan in anhPhus) await _repo.ThemHinhAnhAsync(id, duongDan);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            XoaFiles(daLuu);
            ModelState.AddModelError(nameof(form.MaSanPham), "Mã sản phẩm này đã tồn tại.");
            await NapLookupsAsync(cu);
            return View(form);
        }
        catch
        {
            XoaFiles(daLuu);
            throw;
        }

        if (anhMoi is not null && cu.AnhDaiDien != anhMoi)
        {
            // Ảnh đại diện cũ chỉ xóa khỏi đĩa khi không còn nằm trong bộ ảnh phụ
            var conTrongBo = (await _repo.GetHinhAnhsAsync(id)).Any(h => h.DuongDan == cu.AnhDaiDien);
            if (!conTrongBo) AnhUpload.Xoa(cu.AnhDaiDien, _env.WebRootPath);
        }

        await BaoSapHetAsync(id, sp.TenSanPham, sp.TonKho, cu.TonKho);

        TempData["ThanhCong"] = "Đã lưu sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    // Xóa sản phẩm là thao tác không hoàn tác: chỉ Admin, nhân viên chỉ được ẩn sản phẩm (sửa HienThi).
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AuthServiceExtensions.AdminRole)]
    public async Task<IActionResult> Xoa(int id)
    {
        var sp = await _repo.GetByUidAsync(id);
        if (sp is null)
        {
            TempData["Loi"] = "Không tìm thấy sản phẩm cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        // Không có khóa ngoại: sản phẩm đã có trong đơn thì chỉ nên ẩn, không xóa
        var soDon = await _repo.DemTrongDonHangAsync(id);
        if (soDon > 0)
        {
            TempData["Loi"] = $"Không thể xóa: sản phẩm đã nằm trong {soDon} dòng đơn hàng. Hãy tắt hiển thị để ẩn khỏi website.";
            return RedirectToAction(nameof(Index));
        }

        var anhs = await _repo.GetHinhAnhsAsync(id);
        await _repo.DeleteAsync(id);

        AnhUpload.Xoa(sp.AnhDaiDien, _env.WebRootPath);
        foreach (var a in anhs) AnhUpload.Xoa(a.DuongDan, _env.WebRootPath);

        TempData["ThanhCong"] = $"Đã xóa sản phẩm {sp.MaSanPham}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/SanPham/BatTatHienThi/5 - nút bật/tắt nhanh trên danh sách
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatTatHienThi(int id, string? quayLai)
    {
        var sp = await _repo.GetByUidAsync(id);
        if (sp is null) return NotFound();

        sp.HienThi = !sp.HienThi;
        await _repo.UpdateAsync(sp, await _repo.GetThongSosAsync(id));

        TempData["ThanhCong"] = sp.HienThi ? $"Đã hiển thị {sp.MaSanPham}." : $"Đã ẩn {sp.MaSanPham}.";
        return Url.IsLocalUrl(quayLai) ? LocalRedirect(quayLai) : RedirectToAction(nameof(Index));
    }

    // ---- Ảnh của sản phẩm (trang Sửa) ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaAnh(int id, int anhId)
    {
        var anh = await _repo.GetHinhAnhAsync(anhId);
        var sp = await _repo.GetByUidAsync(id);
        if (anh is null || sp is null || anh.UidSanPham != id) return NotFound();

        await _repo.XoaHinhAnhAsync(anhId);
        if (sp.AnhDaiDien == anh.DuongDan) await _repo.DatAnhDaiDienAsync(id, null);
        AnhUpload.Xoa(anh.DuongDan, _env.WebRootPath);

        TempData["ThanhCong"] = "Đã xóa ảnh.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatAnhDaiDien(int id, int anhId)
    {
        var anh = await _repo.GetHinhAnhAsync(anhId);
        if (anh is null || anh.UidSanPham != id) return NotFound();

        await _repo.DatAnhDaiDienAsync(id, anh.DuongDan);
        TempData["ThanhCong"] = "Đã đổi ảnh đại diện.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /Admin/SanPham/TaiAnhMoTa - CKEditor 5 (SimpleUploadAdapter) gửi file trong field "upload"
    // và mong nhận { "url": "..." }, lỗi thì { "error": { "message": "..." } }.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(AnhUpload.DungLuongToiDa + 1_000_000)]
    public async Task<IActionResult> TaiAnhMoTa(IFormFile? upload)
    {
        if (upload is null)
            return BadRequest(new { error = new { message = "Không nhận được file." } });

        var (duongDan, loi) = await AnhUpload.LuuAsync(upload, _env.WebRootPath, ThuMucAnhMoTa);
        if (loi is not null)
            return BadRequest(new { error = new { message = loi } });

        return Json(new { url = duongDan });
    }

    // ---- Hàm phụ ----

    private async Task<int> LayNguongAsync() =>
        await _caiDatRepo.GetIntAsync(CaiDatKeys.NguongSapHetHang, 5);

    private async Task NapLookupsAsync(SanPham? sp)
    {
        ViewData["Lookups"] = new SanPhamFormLookups
        {
            DanhMucs = await _danhMucRepo.GetAllAsync(chiHienThi: false),
            ThuongHieus = await _thuongHieuRepo.GetAllAsync(chiHienThi: false),
            HinhAnhs = sp is null ? Array.Empty<HinhAnhSanPham>() : await _repo.GetHinhAnhsAsync(sp.Uid),
            AnhDaiDien = sp?.AnhDaiDien,
            NguongSapHet = await LayNguongAsync()
        };
    }

    private static void ChuanHoa(SanPhamForm f)
    {
        f.MaSanPham = f.MaSanPham?.Trim().ToUpperInvariant() ?? string.Empty;
        f.TenSanPham = f.TenSanPham?.Trim() ?? string.Empty;
        f.MaThuongHieu = string.IsNullOrWhiteSpace(f.MaThuongHieu) ? null : f.MaThuongHieu.Trim();
        f.MoTaNgan = string.IsNullOrWhiteSpace(f.MoTaNgan) ? null : f.MoTaNgan.Trim();
        f.MoTa = string.IsNullOrWhiteSpace(f.MoTa) ? null : f.MoTa;
        if (f.GiaGoc == 0) f.GiaGoc = f.GiaBan;   // không nhập giá gốc = không giảm giá
        f.ThongSos ??= new List<ThongSoForm>();
        f.AnhFiles = (f.AnhFiles ?? new List<IFormFile>()).Where(x => x.Length > 0).ToList();
    }

    private async Task KiemTraNghiepVuAsync(SanPhamForm form, int? uid)
    {
        if (!string.IsNullOrEmpty(form.MaDanhMuc) && await _danhMucRepo.GetByMaAsync(form.MaDanhMuc) is null)
            ModelState.AddModelError(nameof(form.MaDanhMuc), "Danh mục không tồn tại.");

        if (form.MaThuongHieu is not null && await _thuongHieuRepo.GetByMaAsync(form.MaThuongHieu) is null)
            ModelState.AddModelError(nameof(form.MaThuongHieu), "Thương hiệu không tồn tại.");

        if (!string.IsNullOrEmpty(form.MaSanPham) && await _repo.MaSanPhamTonTaiAsync(form.MaSanPham, uid))
            ModelState.AddModelError(nameof(form.MaSanPham), "Mã sản phẩm này đã tồn tại.");

        if (form.AnhFiles.Count > 10)
            ModelState.AddModelError(nameof(form.AnhFiles), "Mỗi lần chỉ tải tối đa 10 ảnh.");
    }

    // Lưu file ảnh đại diện và ảnh phụ; mọi file đã ghi được cộng vào daLuu để dọn khi có lỗi.
    private async Task<(string? AnhDaiDien, List<string> AnhPhus, string? Loi)> LuuAnhFormAsync(SanPhamForm form, List<string> daLuu)
    {
        var anhPhus = new List<string>();
        string? anhDaiDien = null;

        if (form.AnhDaiDienFile is { Length: > 0 })
        {
            var (duongDan, loi) = await AnhUpload.LuuAsync(form.AnhDaiDienFile, _env.WebRootPath, ThuMucAnhSanPham);
            if (loi is not null) return (null, anhPhus, loi);
            anhDaiDien = duongDan;
            daLuu.Add(duongDan!);
        }

        foreach (var file in form.AnhFiles)
        {
            var (duongDan, loi) = await AnhUpload.LuuAsync(file, _env.WebRootPath, ThuMucAnhSanPham);
            if (loi is not null) return (null, anhPhus, loi);
            anhPhus.Add(duongDan!);
            daLuu.Add(duongDan!);
        }

        // Thêm mới mà chưa chọn ảnh đại diện thì lấy ảnh phụ đầu tiên
        if (anhDaiDien is null && form.Uid == 0 && anhPhus.Count > 0) anhDaiDien = anhPhus[0];

        return (anhDaiDien, anhPhus, null);
    }

    private void XoaFiles(IEnumerable<string> duongDans)
    {
        foreach (var d in duongDans) AnhUpload.Xoa(d, _env.WebRootPath);
    }

    private static SanPham TaoEntity(SanPhamForm f, string? anhDaiDien) => new()
    {
        MaSanPham = f.MaSanPham,
        TenSanPham = f.TenSanPham,
        MaDanhMuc = f.MaDanhMuc,
        MaThuongHieu = f.MaThuongHieu,
        GiaGoc = f.GiaGoc,
        GiaBan = f.GiaBan,
        TonKho = f.TonKho,
        AnhDaiDien = anhDaiDien,
        MoTaNgan = f.MoTaNgan,
        MoTa = f.MoTa,
        BaoHanhThang = f.BaoHanhThang,
        NoiBat = f.NoiBat,
        HienThi = f.HienThi
    };

    private static List<ThongSoKyThuat> TaoThongSos(SanPhamForm f)
    {
        var ds = new List<ThongSoKyThuat>();
        foreach (var t in f.ThongSos)
        {
            if (string.IsNullOrWhiteSpace(t.TenThongSo) || string.IsNullOrWhiteSpace(t.GiaTri)) continue;
            ds.Add(new ThongSoKyThuat
            {
                TenThongSo = t.TenThongSo.Trim(),
                GiaTri = t.GiaTri.Trim(),
                Ordering = ds.Count
            });
        }
        return ds;
    }

    // Đẩy cảnh báo WebSocket khi tồn kho vừa chạm hoặc xuống dưới ngưỡng
    private async Task BaoSapHetAsync(int uid, string ten, int tonKho, int? tonKhoCu)
    {
        var nguong = await LayNguongAsync();
        if (tonKho <= nguong && tonKho != tonKhoCu)
            await _notifier.NotifyLowStockAsync(uid, ten, tonKho);
    }
}