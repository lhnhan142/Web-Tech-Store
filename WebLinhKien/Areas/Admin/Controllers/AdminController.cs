using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;

namespace WebLinhKien.Areas.Admin.Controllers;

/// <summary>
/// Lớp cha cho MỌI controller trong khu quản trị (sản phẩm, đơn hàng...).
/// Kế thừa lớp này là tự động bắt buộc đăng nhập (Admin hoặc NhanVien đều vào được).
/// Controller chỉ dành cho Admin thì thêm [Authorize(Roles = AuthServiceExtensions.AdminHoacNhanVien)] lên lớp đó:
/// hai điều kiện cộng lại nên kết quả là chỉ Admin vào được.
/// </summary>
[Area("Admin")]
[Authorize(Roles = AuthServiceExtensions.AdminHoacNhanVien)]
public abstract class AdminController : Controller
{
}
