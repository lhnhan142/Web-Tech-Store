using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.Requests;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Areas.Admin.Controllers;

[Authorize(Roles = AuthServiceExtensions.AdminRole)]
public class CaiDatController : AdminController
{
    private readonly ICaiDatRepository _repo;

    // Khóa cài đặt của trang Build PC: hiện trên form kể cả khi database chưa có dòng nào,
    // lần lưu đầu tiên sẽ tự thêm vào bảng CaiDats.
    private static readonly string[] CaiDatBuildPc =
    {
        CaiDatKeys.BuildPcNganSachMacDinh,
        CaiDatKeys.BuildPcNganSachGoiY
    };

    public CaiDatController(ICaiDatRepository repo) => _repo = repo;

    public async Task<IActionResult> Index()
    {
        var caiDats = await _repo.GetAllAsync();
        var hienCo = caiDats.ToDictionary(c => c.Khoa);

        var items = caiDats
            .Where(c => c.Khoa != "TenCuaHang")
            .Select(c => ItemTu(c, c.GiaTri))
            .ToList();

        foreach (var khoa in CaiDatBuildPc)
        {
            if (hienCo.ContainsKey(khoa)) continue;
            items.Add(ItemTu(null, CaiDatRules.Get(khoa).MacDinh, khoa));
        }

        return View(new CaiDatForm { Items = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(CaiDatForm form)
    {
        var hienCo = (await _repo.GetAllAsync()).ToDictionary(c => c.Khoa);

        // Chỉ nhận khóa đã có trong database hoặc đã khai báo quy tắc:
        // người dùng không thể thêm khóa lạ bằng cách sửa form.
        form.Items = (form.Items ?? new())
            .Where(i => hienCo.ContainsKey(i.Khoa) || CaiDatRules.DaBiet(i.Khoa))
            .ToList();

        for (var i = 0; i < form.Items.Count; i++)
        {
            var loi = CaiDatRules.Validate(form.Items[i].Khoa, form.Items[i].GiaTri);
            if (loi is not null) ModelState.AddModelError($"Items[{i}].GiaTri", loi);
        }

        if (!ModelState.IsValid)
        {
            // Điền lại phần hiển thị (nhãn, mô tả); giá trị người dùng đã gõ vẫn được giữ trong ô
            form.Items = form.Items
                .Select(i => hienCo.TryGetValue(i.Khoa, out var c)
                    ? ItemTu(c, i.GiaTri)
                    : ItemTu(null, i.GiaTri, i.Khoa))
                .ToList();
            return View(nameof(Index), form);
        }

        var soThayDoi = 0;
        foreach (var item in form.Items)
        {
            var moi = item.GiaTri!.Trim();
            if (hienCo.TryGetValue(item.Khoa, out var cu) && moi == cu.GiaTri) continue;   // không đổi thì không ghi
            await _repo.SetAsync(item.Khoa, moi);
            soThayDoi++;
        }

        TempData["ThanhCong"] = soThayDoi == 0 ? "Không có thay đổi nào." : "Đã lưu cài đặt.";
        return RedirectToAction(nameof(Index));
    }

    private static CaiDatItemForm ItemTu(CaiDat? c, string? giaTri, string? khoa = null)
    {
        var key = c?.Khoa ?? khoa ?? string.Empty;
        var rule = CaiDatRules.Get(key);
        return new CaiDatItemForm
        {
            Khoa = key,
            GiaTri = giaTri,
            Nhan = rule.Nhan,
            MoTa = string.IsNullOrWhiteSpace(c?.MoTa) ? rule.MoTa : c!.MoTa,
            Loai = rule.Loai
        };
    }
}
