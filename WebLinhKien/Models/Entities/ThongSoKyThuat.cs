namespace WebLinhKien.Models.Entities;

public class ThongSoKyThuat
{
    public int Uid { get; set; }
    public int UidSanPham { get; set; }
    public string TenThongSo { get; set; } = string.Empty;
    public string GiaTri { get; set; } = string.Empty;
    public int Ordering { get; set; }
}
