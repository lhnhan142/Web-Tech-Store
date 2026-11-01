namespace WebLinhKien.Models.Entities;

public class ThuongHieu
{
    public string MaThuongHieu { get; set; } = string.Empty;
    public string TenThuongHieu { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public bool HienThi { get; set; } = true;
}
