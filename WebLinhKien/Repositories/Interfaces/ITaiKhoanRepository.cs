using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

public partial interface ITaiKhoanRepository
{
    Task<TaiKhoan?> GetByTenDangNhapAsync(string tenDangNhap);
    Task<bool> AnyAsync();
    /// <summary>Thêm tài khoản, trả về Uid mới. MatKhauHash phải được hash trước khi gọi.</summary>
    Task<int> InsertAsync(TaiKhoan taiKhoan);
}
