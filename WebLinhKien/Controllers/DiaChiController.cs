using Microsoft.AspNetCore.Mvc;
using VietnamAddress.Core;
using VietnamAddress.Core.Abstractions;

namespace WebLinhKien.Controllers;

[ApiController]
[Route("api/dia-chi")]
public class DiaChiController : ControllerBase
{
    private readonly IVietnamAddressStore _store;
    public DiaChiController(IVietnamAddressStore store) => _store = store;

    // GET /api/dia-chi/tinh -> 34 tỉnh/thành
    [HttpGet("tinh")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Tinh() =>
    Ok(_store.GetProvinces().Select(p => new { code = p.Code, name = p.FullName, ten = p.Name }));

    [HttpGet("tinh/{ma}/xa")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Xa(string ma) =>
        Ok(_store.GetWardsByProvince(ma).Select(w => new { code = w.Code, name = w.FullName, ten = w.Name }));
}