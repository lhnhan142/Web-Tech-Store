using WebLinhKien.Helpers;

namespace WebLinhKien.Models.ViewModels.BuildPc;

/// <summary>Cấu hình khách chọn ở trang Build PC (mỗi loại một sản phẩm, phụ kiện chọn nhiều).</summary>
public sealed class BuildPcChonRequest
{
    public int? Cpu { get; set; }
    public int? Mainboard { get; set; }
    public int? Ram { get; set; }
    public int? Vga { get; set; }
    public int? Ssd { get; set; }
    public int? Psu { get; set; }
    public int? Case { get; set; }
    public int? TanNhiet { get; set; }
    public List<int>? PhuKien { get; set; }

    /// <summary>Các uid linh kiện chính đã chọn, khóa là mã danh mục.</summary>
    public Dictionary<string, int> LinhKienDaChon()
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Them("CPU", Cpu);
        Them("MAINBOARD", Mainboard);
        Them("RAM", Ram);
        Them("VGA", Vga);
        Them("SSD", Ssd);
        Them("PSU", Psu);
        Them("CASE", Case);
        Them("TANNHIET", TanNhiet);
        return map;

        void Them(string ma, int? uid)
        {
            if (uid is > 0) map[ma] = uid.Value;
        }
    }

    public IEnumerable<int> TatCaUid()
    {
        foreach (var uid in LinhKienDaChon().Values) yield return uid;
        if (PhuKien is null) yield break;
        foreach (var uid in PhuKien.Where(u => u > 0).Distinct()) yield return uid;
    }
}

public sealed class BuildPcGoiYRequest
{
    public decimal NganSach { get; set; } = 20_000_000m;
    public string? MucDich { get; set; } = "gaming";
}

public sealed class BuildPcPhuKienKiemTra
{
    public int Uid { get; init; }
    public string Ten { get; init; } = string.Empty;
    public string Loai { get; init; } = string.Empty;
    public string TrangThai { get; init; } = "ok";
    public string ThongDiep { get; init; } = string.Empty;
}

/// <summary>Kết quả kiểm tra một cấu hình (dùng cho cả nút Kiểm tra và Thêm cả bộ vào giỏ).</summary>
public sealed class BuildPcChonKetQua
{
    public bool HopLe { get; set; }
    public List<BuildPcLinhKien> LinhKiens { get; set; } = new();
    public List<BuildPcLinhKien> PhuKiens { get; set; } = new();
    public List<string> Loi { get; set; } = new();
    public List<string> Thieu { get; set; } = new();
    public List<string> CanhBao { get; set; } = new();
    public List<BuildPcPhuKienKiemTra> KiemTraPhuKien { get; set; } = new();
    public int? CongSuatUocTinhW { get; set; }
    public int? CongSuatPsuW { get; set; }

    public decimal TongTien => LinhKiens.Sum(x => x.GiaBan) + PhuKiens.Sum(x => x.GiaBan);
    public string TongTienText => TongTien.ToVnd();
    public int SoMon => LinhKiens.Count + PhuKiens.Count;
}

public sealed class BuildPcGoiYPhuongAn
{
    public string Ten { get; init; } = string.Empty;
    public string MoTa { get; init; } = string.Empty;
    public decimal TongTien { get; init; }
    public decimal ConLai { get; init; }
    public IReadOnlyList<BuildPcLinhKien> LinhKiens { get; init; } = Array.Empty<BuildPcLinhKien>();
    public IReadOnlyList<string> CanhBao { get; init; } = Array.Empty<string>();
    public string TongTienText => TongTien.ToVnd();
    public string ConLaiText => ConLai.ToVnd();
}

public sealed class BuildPcGoiYKetQua
{
    public bool ThanhCong { get; init; }
    public string? ThongBao { get; init; }
    public decimal NganSach { get; init; }
    public IReadOnlyList<BuildPcGoiYPhuongAn> PhuongAns { get; init; } = Array.Empty<BuildPcGoiYPhuongAn>();
}
