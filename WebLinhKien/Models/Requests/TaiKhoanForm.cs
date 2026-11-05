using System.ComponentModel.DataAnnotations;

namespace WebLinhKien.Models.Requests;

/// <summary>Thêm hoặc sửa tài khoản. Khi sửa thì TenDangNhap và MatKhau không dùng tới.</summary>
public class TaiKhoanForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9_.-]+$", ErrorMessage = "Chỉ gồm chữ không dấu, số, dấu chấm (.), gạch dưới (_) và gạch nối (-).")]
    public string TenDangNhap { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string? HoTen { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string VaiTro { get; set; } = "NhanVien";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu tạm.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 100 ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;
}

public class DatLaiMatKhauForm
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu tạm.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 100 ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class DoiMatKhauForm
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string MatKhauHienTai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 100 ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    public string XacNhanMatKhau { get; set; } = string.Empty;
}
