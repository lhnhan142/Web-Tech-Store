namespace WebLinhKien.Models.Entities;

public class ChiTietDonHang
{
    public int Uid { get; set; }
    public int UidDonHang { get; set; }
    public int UidSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;   // lưu tại thời điểm mua
    public decimal DonGia { get; set; }                       // lưu tại thời điểm mua
    public int SoLuong { get; set; }
    public decimal ThanhTien { get; set; }                    // cột tính toán, chỉ đọc
}
