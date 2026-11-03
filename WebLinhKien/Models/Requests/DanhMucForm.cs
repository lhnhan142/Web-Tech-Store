using System.ComponentModel.DataAnnotations;

namespace WebLinhKien.Models.Requests;

public class DanhMucForm
{
    [Required(ErrorMessage = "Vui lòng nhập mã danh mục.")]
    [StringLength(50, ErrorMessage = "Mã tối đa 50 ký tự.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch dưới (_) và gạch nối (-).")]
    public string MaDanhMuc { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự.")]
    public string TenDanhMuc { get; set; } = string.Empty;

    [Range(0, 9999, ErrorMessage = "Thứ tự từ 0 đến 9999.")]
    public int Ordering { get; set; }

    public bool HienThi { get; set; } = true;
}
