namespace WebLinhKien.Models.Requests;

/// <summary>Body JSON của /GioHang/Them, /GioHang/CapNhat, /GioHang/Xoa.</summary>
public class GioHangItemRequest
{
    public int UidSanPham { get; set; }
    public int SoLuong { get; set; } = 1;
}
