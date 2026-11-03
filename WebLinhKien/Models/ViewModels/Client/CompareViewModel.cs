using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels.Client;

public class CompareSpecRow
{
    public string TenThongSo { get; set; } = string.Empty;
    /// <summary>Giá trị thông số theo từng UidSanPham (nếu sản phẩm không có thông số này, giá trị là "-")</summary>
    public Dictionary<int, string> GiaTriTheoSanPham { get; set; } = new();
}

public class CompareViewModel
{
    public List<SanPham> SanPhams { get; set; } = new();
    public List<CompareSpecRow> HangThongSo { get; set; } = new();
}

