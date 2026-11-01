namespace WebLinhKien.Models.Entities;

public class DanhMuc
{
    public string MaDanhMuc { get; set; } = string.Empty;
    public string TenDanhMuc { get; set; } = string.Empty;
    public int Ordering { get; set; }
    public bool HienThi { get; set; } = true;
}
