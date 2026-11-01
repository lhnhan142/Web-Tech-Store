using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;

namespace WebLinhKien.Areas.Admin.Controllers;

/// <summary>
/// Lớp cha cho MỌI controller trong khu quản trị (sản phẩm, đơn hàng...).
/// Kế thừa lớp này là tự động bắt buộc đăng nhập admin.
/// </summary>
[Area("Admin")]
[Authorize(Roles = AuthServiceExtensions.AdminRole)]
public abstract class AdminController : Controller
{
}
