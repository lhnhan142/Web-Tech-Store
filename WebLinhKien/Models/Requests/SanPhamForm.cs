using System.ComponentModel.DataAnnotations;

namespace WebLinhKien.Models.Requests;

public class ThongSoForm
{
    [StringLength(100, ErrorMessage = "Tên thông số tối đa 100 ký tự.")]
    public string? TenThongSo { get; set; }

    [StringLength(300, ErrorMessage = "Giá trị thông số tối đa 300 ký tự.")]
    public string? GiaTri { get; set; }
}

public class SanPhamForm : IValidatableObject
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm.")]
    [StringLength(50, ErrorMessage = "Mã tối đa 50 ký tự.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch dưới (_) và gạch nối (-).")]
    public string MaSanPham { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(250, ErrorMessage = "Tên tối đa 250 ký tự.")]
    public string TenSanPham { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    public string MaDanhMuc { get; set; } = string.Empty;

    public string? MaThuongHieu { get; set; }

    [Range(0, 9_999_999_999, ErrorMessage = "Giá gốc không hợp lệ.")]
    public decimal GiaGoc { get; set; }

    [Range(1, 9_999_999_999, ErrorMessage = "Giá bán phải lớn hơn 0.")]
    public decimal GiaBan { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Tồn kho từ 0 đến 1.000.000.")]
    public int TonKho { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả ngắn tối đa 500 ký tự.")]
    public string? MoTaNgan { get; set; }

    public string? MoTa { get; set; }   // HTML từ CKEditor

    [Range(0, 120, ErrorMessage = "Bảo hành từ 0 đến 120 tháng.")]
    public int BaoHanhThang { get; set; }

    public bool NoiBat { get; set; }
    public bool HienThi { get; set; } = true;

    public List<ThongSoForm> ThongSos { get; set; } = new();

    // Ảnh tải lên (đường dẫn ảnh đang có luôn lấy từ database, không nhận từ form)
    public IFormFile? AnhDaiDienFile { get; set; }
    public List<IFormFile> AnhFiles { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GiaGoc > 0 && GiaGoc < GiaBan)
            yield return new ValidationResult("Giá gốc không được thấp hơn giá bán (để 0 nếu không giảm giá).", new[] { nameof(GiaGoc) });

        for (var i = 0; i < ThongSos.Count; i++)
        {
            var coTen = !string.IsNullOrWhiteSpace(ThongSos[i].TenThongSo);
            var coGiaTri = !string.IsNullOrWhiteSpace(ThongSos[i].GiaTri);
            if (coTen != coGiaTri)
                yield return new ValidationResult("Mỗi thông số cần nhập đủ cả tên và giá trị (hoặc để trống cả hai dòng).", new[] { nameof(ThongSos) });
        }
    }
}
