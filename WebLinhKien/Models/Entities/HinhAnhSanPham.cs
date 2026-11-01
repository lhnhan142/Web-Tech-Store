namespace WebLinhKien.Models.Entities;

public class HinhAnhSanPham
{
    public int Uid { get; set; }
    public int UidSanPham { get; set; }
    public string DuongDan { get; set; } = string.Empty;
    public int Ordering { get; set; }
}
