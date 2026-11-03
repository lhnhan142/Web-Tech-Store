using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Models.ViewModels;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Areas.Admin.Controllers;

public class HomeController : AdminController
{
    private readonly IDonDatHangRepository _donRepo;
    private readonly ITinhTrangDonHangRepository _tinhTrangRepo;

    public HomeController(IDonDatHangRepository donRepo, ITinhTrangDonHangRepository tinhTrangRepo)
    {
        _donRepo = donRepo;
        _tinhTrangRepo = tinhTrangRepo;
    }

    public async Task<IActionResult> Index()
    {
        var donMoiNhat = await _donRepo.GetPagedAsync(null, null, 1, 6);

        var vm = new DashboardViewModel
        {
            DonMoiNhat = donMoiNhat.Items,
            TinhTrangs = await _tinhTrangRepo.GetAllAsync(),
            DemTheoTinhTrang = await _donRepo.DemTheoTinhTrangAsync()
        };
        return View(vm);
    }
}
