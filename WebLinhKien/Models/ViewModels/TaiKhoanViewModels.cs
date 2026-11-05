using WebLinhKien.Models.Entities;

namespace WebLinhKien.Models.ViewModels;

public class TaiKhoanListViewModel
{
    public IReadOnlyList<TaiKhoan> Items { get; set; } = Array.Empty<TaiKhoan>();

    /// <summary>Uid của người đang đăng nhập, để không hiện nút khóa / đặt lại cho chính mình.</summary>
    public int UidHienTai { get; set; }
}
