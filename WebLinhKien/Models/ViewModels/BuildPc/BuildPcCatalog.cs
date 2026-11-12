using WebLinhKien.Helpers;
using WebLinhKien.Models.ViewModels.Client;

namespace WebLinhKien.Models.ViewModels.BuildPc;

/// <summary>Một linh kiện đã đọc sẵn thông số tương thích từ bảng ThongSoKyThuats.</summary>
public sealed class BuildPcLinhKien
{
    public int Uid { get; init; }
    public string MaSanPham { get; init; } = string.Empty;
    public string TenSanPham { get; init; } = string.Empty;
    public string MaDanhMuc { get; init; } = string.Empty;
    public string? MaThuongHieu { get; init; }
    public decimal GiaGoc { get; init; }
    public decimal GiaBan { get; init; }
    public int TonKho { get; init; }
    public string? AnhDaiDien { get; init; }
    public int BaoHanhThang { get; init; }
    public bool NoiBat { get; init; }

    public string? Socket { get; init; }
    public string? LoaiRam { get; init; }
    public string? FormFactor { get; init; }
    public int? TdpW { get; init; }
    public int? CongSuatW { get; init; }
    public int? TieuThuW { get; init; }
    public int? ChieuDaiMm { get; init; }
    public int? ChieuDaiVgaToiDaMm { get; init; }
    public IReadOnlyList<string> SocketHoTro { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FormHoTro { get; init; } = Array.Empty<string>();
    public string? LoaiPhuKien { get; init; }
    public string? DungLuong { get; init; }
    public string? ChuanKetNoi { get; init; }
    public string? KetNoi { get; init; }
    public int? DoPhanGiaiNgang { get; init; }
    public int? TanSoQuetHz { get; init; }
    public int? VramGb { get; init; }
    public IReadOnlyList<BuildPcThongSo> ThongSoChinh { get; init; } = Array.Empty<BuildPcThongSo>();

    public bool ConHang => TonKho > 0;
    public bool CoGiamGia => GiaGoc > GiaBan && GiaBan > 0;
    public string GiaBanText => GiaBan.ToVnd();
    public string GiaGocText => GiaGoc.ToVnd();
    public string TenThuongHieu => string.IsNullOrWhiteSpace(MaThuongHieu) ? "Chính hãng" : MaThuongHieu!;
}

public sealed class BuildPcThongSo
{
    public string Ten { get; init; } = string.Empty;
    public string GiaTri { get; init; } = string.Empty;
}

/// <summary>Một bước chọn linh kiện (CPU, MAINBOARD...).</summary>
public sealed class BuildPcBuoc
{
    public string MaDanhMuc { get; init; } = string.Empty;
    public string Ten { get; init; } = string.Empty;
    public string Icon { get; init; } = "fa-box";
    public bool BatBuoc { get; init; }
    public string? GoiY { get; init; }
    public int ThuTu { get; init; }
    public IReadOnlyList<BuildPcLinhKien> LinhKiens { get; init; } = Array.Empty<BuildPcLinhKien>();
}

public sealed class BuildPcPhuKienNhom
{
    public string Loai { get; init; } = string.Empty;
    public IReadOnlyList<BuildPcLinhKien> Items { get; init; } = Array.Empty<BuildPcLinhKien>();
}

/// <summary>ViewModel cho trang /BuildPc.</summary>
public sealed class BuildPcIndexViewModel
{
    public IReadOnlyList<BuildPcBuoc> Buocs { get; init; } = Array.Empty<BuildPcBuoc>();
    public IReadOnlyList<BuildPcPhuKienNhom> PhuKienNhoms { get; init; } = Array.Empty<BuildPcPhuKienNhom>();
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; init; } = Array.Empty<BreadcrumbItem>();
    public decimal NganSachMacDinh { get; init; } = 20_000_000m;
    public IReadOnlyList<decimal> NganSachGoiY { get; init; } = Array.Empty<decimal>();
    public bool CoDuLieu => Buocs.Any(b => b.LinhKiens.Count > 0);
}
