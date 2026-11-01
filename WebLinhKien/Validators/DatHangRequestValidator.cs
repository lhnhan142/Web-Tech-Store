using FluentValidation;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Requests;

namespace WebLinhKien.Validators;

/// <summary>Quy tắc kiểm tra ở server, GIỐNG với dat-hang.js ở trình duyệt.</summary>
public class DatHangRequestValidator : AbstractValidator<DatHangRequest>
{
    private static readonly string[] HinhThucHopLe = { WebConstants.HinhThucCod, WebConstants.HinhThucChuyenKhoan };

    public DatHangRequestValidator()
    {
        RuleFor(x => x.TenKhachHang)
            .NotEmpty().WithMessage("Vui lòng nhập họ tên.")
            .Length(2, 100).WithMessage("Họ tên phải từ 2 đến 100 ký tự.");

        RuleFor(x => x.Sdt)
            .NotEmpty().WithMessage("Vui lòng nhập số điện thoại.")
            .Matches(@"^(0|84)[35789][0-9]{8}$").WithMessage("Số điện thoại di động không hợp lệ (ví dụ 0912345678).");

        RuleFor(x => x.DiaChi)
            .NotEmpty().WithMessage("Vui lòng nhập địa chỉ.")
            .Length(5, 200).WithMessage("Địa chỉ phải từ 5 đến 200 ký tự.");

        RuleFor(x => x.GhiChu)
            .MaximumLength(300).WithMessage("Ghi chú tối đa 300 ký tự.");

        RuleFor(x => x.MaHinhThucThanhToan)
            .Must(v => HinhThucHopLe.Contains(v)).WithMessage("Hình thức thanh toán không hợp lệ.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn hàng chưa có sản phẩm nào.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.UidSanPham).GreaterThan(0).WithMessage("Sản phẩm không hợp lệ.");
            item.RuleFor(i => i.SoLuong)
                .InclusiveBetween(1, WebConstants.SoLuongToiDa)
                .WithMessage($"Số lượng mỗi sản phẩm phải từ 1 đến {WebConstants.SoLuongToiDa}.");
        });

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.UidSanPham).Distinct().Count() == items.Count)
            .WithMessage("Một sản phẩm không được xuất hiện hai lần trong đơn.")
            .When(x => x.Items.Count > 0);
    }
}
