using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

public partial interface ITaiKhoanRepository
{
    Task<TaiKhoan?> GetByTenDangNhapAsync(string tenDangNhap);
    Task<TaiKhoan?> GetByUidAsync(int uid);
    Task<IReadOnlyList<TaiKhoan>> GetAllAsync();
    Task<bool> AnyAsync();
    /// <summary>Thêm tài khoản, trả về Uid mới. MatKhauHash phải được hash trước khi gọi.</summary>
    Task<int> InsertAsync(TaiKhoan taiKhoan);

    /// <summary>Sửa họ tên và vai trò. Trả về false nếu không có tài khoản đó.</summary>
    Task<bool> UpdateThongTinAsync(int uid, string? hoTen, string vaiTro);

    Task<bool> SetHoatDongAsync(int uid, bool hoatDong);

    /// <summary>Lưu mật khẩu mới (đã hash). canDoiMatKhau = true thì lần đăng nhập tới bị bắt đổi mật khẩu.</summary>
    Task<bool> DoiMatKhauAsync(int uid, string matKhauHash, bool canDoiMatKhau);

    /// <summary>Số admin đang hoạt động, dùng để không cho khóa hoặc hạ quyền admin cuối cùng.</summary>
    Task<int> DemAdminHoatDongAsync();
}
