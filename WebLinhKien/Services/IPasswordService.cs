using WebLinhKien.Models.Entities;

namespace WebLinhKien.Services;

public interface IPasswordService
{
    /// <summary>Băm mật khẩu để lưu vào TaiKhoans.MatKhauHash.</summary>
    string Hash(string matKhau);

    /// <summary>
    /// Kiểm tra mật khẩu. Nếu taiKhoan = null (sai tên đăng nhập) vẫn thực hiện một lần
    /// băm giả để thời gian phản hồi gần bằng nhau, tránh lộ tên đăng nhập nào tồn tại.
    /// </summary>
    bool Verify(TaiKhoan? taiKhoan, string matKhau);
}
