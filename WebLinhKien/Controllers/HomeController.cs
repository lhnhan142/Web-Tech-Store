using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebLinhKien.Models;
using WebLinhKien.Models.ViewModels.Client;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISanPhamRepository _sanPhamRepo;
        private readonly IDanhMucRepository _danhMucRepo;
        private readonly IThuongHieuRepository _thuongHieuRepo;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ISanPhamRepository sanPhamRepo,
            IDanhMucRepository danhMucRepo,
            IThuongHieuRepository thuongHieuRepo,
            ILogger<HomeController> logger)
        {
            _sanPhamRepo = sanPhamRepo;
            _danhMucRepo = danhMucRepo;
            _thuongHieuRepo = thuongHieuRepo;
            _logger = logger;
        }

        // GET: / (Trang chủ TECHZONE)
        public async Task<IActionResult> Index()
        {
            try
            {
                // Tải song song dữ liệu thực tế từ SQL Server qua các Repository
                var categoriesTask = _danhMucRepo.GetAllAsync(chiHienThi: true);
                var brandsTask = _thuongHieuRepo.GetAllAsync(chiHienThi: true);
                var featuredTask = _sanPhamRepo.GetFeaturedProductsAsync(limit: 8);
                var newArrivalsTask = _sanPhamRepo.GetNewArrivalsAsync(limit: 8);
                var bestDealsTask = _sanPhamRepo.GetBestDealsAsync(limit: 8);

                await Task.WhenAll(categoriesTask, brandsTask, featuredTask, newArrivalsTask, bestDealsTask);

                var vm = new HomeViewModel
                {
                    Categories = (await categoriesTask).Select(c => new CategoryCardViewModel(c)).ToList(),
                    Brands = (await brandsTask).Select(b => new BrandCardViewModel(b)).ToList(),
                    FeaturedProducts = (await featuredTask).Select(p => new ProductCardViewModel(p)).ToList(),
                    NewArrivals = (await newArrivalsTask).Select(p => new ProductCardViewModel(p)).ToList(),
                    BestDeals = (await bestDealsTask).Select(p => new ProductCardViewModel(p)).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu cho Trang chủ TECHZONE.");
                // Trả về ViewModel rỗng nếu DB gặp sự cố, view sẽ kích hoạt EmptyState thay vì crash
                return View(new HomeViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
