using System.ComponentModel.DataAnnotations;

namespace WebLinhKien.Models.Requests;

public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;

    public bool GhiNho { get; set; }

    public string? ReturnUrl { get; set; }
}
