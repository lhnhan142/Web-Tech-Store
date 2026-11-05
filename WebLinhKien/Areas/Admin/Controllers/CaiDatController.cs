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

    public CaiDatController(ICaiDatRepository repo) => _repo = repo;

    public async Task<IActionResult> Index()
    {
        var caiDats = await _repo.GetAllAsync();
        return View(new CaiDatForm { Items = caiDats.Select(c => ItemTu(c, c.GiaTri)).ToList() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(CaiDatForm form)
    {
        var hienCo = (await _repo.GetAllAsync()).ToDictionary(c => c.Khoa);

        // Chỉ nhận các khóa ĐÃ CÓ trong database: người dùng không thể thêm khóa lạ bằng cách sửa form
        form.Items = (form.Items ?? new()).Where(i => hienCo.ContainsKey(i.Khoa)).ToList();

        for (var i = 0; i < form.Items.Count; i++)
        {
            var loi = CaiDatRules.Validate(form.Items[i].Khoa, form.Items[i].GiaTri);
            if (loi is not null) ModelState.AddModelError($"Items[{i}].GiaTri", loi);
        }

        if (!ModelState.IsValid)
        {
            // Điền lại phần hiển thị (nhãn, mô tả); giá trị người dùng đã gõ vẫn được giữ trong ô
            form.Items = form.Items.Select(i => ItemTu(hienCo[i.Khoa], i.GiaTri)).ToList();
            return View(nameof(Index), form);
        }

        var soThayDoi = 0;
        foreach (var item in form.Items)
        {
            var moi = item.GiaTri!.Trim();
            if (moi == hienCo[item.Khoa].GiaTri) continue;   // không đổi thì không ghi
            await _repo.SetAsync(item.Khoa, moi);
            soThayDoi++;
        }

        TempData["ThanhCong"] = soThayDoi == 0 ? "Không có thay đổi nào." : "Đã lưu cài đặt.";
        return RedirectToAction(nameof(Index));
    }

    private static CaiDatItemForm ItemTu(CaiDat c, string? giaTri)
    {
        var rule = CaiDatRules.Get(c.Khoa);
        return new CaiDatItemForm
        {
            Khoa = c.Khoa,
            GiaTri = giaTri,
            Nhan = rule.Nhan,
            MoTa = c.MoTa,
            Loai = rule.Loai
        };
    }
}
