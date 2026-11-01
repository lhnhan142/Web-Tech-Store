using Microsoft.AspNetCore.Mvc;

namespace WebLinhKien.Areas.Admin.Controllers;

public class HomeController : AdminController
{
    public IActionResult Index() => View();
}
