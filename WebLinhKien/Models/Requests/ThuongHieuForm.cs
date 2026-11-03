using System.ComponentModel.DataAnnotations;

namespace WebLinhKien.Models.Requests;

public class ThuongHieuForm
{
    [Required(ErrorMessage = "Vui lòng nhập mã thương hiệu.")]
    [StringLength(50, ErrorMessage = "Mã tối đa 50 ký tự.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch dưới (_) và gạch nối (-).")]
    public string MaThuongHieu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu.")]
    [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự.")]
    public string TenThuongHieu { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Đường dẫn logo tối đa 300 ký tự.")]
    [RegularExpression(@"^(/|https?://).*$", ErrorMessage = "Đường dẫn logo phải bắt đầu bằng / hoặc http:// hoặc https://")]
    public string? Logo { get; set; }

    public bool HienThi { get; set; } = true;
}
