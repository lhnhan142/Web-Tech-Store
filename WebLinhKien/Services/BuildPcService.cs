using System.Globalization;
using System.Text.RegularExpressions;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels.BuildPc;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Services;

/// <summary>
/// Nghiệp vụ Build PC: đọc thông số kỹ thuật dạng văn bản, kiểm tra tương thích
/// và gợi ý cấu hình theo ngân sách.
/// </summary>
public sealed class BuildPcService : IBuildPcService
{
    private readonly IBuildPcRepository _repo;
    private readonly ICaiDatRepository _caiDat;

    private static readonly string[] LoaiLinhKien =
        { "CPU", "MAINBOARD", "RAM", "VGA", "SSD", "PSU", "CASE", "TANNHIET" };

    private static readonly string[] LoaiBatBuoc =
        { "CPU", "MAINBOARD", "RAM", "VGA", "SSD", "PSU", "CASE" };

    private static readonly string[] ThuTuLap =
        { "CPU", "MAINBOARD", "RAM", "VGA", "SSD", "PSU", "CASE", "TANNHIET" };

    private static readonly (string Ma, string Ten, string Icon, bool BatBuoc, string GoiY)[] DinhNghiaBuoc =
    {
        ("CPU",       "Bộ xử lý (CPU)",              "fa-microchip", true,  "Chọn CPU trước để lọc mainboard lắp vừa cùng chân cắm."),
        ("MAINBOARD", "Bo mạch chủ (Mainboard)",      "fa-server",    true,  "Phải cùng chân cắm với CPU và đúng chuẩn RAM."),
        ("RAM",       "Bộ nhớ trong (RAM)",           "fa-memory",    true,  "Chọn đúng DDR4/DDR5 theo mainboard."),
        ("VGA",       "Card đồ họa (VGA)",            "fa-display",   true,  "Kiểm tra chiều dài VGA và công suất nguồn."),
        ("SSD",       "Ổ cứng SSD",                   "fa-hard-drive",true,  "Ưu tiên NVMe M.2 nếu mainboard hỗ trợ."),
        ("PSU",       "Nguồn máy tính (PSU)",         "fa-bolt",      true,  "Công suất phải đủ cho CPU và VGA."),
        ("CASE",      "Vỏ case",                      "fa-desktop",   true,  "Cần hỗ trợ kích thước mainboard và chiều dài VGA."),
        ("TANNHIET",  "Tản nhiệt (tùy chọn)",         "fa-fan",       false, "Tản nhiệt phải lắp vừa chân cắm của CPU.")
    };

    private static readonly string[] ThuTuPhuKien =
        { "Màn hình", "Bàn phím", "Chuột", "Tai nghe", "Loa", "Webcam", "Microphone", "Bàn di chuột" };

    private static readonly Dictionary<string, string> TenTheoMa =
        DinhNghiaBuoc.ToDictionary(b => b.Ma, b => b.Ten, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> IconTheoMa =
        DinhNghiaBuoc.ToDictionary(b => b.Ma, b => b.Icon, StringComparer.OrdinalIgnoreCase);

    public BuildPcService(IBuildPcRepository repo, ICaiDatRepository caiDat)
    {
        _repo = repo;
        _caiDat = caiDat;
    }

    // =====================================================================
    // Trang dữ liệu
    // =====================================================================

    public async Task<BuildPcIndexViewModel> LayTrangAsync()
    {
        var danhMuc = LoaiLinhKien.Append("PHUKIEN").ToArray();
        var sanPhamsTask = _repo.GetByCategoriesAsync(danhMuc);
        var thongSoTask = _repo.GetThongSoTheoDanhMucAsync(danhMuc);
        var macDinhTask = _caiDat.GetDecimalAsync(CaiDatKeys.BuildPcNganSachMacDinh, 20_000_000m);
        var goiYTask = _caiDat.GetGiaTriAsync(CaiDatKeys.BuildPcNganSachGoiY);
        await Task.WhenAll(sanPhamsTask, thongSoTask, macDinhTask, goiYTask);

        var nganSachMacDinh = await macDinhTask;
        if (nganSachMacDinh <= 0) nganSachMacDinh = 20_000_000m;
        var nganSachGoiY = DocDanhSachNganSach(await goiYTask, nganSachMacDinh);

        var catalog = TaoCatalog(await sanPhamsTask, await thongSoTask);
        var theoLoai = catalog.Values
            .GroupBy(x => x.MaDanhMuc.ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.GiaBan).ThenBy(x => x.TenSanPham).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var buocs = DinhNghiaBuoc.Select((b, i) => new BuildPcBuoc
        {
            MaDanhMuc = b.Ma,
            Ten = b.Ten,
            Icon = b.Icon,
            BatBuoc = b.BatBuoc,
            GoiY = b.GoiY,
            ThuTu = i + 1,
            LinhKiens = theoLoai.TryGetValue(b.Ma, out var list)
                ? list
                : Array.Empty<BuildPcLinhKien>()
        }).ToList();

        var phuKienNhoms = theoLoai.TryGetValue("PHUKIEN", out var phuKien)
            ? phuKien
                .GroupBy(x => string.IsNullOrWhiteSpace(x.LoaiPhuKien) ? "Khác" : x.LoaiPhuKien!)
                .OrderBy(g => ThuTuNhomPhuKien(g.Key))
                .ThenBy(g => g.Key)
                .Select(g => new BuildPcPhuKienNhom
                {
                    Loai = g.Key,
                    Items = g.OrderBy(x => x.GiaBan).ThenBy(x => x.TenSanPham).ToList()
                })
                .ToList()
            : new List<BuildPcPhuKienNhom>();

        return new BuildPcIndexViewModel
        {
            Buocs = buocs,
            PhuKienNhoms = phuKienNhoms,
            Breadcrumbs = new List<WebLinhKien.Models.ViewModels.Client.BreadcrumbItem>
            {
                new("Build PC", null, true)
            },
            NganSachMacDinh = nganSachMacDinh,
            NganSachGoiY = nganSachGoiY
        };
    }

    private static IReadOnlyList<decimal> DocDanhSachNganSach(string? raw, decimal macDinh)
    {
        var text = string.IsNullOrWhiteSpace(raw)
            ? CaiDatRules.Get(CaiDatKeys.BuildPcNganSachGoiY).MacDinh
            : raw;

        var list = (text ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => decimal.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m)
            .Where(v => v > 0)
            .Distinct()
            .OrderBy(v => v)
            .Take(5)
            .ToList();

        if (list.Count == 0) list.Add(macDinh);
        return list;
    }

    // =====================================================================
    // Kiểm tra cấu hình đang chọn
    // =====================================================================

    public async Task<BuildPcChonKetQua> KiemTraAsync(BuildPcChonRequest? chon)
    {
        chon ??= new BuildPcChonRequest();
        var ketQua = new BuildPcChonKetQua();
        var daChon = chon.LinhKienDaChon();

        var uids = chon.TatCaUid().Distinct().ToList();
        if (uids.Count == 0)
        {
            ketQua.Thieu.AddRange(LoaiBatBuoc.Select(ma => TenTheoMa[ma]));
            return ketQua;
        }

        var sanPhamsTask = _repo.GetByUidsAsync(uids);
        var thongSoTask = _repo.GetThongSoTheoUidsAsync(uids);
        await Task.WhenAll(sanPhamsTask, thongSoTask);
        var catalog = TaoCatalog(await sanPhamsTask, await thongSoTask);

        var daDung = new HashSet<int>();
        foreach (var (ma, uid) in daChon)
        {
            if (!catalog.TryGetValue(uid, out var lk))
            {
                ketQua.Loi.Add($"{TenTheoMa[ma]}: sản phẩm không tồn tại hoặc đã ngừng bán.");
                continue;
            }

            if (!string.Equals(lk.MaDanhMuc, ma, StringComparison.OrdinalIgnoreCase))
            {
                ketQua.Loi.Add($"\"{lk.TenSanPham}\" không thuộc nhóm {TenTheoMa[ma]}.");
                continue;
            }

            if (!daDung.Add(uid))
            {
                ketQua.Loi.Add($"\"{lk.TenSanPham}\" bị chọn trùng ở nhiều nhóm linh kiện.");
                continue;
            }

            if (lk.TonKho <= 0)
            {
                ketQua.Loi.Add($"\"{lk.TenSanPham}\" đã hết hàng.");
            }

            ketQua.LinhKiens.Add(lk);
        }

        foreach (var ma in LoaiBatBuoc)
        {
            if (!daChon.ContainsKey(ma))
            {
                ketQua.Thieu.Add(TenTheoMa[ma]);
            }
        }

        var cpu = TimTheoLoai(ketQua.LinhKiens, "CPU");
        var mainboard = TimTheoLoai(ketQua.LinhKiens, "MAINBOARD");
        var ram = TimTheoLoai(ketQua.LinhKiens, "RAM");
        var vga = TimTheoLoai(ketQua.LinhKiens, "VGA");
        var psu = TimTheoLoai(ketQua.LinhKiens, "PSU");
        var voCase = TimTheoLoai(ketQua.LinhKiens, "CASE");
        var tanNhiet = TimTheoLoai(ketQua.LinhKiens, "TANNHIET");

        if (cpu?.Socket != null && mainboard?.Socket != null && !SocketBang(cpu.Socket, mainboard.Socket))
        {
            ketQua.Loi.Add($"CPU \"{TenGoi(cpu, "CPU")}\" dùng chân cắm {cpu.Socket}, còn mainboard \"{TenGoi(mainboard, "Mainboard", "Bo mạch chủ")}\" dùng chân cắm {mainboard.Socket}. Hai loại chân cắm khác nhau nên không lắp vào nhau được — hãy chọn CPU và mainboard cùng chân cắm.");
        }

        if (ram?.LoaiRam != null && mainboard?.LoaiRam != null && !RamTuongThich(ram.LoaiRam, mainboard.LoaiRam))
        {
            ketQua.Loi.Add($"Thanh RAM \"{TenGoi(ram, "RAM")}\" là loại {ram.LoaiRam}, nhưng mainboard \"{TenGoi(mainboard, "Mainboard", "Bo mạch chủ")}\" chỉ dùng được RAM {mainboard.LoaiRam}. Hãy chọn thanh RAM loại {mainboard.LoaiRam}.");
        }

        if (mainboard?.FormFactor != null && voCase != null && !CaseHoTro(mainboard.FormFactor, voCase.FormHoTro))
        {
            ketQua.Loi.Add($"Mainboard \"{TenGoi(mainboard, "Mainboard", "Bo mạch chủ")}\" có kích thước {mainboard.FormFactor}, không vừa vỏ case \"{TenGoi(voCase, "Vỏ case", "Case")}\" (vỏ này chỉ nhận mainboard {string.Join(", ", voCase.FormHoTro)}). Hãy chọn vỏ case phù hợp hơn.");
        }

        if (vga?.ChieuDaiMm is int chieuDaiVga && voCase?.ChieuDaiVgaToiDaMm is int chieuDaiToiDa && chieuDaiVga > chieuDaiToiDa)
        {
            ketQua.Loi.Add($"Card màn hình \"{TenGoi(vga, "Card màn hình", "VGA")}\" dài {chieuDaiVga} mm, nhưng vỏ case \"{TenGoi(voCase, "Vỏ case", "Case")}\" chỉ chứa được card dài tối đa {chieuDaiToiDa} mm. Hãy chọn card ngắn hơn hoặc vỏ case lớn hơn.");
        }

        if (tanNhiet != null && cpu?.Socket != null && tanNhiet.SocketHoTro.Count > 0 &&
            !tanNhiet.SocketHoTro.Any(s => SocketBang(s, cpu.Socket)))
        {
            ketQua.Loi.Add($"Tản nhiệt \"{TenGoi(tanNhiet, "Tản nhiệt")}\" không lắp được cho CPU \"{TenGoi(cpu, "CPU")}\" vì không hỗ trợ chân cắm {cpu.Socket}. Hãy chọn tản nhiệt có hỗ trợ {cpu.Socket}.");
        }

        ketQua.CongSuatUocTinhW = TinhTieuThuHeThongW(cpu, vga);
        var congSuatToiThieu = TinhCongSuatNguonToiThieuW(cpu, vga);
        if (psu != null)
        {
            ketQua.CongSuatPsuW = psu.CongSuatW;
            if (congSuatToiThieu is int canThiet)
            {
                if (psu.CongSuatW is int congSuat)
                {
                    if (congSuat < canThiet)
                    {
                        ketQua.Loi.Add($"Nguồn \"{TenGoi(psu, "Nguồn máy tính", "Nguồn", "PSU")}\" chỉ có {congSuat}W, nhưng cả bộ máy cần tối thiểu {canThiet}W. Hãy chọn nguồn từ {canThiet}W trở lên.");
                    }
                    else if (congSuat - canThiet < 50)
                    {
                        ketQua.CanhBao.Add($"Nguồn \"{TenGoi(psu, "Nguồn máy tính", "Nguồn", "PSU")}\" {congSuat}W chỉ dư {congSuat - canThiet}W so với mức cần {canThiet}W. Nên chọn nguồn công suất cao hơn để máy chạy ổn định.");
                    }
                }
                else
                {
                    ketQua.CanhBao.Add("Nguồn chưa có thông tin công suất, không thể xác thực tải.");
                }
            }
        }

        KiemTraPhuKien(chon, catalog, cpu, vga, ketQua);

        ketQua.HopLe = ketQua.Loi.Count == 0 && ketQua.Thieu.Count == 0;
        return ketQua;
    }

    private static void KiemTraPhuKien(
        BuildPcChonRequest chon,
        IReadOnlyDictionary<int, BuildPcLinhKien> catalog,
        BuildPcLinhKien? cpu,
        BuildPcLinhKien? vga,
        BuildPcChonKetQua ketQua)
    {
        if (chon.PhuKien is not { Count: > 0 }) return;

        var daGap = new HashSet<int>();
        foreach (var uid in chon.PhuKien.Where(u => u > 0))
        {
            if (!daGap.Add(uid))
            {
                ketQua.CanhBao.Add("Một phụ kiện bị chọn trùng, chỉ thêm một lần.");
                continue;
            }

            if (!catalog.TryGetValue(uid, out var pk))
            {
                ketQua.KiemTraPhuKien.Add(new BuildPcPhuKienKiemTra
                {
                    Uid = uid,
                    Ten = "Phụ kiện đã chọn",
                    TrangThai = "error",
                    ThongDiep = "Sản phẩm không còn tồn tại hoặc đã ngừng bán."
                });
                ketQua.CanhBao.Add("Một phụ kiện đã chọn không còn tồn tại hoặc đã ngừng bán.");
                continue;
            }

            if (!string.Equals(pk.MaDanhMuc, "PHUKIEN", StringComparison.OrdinalIgnoreCase))
            {
                ketQua.KiemTraPhuKien.Add(new BuildPcPhuKienKiemTra
                {
                    Uid = uid,
                    Ten = pk.TenSanPham,
                    Loai = pk.LoaiPhuKien ?? "Khác",
                    TrangThai = "error",
                    ThongDiep = "Sản phẩm này không thuộc danh mục phụ kiện."
                });
                ketQua.CanhBao.Add($"\"{pk.TenSanPham}\" không phải phụ kiện, đã bỏ qua.");
                continue;
            }

            if (pk.TonKho <= 0)
            {
                ketQua.KiemTraPhuKien.Add(new BuildPcPhuKienKiemTra
                {
                    Uid = uid,
                    Ten = pk.TenSanPham,
                    Loai = pk.LoaiPhuKien ?? "Khác",
                    TrangThai = "error",
                    ThongDiep = "Phụ kiện đã hết hàng."
                });
                ketQua.CanhBao.Add($"\"{pk.TenSanPham}\" đã hết hàng, sẽ không được thêm vào giỏ.");
                continue;
            }

            var kiemTra = KiemTraMotPhuKien(pk, cpu, vga);
            ketQua.KiemTraPhuKien.Add(kiemTra);
            ketQua.PhuKiens.Add(pk);
            if (!string.Equals(kiemTra.TrangThai, "ok", StringComparison.OrdinalIgnoreCase))
            {
                ketQua.CanhBao.Add($"{pk.TenSanPham}: {kiemTra.ThongDiep}");
            }
        }
    }

    private static BuildPcPhuKienKiemTra KiemTraMotPhuKien(
        BuildPcLinhKien pk,
        BuildPcLinhKien? cpu,
        BuildPcLinhKien? vga)
    {
        var loai = pk.LoaiPhuKien ?? "Khác";

        if (string.Equals(loai, "Màn hình", StringComparison.OrdinalIgnoreCase))
        {
            if (vga == null)
            {
                return TaoKiemTra(pk, "warn", "Chưa chọn VGA để kiểm tra cổng xuất hình.");
            }

            if (pk.DoPhanGiaiNgang is int rong && rong >= 3840 && vga.VramGb is < 8)
            {
                return TaoKiemTra(pk, "warn",
                    $"Màn hình 4K nên đi kèm VGA 8GB VRAM trở lên (VGA đang chọn {vga.VramGb?.ToString() ?? "chưa rõ"}GB).");
            }

            if (pk.DoPhanGiaiNgang is int rong2 && rong2 >= 2560 && vga.VramGb is < 6)
            {
                return TaoKiemTra(pk, "warn",
                    $"Màn hình 2K nên đi kèm VGA 6GB VRAM trở lên (VGA đang chọn {vga.VramGb?.ToString() ?? "chưa rõ"}GB).");
            }

            if (pk.TanSoQuetHz is int hz && hz >= 144)
            {
                return TaoKiemTra(pk, "ok",
                    $"Màn hình {hz}Hz: dùng cổng DisplayPort/HDMI của VGA để đạt tần số quét tối đa.");
            }

            return TaoKiemTra(pk, "ok", "Độ phân giải và tần số quét phù hợp với VGA đã chọn.");
        }

        if (string.Equals(loai, "Bàn di chuột", StringComparison.OrdinalIgnoreCase))
        {
            return TaoKiemTra(pk, "ok", "Không yêu cầu cổng kết nối.");
        }

        var ketNoi = pk.KetNoi ?? string.Empty;
        if (ketNoi.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) &&
            !ketNoi.Contains("USB", StringComparison.OrdinalIgnoreCase))
        {
            return TaoKiemTra(pk, "warn",
                "Thiết bị dùng Bluetooth; cần mainboard có Bluetooth hoặc bộ thu USB đi kèm.");
        }

        if (ketNoi.Contains("USB-C", StringComparison.OrdinalIgnoreCase))
        {
            return TaoKiemTra(pk, "ok",
                "Dùng cổng USB-C/USB-A trên mainboard hoặc case (kiểm tra cổng USB-C nếu cần đúng chuẩn).");
        }

        if (ketNoi.Contains("3.5", StringComparison.OrdinalIgnoreCase))
        {
            return TaoKiemTra(pk, "ok", "Dùng jack 3.5mm trên mainboard hoặc case.");
        }

        if (ketNoi.Contains("USB", StringComparison.OrdinalIgnoreCase))
        {
            return TaoKiemTra(pk, "ok", "Dùng cổng USB trên mainboard hoặc case.");
        }

        return TaoKiemTra(pk, "warn",
            "Chưa có dữ liệu kết nối chi tiết; kiểm tra cổng trên mainboard trước khi mua.");
    }

    private static BuildPcPhuKienKiemTra TaoKiemTra(BuildPcLinhKien pk, string trangThai, string thongDiep) => new()
    {
        Uid = pk.Uid,
        Ten = pk.TenSanPham,
        Loai = pk.LoaiPhuKien ?? "Khác",
        TrangThai = trangThai,
        ThongDiep = thongDiep
    };

    // =====================================================================
    // Gợi ý cấu hình theo ngân sách
    // =====================================================================

    public async Task<BuildPcGoiYKetQua> GoiYTheoNganSachAsync(BuildPcGoiYRequest? request)
    {
        request ??= new BuildPcGoiYRequest();

        if (request.NganSach <= 0)
        {
            return new BuildPcGoiYKetQua
            {
                ThanhCong = false,
                NganSach = 0,
                ThongBao = "Vui lòng nhập ngân sách hợp lệ (lớn hơn 0)."
            };
        }

        var nganSach = request.NganSach;

        var sanPhamsTask = _repo.GetByCategoriesAsync(LoaiLinhKien);
        var thongSoTask = _repo.GetThongSoTheoDanhMucAsync(LoaiLinhKien);
        await Task.WhenAll(sanPhamsTask, thongSoTask);

        var catalog = TaoCatalog(await sanPhamsTask, await thongSoTask);
        var theoLoai = catalog.Values
            .Where(x => x.ConHang)
            .GroupBy(x => x.MaDanhMuc.ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.GiaBan).ThenBy(x => x.TenSanPham).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var ma in LoaiBatBuoc)
        {
            if (!theoLoai.TryGetValue(ma, out var ds) || ds.Count == 0)
            {
                return new BuildPcGoiYKetQua
                {
                    ThanhCong = false,
                    NganSach = nganSach,
                    ThongBao = $"Kho hiện chưa có {TenTheoMa[ma]} còn hàng để lắp cấu hình."
                };
            }
        }

        // Cấu hình rẻ nhất còn hàng (dùng để xác định ngân sách tối thiểu thực tế).
        var hoSoToiThieu = new NganSachHoSo(
            "Tối thiểu",
            "Cấu hình rẻ nhất còn hàng.",
            TyLeMacDinh(),
            "re-nhat");
        var phuongAnToiThieu = TaoPhuongAn(hoSoToiThieu, 1_000_000_000m, theoLoai, gomTanNhiet: false);

        if (phuongAnToiThieu != null && nganSach < phuongAnToiThieu.TongTien)
        {
            return new BuildPcGoiYKetQua
            {
                ThanhCong = false,
                NganSach = nganSach,
                ThongBao = $"Ngân sách tối thiểu để lắp một bộ PC tương thích hiện tại là {phuongAnToiThieu.TongTien.ToVnd()}. Vui lòng tăng ngân sách."
            };
        }

        var laGaming = !LaVanPhong(request.MucDich);
        var hoSos = laGaming ? HoSoGaming() : HoSoVanPhong();

        var phuongAns = new List<BuildPcGoiYPhuongAn>();
        foreach (var hoSo in hoSos)
        {
            var pa = TaoPhuongAn(hoSo, nganSach, theoLoai);
            if (pa == null) continue;
            if (phuongAns.Any(p => CungCauHinh(p, pa))) continue;
            phuongAns.Add(pa);
        }

        if (phuongAns.Count < 2)
        {
            foreach (var hoSo in hoSos)
            {
                var pa = TaoPhuongAn(hoSo with { ChienLuoc = "re-nhat", Ten = "Tiết kiệm", MoTa = "Chọn linh kiện rẻ nhất còn hàng trong ngân sách." }, nganSach, theoLoai);
                if (pa == null || phuongAns.Any(p => CungCauHinh(p, pa))) continue;
                phuongAns.Add(pa);
                if (phuongAns.Count >= 2) break;
            }
        }

        if (phuongAns.Count == 0)
        {
            return new BuildPcGoiYKetQua
            {
                ThanhCong = false,
                NganSach = nganSach,
                ThongBao = $"Không tìm thấy bộ PC tương thích trong ngân sách {nganSach.ToVnd()}. Vui lòng thử tăng ngân sách hoặc chọn linh kiện thủ công."
            };
        }

        return new BuildPcGoiYKetQua
        {
            ThanhCong = true,
            NganSach = nganSach,
            ThongBao = laGaming
                ? $"Gợi ý {phuongAns.Count} cấu hình gaming trong ngân sách {nganSach.ToVnd()}."
                : $"Gợi ý {phuongAns.Count} cấu hình văn phòng trong ngân sách {nganSach.ToVnd()}.",
            PhuongAns = phuongAns
        };
    }

    private static BuildPcGoiYPhuongAn? TaoPhuongAn(
        NganSachHoSo hoSo,
        decimal nganSach,
        IReadOnlyDictionary<string, List<BuildPcLinhKien>> theoLoai,
        bool gomTanNhiet = true)
    {
        var chon = new Dictionary<string, BuildPcLinhKien>(StringComparer.OrdinalIgnoreCase);
        decimal daChi = 0;
        double tyLeDaQua = 0;

        for (var i = 0; i < ThuTuLap.Length; i++)
        {
            var ma = ThuTuLap[i];
            if (!LoaiBatBuoc.Contains(ma)) continue;

            if (!theoLoai.TryGetValue(ma, out var ds) || ds.Count == 0) return null;

            var duPhong = TinhDuPhong(i + 1, chon, theoLoai);
            if (duPhong >= decimal.MaxValue / 8) return null;

            var conLai = nganSach - daChi;
            var tyLe = hoSo.TyLe.TryGetValue(ma, out var tl) ? tl : 0.10m;
            var tietKiem = Math.Max(0m, nganSach * (decimal)tyLeDaQua - daChi);
            var cap = Math.Min(nganSach * tyLe + tietKiem, conLai - duPhong);
            if (cap < 0) return null;

            var ungVien = ds.Where(x => TuongThichVoiHeThong(x, ma, chon, theoLoai)).ToList();
            if (ungVien.Count == 0) return null;

            var chonDuoc = ChonLinhKien(ungVien, cap, hoSo.ChienLuoc, ma);
            if (chonDuoc == null) return null;
            if (chonDuoc.GiaBan > conLai - duPhong) return null;

            chon[ma] = chonDuoc;
            daChi += chonDuoc.GiaBan;
            tyLeDaQua += (double)tyLe;
        }

        // Tản nhiệt tùy chọn: chỉ thêm nếu còn ngân sách.
        if (gomTanNhiet && theoLoai.TryGetValue("TANNHIET", out var tanNhiets))
        {
            var conLai = nganSach - daChi;
            var ungVien = tanNhiets
                .Where(x => x.GiaBan <= conLai && TuongThichVoiHeThong(x, "TANNHIET", chon, theoLoai))
                .ToList();
            if (ungVien.Count > 0)
            {
                var tanNhiet = hoSo.ChienLuoc == "re-nhat"
                    ? ungVien.OrderBy(x => x.GiaBan).First()
                    : ungVien.OrderByDescending(x => x.GiaBan).First();
                chon["TANNHIET"] = tanNhiet;
                daChi += tanNhiet.GiaBan;
            }
        }

        if (daChi > nganSach) return null;

        var items = ThuTuLap.Where(chon.ContainsKey).Select(ma => chon[ma]).ToList();
        var canhBao = new List<string>();
        foreach (var item in items.Where(x => x.TonKho <= 3))
        {
            canhBao.Add($"\"{item.TenSanPham}\" chỉ còn {item.TonKho} sản phẩm.");
        }

        if (!chon.ContainsKey("TANNHIET"))
        {
            canhBao.Add("Chưa gồm tản nhiệt rời; dùng tản nhiệt đi kèm CPU nếu có.");
        }

        var psu = chon.GetValueOrDefault("PSU");
        var congSuatToiThieu = TinhCongSuatNguonToiThieuW(chon.GetValueOrDefault("CPU"), chon.GetValueOrDefault("VGA"));
        if (psu?.CongSuatW is int congSuat && congSuatToiThieu is int canThiet && congSuat - canThiet < 50)
        {
            canhBao.Add($"Nguồn {congSuat}W chỉ dư {congSuat - canThiet}W so với nhu cầu tối thiểu {canThiet}W.");
        }

        return new BuildPcGoiYPhuongAn
        {
            Ten = hoSo.Ten,
            MoTa = hoSo.MoTa,
            LinhKiens = items,
            CanhBao = canhBao,
            TongTien = items.Sum(x => x.GiaBan),
            ConLai = nganSach - items.Sum(x => x.GiaBan)
        };
    }

    private static BuildPcLinhKien? ChonLinhKien(
        List<BuildPcLinhKien> ds,
        decimal cap,
        string chienLuoc,
        string ma)
    {
        var trongNganSach = ds.Where(x => x.GiaBan <= cap).ToList();
        if (trongNganSach.Count == 0)
        {
            return ds.OrderBy(x => x.GiaBan).ThenByDescending(x => x.TonKho).FirstOrDefault();
        }

        return chienLuoc switch
        {
            "re-nhat" => trongNganSach.OrderBy(x => x.GiaBan).ThenByDescending(x => x.TonKho).First(),
            "vga-manh" when string.Equals(ma, "VGA", StringComparison.OrdinalIgnoreCase) =>
                trongNganSach.OrderByDescending(x => x.GiaBan).ThenByDescending(x => x.TonKho).First(),
            "vga-manh" => trongNganSach.OrderBy(x => x.GiaBan).ThenByDescending(x => x.TonKho).First(),
            _ => trongNganSach.OrderByDescending(x => x.GiaBan).ThenByDescending(x => x.TonKho).First()
        };
    }

    private static decimal TinhDuPhong(
        int chiSoBatDau,
        Dictionary<string, BuildPcLinhKien> chon,
        IReadOnlyDictionary<string, List<BuildPcLinhKien>> theoLoai)
    {
        var tam = new Dictionary<string, BuildPcLinhKien>(chon, StringComparer.OrdinalIgnoreCase);
        decimal tong = 0;

        for (var i = chiSoBatDau; i < ThuTuLap.Length; i++)
        {
            var ma = ThuTuLap[i];
            if (!LoaiBatBuoc.Contains(ma)) continue;
            if (!theoLoai.TryGetValue(ma, out var ds)) return decimal.MaxValue / 4;

            var re = ds.Where(x => TuongThichVoiHeThong(x, ma, tam, theoLoai))
                       .OrderBy(x => x.GiaBan)
                       .FirstOrDefault();
            if (re == null) return decimal.MaxValue / 4;

            tong += re.GiaBan;
            tam[ma] = re;
        }

        return tong;
    }

    private static bool TuongThichVoiHeThong(
        BuildPcLinhKien sp,
        string ma,
        IReadOnlyDictionary<string, BuildPcLinhKien> chon,
        IReadOnlyDictionary<string, List<BuildPcLinhKien>> theoLoai)
    {
        chon.TryGetValue("CPU", out var cpu);
        chon.TryGetValue("MAINBOARD", out var mainboard);
        chon.TryGetValue("VGA", out var vga);

        List<BuildPcLinhKien> Lay(string key) =>
            theoLoai.TryGetValue(key, out var list) ? list : new List<BuildPcLinhKien>();

        switch (ma.ToUpperInvariant())
        {
            case "CPU":
                if (sp.Socket == null) return true;
                var mainboards = Lay("MAINBOARD").Where(x => SocketBang(x.Socket, sp.Socket)).ToList();
                if (mainboards.Count == 0) return false;
                var rams = Lay("RAM");
                return mainboards.Any(mb => rams.Any(r => RamTuongThich(r.LoaiRam, mb.LoaiRam)));

            case "MAINBOARD":
                if (cpu != null && !SocketBang(sp.Socket, cpu.Socket)) return false;
                if (!Lay("RAM").Any(r => RamTuongThich(r.LoaiRam, sp.LoaiRam))) return false;
                return Lay("CASE").Any(c => CaseHoTro(sp.FormFactor, c.FormHoTro));

            case "RAM":
                if (mainboard != null && !RamTuongThich(sp.LoaiRam, mainboard.LoaiRam)) return false;
                return true;

            case "VGA":
                if (sp.ChieuDaiMm is int chieuDai)
                {
                    return Lay("CASE").Any(c => c.ChieuDaiVgaToiDaMm is int toiDa && toiDa >= chieuDai);
                }
                return true;

            case "PSU":
                var canThiet = TinhCongSuatNguonToiThieuW(cpu, vga);
                if (canThiet is int yeuCau)
                {
                    if (sp.CongSuatW is not int congSuat) return false;
                    return congSuat >= yeuCau;
                }
                return true;

            case "CASE":
                if (mainboard != null && !CaseHoTro(mainboard.FormFactor, sp.FormHoTro)) return false;
                if (vga?.ChieuDaiMm is int daiVga && sp.ChieuDaiVgaToiDaMm is int maxVga && maxVga < daiVga)
                {
                    return false;
                }
                return true;

            case "TANNHIET":
                if (cpu?.Socket != null && sp.SocketHoTro.Count > 0 &&
                    !sp.SocketHoTro.Any(s => SocketBang(s, cpu.Socket)))
                {
                    return false;
                }
                return true;

            default:
                return true;
        }
    }

    // =====================================================================
    // Đọc/chuẩn hóa thông số kỹ thuật
    // =====================================================================

    private static Dictionary<int, BuildPcLinhKien> TaoCatalog(
        IEnumerable<SanPham> sanPhams,
        IEnumerable<ThongSoKyThuat> thongSos)
    {
        var thongSoTheoSanPham = thongSos
            .GroupBy(t => t.UidSanPham)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ThongSoKyThuat>)g.OrderBy(t => t.Ordering).ThenBy(t => t.Uid).ToList());

        var catalog = new Dictionary<int, BuildPcLinhKien>();
        foreach (var sp in sanPhams)
        {
            thongSoTheoSanPham.TryGetValue(sp.Uid, out var specs);
            catalog[sp.Uid] = TaoLinhKien(sp, specs ?? Array.Empty<ThongSoKyThuat>());
        }
        return catalog;
    }

    private static BuildPcLinhKien TaoLinhKien(SanPham sp, IReadOnlyList<ThongSoKyThuat> specs)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in specs)
        {
            if (!string.IsNullOrWhiteSpace(t.TenThongSo))
            {
                map[t.TenThongSo.Trim()] = t.GiaTri?.Trim() ?? string.Empty;
            }
        }

        string? Lay(params string[] ten)
        {
            foreach (var key in ten)
            {
                if (map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
            }
            return null;
        }

        var ma = sp.MaDanhMuc.ToUpperInvariant();
        var socketHoTro = Array.Empty<string>();
        if (ma == "TANNHIET")
        {
            socketHoTro = TachDanhSach(Lay("Socket hỗ trợ", "Hỗ trợ socket", "Socket"))
                .Select(ChuanHoaSocket)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var formHoTro = ma == "CASE"
            ? TachDanhSach(Lay("Hỗ trợ mainboard", "Hỗ trợ form factor", "Form factor hỗ trợ"))
            : Array.Empty<string>();

        return new BuildPcLinhKien
        {
            Uid = sp.Uid,
            MaSanPham = sp.MaSanPham,
            TenSanPham = sp.TenSanPham,
            MaDanhMuc = ma,
            MaThuongHieu = sp.MaThuongHieu,
            GiaGoc = sp.GiaGoc,
            GiaBan = sp.GiaBan,
            TonKho = sp.TonKho,
            AnhDaiDien = sp.AnhDaiDien,
            BaoHanhThang = sp.BaoHanhThang,
            NoiBat = sp.NoiBat,
            Socket = ma is "CPU" or "MAINBOARD"
                ? ChuanHoaSocket(Lay("Socket", "Socket CPU", "Chân CPU"))
                : null,
            LoaiRam = ma is "MAINBOARD" or "RAM"
                ? ChuanHoaRam(Lay("Loại RAM hỗ trợ", "Loại RAM", "Chuẩn RAM"))
                : null,
            FormFactor = ma == "MAINBOARD"
                ? Lay("Kích thước", "Form factor", "Chuẩn mainboard")
                : null,
            TdpW = ma == "CPU"
                ? SoDauTien(Lay("TDP", "Công suất tiêu thụ", "Điện năng tiêu thụ"))
                : null,
            CongSuatW = ma switch
            {
                "PSU" => SoDauTien(Lay("Công suất", "Công suất tối đa")),
                "VGA" => SoDauTien(Lay("Nguồn đề nghị", "Nguồn khuyến nghị", "PSU đề nghị")),
                _ => null
            },
            TieuThuW = ma == "VGA"
                ? SoDauTien(Lay("Công suất tiêu thụ", "Tiêu thụ điện", "Điện năng tiêu thụ"))
                : null,
            ChieuDaiMm = ma == "VGA" ? SoDauTien(Lay("Chiều dài", "Chiều dài card")) : null,
            ChieuDaiVgaToiDaMm = ma == "CASE" ? SoDauTien(Lay("Chiều dài VGA tối đa", "Chiều dài card tối đa")) : null,
            SocketHoTro = socketHoTro,
            FormHoTro = formHoTro,
            LoaiPhuKien = ma == "PHUKIEN" ? Lay("Loại phụ kiện", "Loại") : null,
            DungLuong = ma is "RAM" or "SSD" ? Lay("Dung lượng") : null,
            ChuanKetNoi = ma == "SSD" ? Lay("Chuẩn kết nối", "Chuẩn giao tiếp") : null,
            KetNoi = ma == "PHUKIEN" ? Lay("Kết nối", "Cổng kết nối") : null,
            DoPhanGiaiNgang = ma == "PHUKIEN" ? SoDauTien(Lay("Độ phân giải")) : null,
            TanSoQuetHz = ma == "PHUKIEN" ? SoDauTien(Lay("Tần số quét")) : null,
            VramGb = ma == "VGA" ? SoDauTien(Lay("Bộ nhớ", "VRAM", "Dung lượng bộ nhớ")) : null,
            ThongSoChinh = specs.Take(4)
                .Select(t => new BuildPcThongSo { Ten = t.TenThongSo, GiaTri = t.GiaTri })
                .ToList()
        };
    }

    private static IReadOnlyList<string> TachDanhSach(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split(new[] { ',', ';', '|', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? ChuanHoaSocket(string? socket)
    {
        if (string.IsNullOrWhiteSpace(socket)) return null;
        return Regex.Replace(socket.ToUpperInvariant(), @"\s+", string.Empty);
    }

    private static string? ChuanHoaRam(string? ram)
    {
        if (string.IsNullOrWhiteSpace(ram)) return null;
        var tokens = Regex.Matches(ram.ToUpperInvariant(), @"DDR\s*\d")
            .Select(m => m.Value.Replace(" ", string.Empty))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return tokens.Count > 0 ? string.Join("/", tokens) : ram.Trim().ToUpperInvariant();
    }

    private static int? SoDauTien(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = Regex.Match(text, @"\d+");
        return match.Success && int.TryParse(match.Value, out var value) ? value : null;
    }

    // =====================================================================
    // Quy tắc tương thích dùng chung
    // =====================================================================

    private static bool SocketBang(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return true;
        return string.Equals(ChuanHoaSocket(a), ChuanHoaSocket(b), StringComparison.OrdinalIgnoreCase);
    }

    private static bool RamTuongThich(string? ram, string? mainboardRam)
    {
        if (string.IsNullOrWhiteSpace(ram) || string.IsNullOrWhiteSpace(mainboardRam)) return true;

        var ramTokens = TachDdr(ram);
        var mainboardTokens = TachDdr(mainboardRam);
        if (ramTokens.Count == 0 || mainboardTokens.Count == 0)
        {
            return string.Equals(ram.Trim(), mainboardRam.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        return ramTokens.Overlaps(mainboardTokens);
    }

    private static HashSet<string> TachDdr(string text) =>
        Regex.Matches(text.ToUpperInvariant(), @"DDR\s*\d")
            .Select(m => m.Value.Replace(" ", string.Empty))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool CaseHoTro(string? formMainboard, IReadOnlyList<string> formCase)
    {
        if (string.IsNullOrWhiteSpace(formMainboard) || formCase.Count == 0) return true;

        var rankMainboard = FormRank(formMainboard);
        if (rankMainboard == 0) return true;

        return formCase.Select(FormRank).DefaultIfEmpty(0).Max() >= rankMainboard;
    }

    private static int FormRank(string? form)
    {
        var text = form?.ToLowerInvariant() ?? string.Empty;
        if (text.Contains("mini-itx") || text.Contains("mini itx") || Regex.IsMatch(text, @"\bitx\b")) return 1;
        if (text.Contains("micro-atx") || text.Contains("micro atx") || text.Contains("matx") || text.Contains("m-atx")) return 2;
        if (text.Contains("e-atx") || text.Contains("eatx")) return 4;
        if (text.Contains("atx")) return 3;
        return 0;
    }

    private static int TinhTieuThuVgaW(BuildPcLinhKien? vga)
    {
        if (vga == null) return 0;
        if (vga.TieuThuW is int tieuThu && tieuThu > 0) return tieuThu;
        if (vga.CongSuatW is int deNghi && deNghi > 0) return Math.Max(75, (int)Math.Round(deNghi * 0.4));
        return 150;
    }

    private static int TinhTieuThuHeThongW(BuildPcLinhKien? cpu, BuildPcLinhKien? vga) =>
        (cpu?.TdpW ?? 95) + TinhTieuThuVgaW(vga) + 100;

    private static int? TinhCongSuatNguonToiThieuW(BuildPcLinhKien? cpu, BuildPcLinhKien? vga)
    {
        if (cpu == null && vga == null) return null;
        var tieuThu = TinhTieuThuHeThongW(cpu, vga);
        var deNghi = vga?.CongSuatW ?? 0;
        return Math.Max(tieuThu, deNghi);
    }

    private static BuildPcLinhKien? TimTheoLoai(IEnumerable<BuildPcLinhKien> ds, string ma) =>
        ds.FirstOrDefault(x => string.Equals(x.MaDanhMuc, ma, StringComparison.OrdinalIgnoreCase));

    /// <summary>Tên sản phẩm rút gọn, bỏ tiền tố loại hàng trùng với từ mô tả trong câu thông báo.</summary>
    private static string TenGoi(BuildPcLinhKien lk, params string[] tienTo)
    {
        var ten = lk.TenSanPham.Trim();
        foreach (var tu in tienTo)
        {
            if (ten.Length > tu.Length + 1 && ten.StartsWith(tu + " ", StringComparison.OrdinalIgnoreCase))
            {
                return ten[(tu.Length + 1)..].Trim();
            }
        }
        return ten;
    }

    private static int ThuTuNhomPhuKien(string loai)
    {
        var index = Array.FindIndex(ThuTuPhuKien, x => string.Equals(x, loai, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? int.MaxValue : index;
    }

    private static bool LaVanPhong(string? mucDich)
    {
        var text = mucDich?.Trim().ToLowerInvariant() ?? string.Empty;
        return text.Contains("van-phong") || text.Contains("văn phòng") || text.Contains("office") ||
               text.Contains("hoc") || text.Contains("học") || text.Contains("work");
    }

    private static bool CungCauHinh(BuildPcGoiYPhuongAn a, BuildPcGoiYPhuongAn b) =>
        a.LinhKiens.Select(x => x.Uid).OrderBy(x => x).SequenceEqual(b.LinhKiens.Select(x => x.Uid).OrderBy(x => x));

    private static Dictionary<string, decimal> TyLeMacDinh() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["CPU"] = 0.25m,
        ["MAINBOARD"] = 0.14m,
        ["RAM"] = 0.10m,
        ["VGA"] = 0.10m,
        ["SSD"] = 0.15m,
        ["PSU"] = 0.10m,
        ["CASE"] = 0.10m,
        ["TANNHIET"] = 0.06m
    };

    private static List<NganSachHoSo> HoSoGaming() => new()
    {
        new NganSachHoSo(
            "Cân bằng",
            "Cân bằng CPU và VGA, phù hợp chơi game Full HD đến 2K.",
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["CPU"] = 0.20m,
                ["MAINBOARD"] = 0.11m,
                ["RAM"] = 0.08m,
                ["VGA"] = 0.37m,
                ["SSD"] = 0.08m,
                ["PSU"] = 0.08m,
                ["CASE"] = 0.06m,
                ["TANNHIET"] = 0.02m
            },
            "tot-nhat"),
        new NganSachHoSo(
            "Ưu tiên VGA",
            "Dồn ngân sách cho card đồ họa để chơi game nặng và AAA.",
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["CPU"] = 0.17m,
                ["MAINBOARD"] = 0.08m,
                ["RAM"] = 0.07m,
                ["VGA"] = 0.46m,
                ["SSD"] = 0.07m,
                ["PSU"] = 0.07m,
                ["CASE"] = 0.06m,
                ["TANNHIET"] = 0.02m
            },
            "vga-manh"),
        new NganSachHoSo(
            "Tiết kiệm",
            "Tối ưu chi phí, vẫn đáp ứng nhu cầu gaming phổ thông.",
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["CPU"] = 0.22m,
                ["MAINBOARD"] = 0.12m,
                ["RAM"] = 0.08m,
                ["VGA"] = 0.34m,
                ["SSD"] = 0.08m,
                ["PSU"] = 0.08m,
                ["CASE"] = 0.06m,
                ["TANNHIET"] = 0.02m
            },
            "re-nhat")
    };

    private static List<NganSachHoSo> HoSoVanPhong() => new()
    {
        new NganSachHoSo(
            "Cân bằng văn phòng",
            "Ổn định cho văn phòng, học tập và đa nhiệm.",
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["CPU"] = 0.25m,
                ["MAINBOARD"] = 0.14m,
                ["RAM"] = 0.10m,
                ["VGA"] = 0.10m,
                ["SSD"] = 0.15m,
                ["PSU"] = 0.10m,
                ["CASE"] = 0.10m,
                ["TANNHIET"] = 0.06m
            },
            "tot-nhat"),
        new NganSachHoSo(
            "Tiết kiệm",
            "Chi phí thấp nhất, phù hợp nhu cầu cơ bản.",
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["CPU"] = 0.24m,
                ["MAINBOARD"] = 0.13m,
                ["RAM"] = 0.11m,
                ["VGA"] = 0.08m,
                ["SSD"] = 0.15m,
                ["PSU"] = 0.11m,
                ["CASE"] = 0.11m,
                ["TANNHIET"] = 0.07m
            },
            "re-nhat"),
        new NganSachHoSo(
            "Ưu tiên đồ họa nhẹ",
            "Tăng ngân sách cho VGA và SSD, phù hợp thiết kế cơ bản.",
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["CPU"] = 0.20m,
                ["MAINBOARD"] = 0.12m,
                ["RAM"] = 0.10m,
                ["VGA"] = 0.20m,
                ["SSD"] = 0.16m,
                ["PSU"] = 0.09m,
                ["CASE"] = 0.07m,
                ["TANNHIET"] = 0.06m
            },
            "vga-manh")
    };

    private sealed record NganSachHoSo(
        string Ten,
        string MoTa,
        Dictionary<string, decimal> TyLe,
        string ChienLuoc);
}
