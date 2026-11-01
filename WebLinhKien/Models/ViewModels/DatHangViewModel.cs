namespace WebLinhKien.Models.ViewModels;

public class DatHangViewModel
{
    public List<GioHangItemViewModel> Items { get; set; } = new();
    public decimal PhiGiaoHang { get; set; }
    public decimal NguongMienPhi { get; set; }
}

public class GioHangItemViewModel
{
    public int Uid { get; set; }
    public string Ten { get; set; } = string.Empty;
    public decimal Gia { get; set; }
    public int SoLuong { get; set; }
    public int TonKho { get; set; }
    public string Icon { get; set; } = "fa-box";   // class Font Awesome theo danh mục
}
