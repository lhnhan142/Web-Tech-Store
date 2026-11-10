using Microsoft.AspNetCore.Mvc;
using WebLinhKien.Helpers;
using WebLinhKien.Models;
using WebLinhKien.Models.Entities;
using WebLinhKien.Models.ViewModels.Client;
using WebLinhKien.Repositories.Interfaces;

namespace WebLinhKien.Controllers;

[Route("SanPham")]
public class SanPhamController : Controller
{
    private readonly ISanPhamRepository _sanPhamRepo;
    private readonly IDanhMucRepository _danhMucRepo;
    private readonly IThuongHieuRepository _thuongHieuRepo;
    private readonly ILogger<SanPhamController> _logger;

    public SanPhamController(
        ISanPhamRepository sanPhamRepo,
        IDanhMucRepository danhMucRepo,
        IThuongHieuRepository thuongHieuRepo,
        ILogger<SanPhamController> logger)
    {
        _sanPhamRepo = sanPhamRepo;
        _danhMucRepo = danhMucRepo;
        _thuongHieuRepo = thuongHieuRepo;
        _logger = logger;
    }

    // GET /SanPham?danhMuc=CPU&thuongHieu=INTEL&minPrice=1000000&maxPrice=5000000&sort=gia-tang&page=1
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] ProductFilterRequest request)
    {
        request ??= new ProductFilterRequest();

        // Xử lý giá trị minPrice / maxPrice nếu người dùng nhập có dấu chấm phân cách hàng nghìn (vd: 1.000.000)
        var hasMin = Request.Query.TryGetValue("minPrice", out var rawMinValues) && !string.IsNullOrWhiteSpace(rawMinValues.ToString());
        var hasMax = Request.Query.TryGetValue("maxPrice", out var rawMaxValues) && !string.IsNullOrWhiteSpace(rawMaxValues.ToString());

        if (hasMin)
        {
            request.MinPrice = ProductFilterRequest.ParsePrice(rawMinValues.ToString());
        }
        else if (Request.Query.ContainsKey("minPrice"))
        {
            request.MinPrice = null;
        }

        if (hasMax)
        {
            request.MaxPrice = ProductFilterRequest.ParsePrice(rawMaxValues.ToString());
        }
        else if (Request.Query.ContainsKey("maxPrice"))
        {
            request.MaxPrice = null;
        }

        // Chỉ hủy PriceRange khi người dùng thực sự nhập giá trị minPrice hoặc maxPrice khác rỗng
        if (hasMin || hasMax)
        {
            request.PriceRange = null;
        }

        request.Normalize();

        string? priceErrorMessage = null;
        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice.Value > request.MaxPrice.Value)
        {
            priceErrorMessage = "Giá tối thiểu không được lớn hơn giá tối đa. Vui lòng kiểm tra lại.";
        }

        try
        {
            var categories = await _danhMucRepo.GetAllAsync(chiHienThi: true);

            IReadOnlyList<ThuongHieuFilterItem> categoryBrands = Array.Empty<ThuongHieuFilterItem>();
            if (!string.IsNullOrEmpty(request.DanhMuc))
            {
                categoryBrands = await _sanPhamRepo.GetBrandsByCategoryAsync(request.DanhMuc);
                // Nếu người dùng chọn một thương hiệu không thuộc danh mục này -> tự động loại bỏ
                if (!string.IsNullOrEmpty(request.ThuongHieu) &&
                    !categoryBrands.Any(b => b.MaThuongHieu.Equals(request.ThuongHieu, StringComparison.OrdinalIgnoreCase)))
                {
                    request.ThuongHieu = null;
                }
            }

            var pagedEntity = await _sanPhamRepo.GetPagedProductsAsync(request);

            var cardItems = pagedEntity.Items.Select(p => new ProductCardViewModel(p)).ToList();
            var pagedResult = new PagedResult<ProductCardViewModel>(cardItems, pagedEntity.Page, pagedEntity.PageSize, pagedEntity.TotalItems);

            var currentCategory = !string.IsNullOrEmpty(request.DanhMuc)
                ? categories.FirstOrDefault(c => c.MaDanhMuc.Equals(request.DanhMuc, StringComparison.OrdinalIgnoreCase))
                : null;

            ThuongHieu? currentBrand = null;
            if (!string.IsNullOrEmpty(request.ThuongHieu))
            {
                currentBrand = await _thuongHieuRepo.GetByMaAsync(request.ThuongHieu);
                if (currentBrand == null)
                {
                    var matchFamous = categoryBrands.FirstOrDefault(b => b.MaThuongHieu.Equals(request.ThuongHieu, StringComparison.OrdinalIgnoreCase));
                    if (matchFamous != null)
                    {
                        currentBrand = new ThuongHieu { MaThuongHieu = matchFamous.MaThuongHieu, TenThuongHieu = matchFamous.TenThuongHieu, HienThi = true };
                    }
                }
            }

            var breadcrumbs = new List<BreadcrumbItem>
            {
                new("Sản phẩm", "/SanPham", false)
            };

            var pageTitle = "Tất cả linh kiện máy tính";
            string? pageDesc = "Kho linh kiện PC Gaming, đồ họa và văn phòng chính hãng giá tốt nhất tại TECHZONE.";

            if (!string.IsNullOrEmpty(request.Q))
            {
                pageTitle = $"Kết quả tìm kiếm: \"{request.Q}\"";
                pageDesc = $"Tìm thấy {pagedResult.TotalItems} sản phẩm phù hợp với từ khóa \"{request.Q}\".";
                breadcrumbs.Add(new($"Tìm kiếm: \"{request.Q}\"", null, true));
            }
            else if (currentCategory != null && currentBrand != null)
            {
                pageTitle = $"{currentCategory.TenDanhMuc} - {currentBrand.TenThuongHieu}";
                pageDesc = $"Danh sách linh kiện {currentCategory.TenDanhMuc} thương hiệu {currentBrand.TenThuongHieu} chính hãng.";
                breadcrumbs.Add(new(currentCategory.TenDanhMuc, $"/SanPham?danhMuc={currentCategory.MaDanhMuc}", false));
                breadcrumbs.Add(new(currentBrand.TenThuongHieu, null, true));
            }
            else if (currentCategory != null)
            {
                pageTitle = currentCategory.TenDanhMuc;
                pageDesc = $"Danh sách linh kiện {currentCategory.TenDanhMuc} chính hãng, bảo hành tới 36 tháng.";
                breadcrumbs.Add(new(currentCategory.TenDanhMuc, null, true));
            }
            else if (currentBrand != null)
            {
                pageTitle = $"Linh kiện thương hiệu {currentBrand.TenThuongHieu}";
                pageDesc = $"Tổng hợp các sản phẩm phần cứng máy tính từ thương hiệu {currentBrand.TenThuongHieu}.";
                breadcrumbs.Add(new(currentBrand.TenThuongHieu, null, true));
            }
            else
            {
                breadcrumbs.Add(new("Tất cả linh kiện", null, true));
            }

            var vm = new ProductListViewModel
            {
                PagedProducts = pagedResult,
                Filter = request,
                Categories = categories,
                Brands = categoryBrands.Select(b => new ThuongHieu { MaThuongHieu = b.MaThuongHieu, TenThuongHieu = b.TenThuongHieu, HienThi = true }).ToList(),
                CategoryBrands = categoryBrands,
                CurrentCategory = currentCategory,
                CurrentBrand = currentBrand,
                Breadcrumbs = breadcrumbs,
                PageTitle = pageTitle,
                PageDescription = pageDesc,
                PriceErrorMessage = priceErrorMessage
            };

            return View("Index", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi lọc danh sách sản phẩm.");
            return View("Index", new ProductListViewModel());
        }
    }

    // GET /SanPham/GetBrandsByCategory?maDanhMuc=CPU
    [HttpGet("GetBrandsByCategory")]
    public async Task<IActionResult> GetBrandsByCategory(string? maDanhMuc)
    {
        if (string.IsNullOrWhiteSpace(maDanhMuc))
        {
            return Json(new { success = true, data = Array.Empty<ThuongHieuFilterItem>() });
        }

        var brands = await _sanPhamRepo.GetBrandsByCategoryAsync(maDanhMuc);
        return Json(new { success = true, data = brands });
    }

    // GET /SanPham/DanhMuc/{maDanhMuc}
    [HttpGet("DanhMuc/{maDanhMuc}")]
    public Task<IActionResult> DanhMuc(string maDanhMuc, [FromQuery] ProductFilterRequest request)
    {
        request ??= new ProductFilterRequest();
        request.DanhMuc = maDanhMuc;
        return Index(request);
    }

    // GET /SanPham/ThuongHieu/{maThuongHieu}
    [HttpGet("ThuongHieu/{maThuongHieu}")]
    public Task<IActionResult> ThuongHieu(string maThuongHieu, [FromQuery] ProductFilterRequest request)
    {
        request ??= new ProductFilterRequest();
        request.ThuongHieu = maThuongHieu;
        return Index(request);
    }

    // GET /SanPham/ChiTiet/{id}
    [HttpGet("ChiTiet/{id:int}")]
    public async Task<IActionResult> ChiTiet(int id)
    {
        try
        {
            var sp = await _sanPhamRepo.GetByUidAsync(id);
            if (sp == null || !sp.HienThi)
            {
                return NotFound();
            }

            var imagesTask = _sanPhamRepo.GetProductImagesAsync(id);
            var specsTask = _sanPhamRepo.GetProductSpecificationsAsync(id);
            var relatedTask = _sanPhamRepo.GetRelatedProductsAsync(sp.MaDanhMuc, id, 4);
            var categoryTask = _danhMucRepo.GetByMaAsync(sp.MaDanhMuc);
            var brandTask = !string.IsNullOrEmpty(sp.MaThuongHieu)
                ? _thuongHieuRepo.GetByMaAsync(sp.MaThuongHieu)
                : Task.FromResult<ThuongHieu?>(null);

            await Task.WhenAll(imagesTask, specsTask, relatedTask, categoryTask, brandTask);

            var category = await categoryTask;
            var brand = await brandTask;

            var breadcrumbs = new List<BreadcrumbItem>
            {
                new("Sản phẩm", "/SanPham", false)
            };

            if (category != null)
            {
                breadcrumbs.Add(new(category.TenDanhMuc, $"/SanPham?danhMuc={category.MaDanhMuc}", false));
            }

            if (brand != null)
            {
                breadcrumbs.Add(new(brand.TenThuongHieu, $"/SanPham?thuongHieu={brand.MaThuongHieu}", false));
            }

            breadcrumbs.Add(new(sp.TenSanPham, null, true));

            var vm = new ProductDetailViewModel
            {
                SanPham = sp,
                DanhMuc = category,
                ThuongHieu = brand,
                HinhAnh = await imagesTask,
                ThongSo = await specsTask,
                SanPhamLienQuan = (await relatedTask).Select(p => new ProductCardViewModel(p)).ToList(),
                Breadcrumbs = breadcrumbs
            };

            return View("ChiTiet", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi tải trang chi tiết sản phẩm Uid = {Uid}", id);
            return NotFound();
        }
    }

    // GET /SanPham/QuickViewModal/{id} (Nội dung xem nhanh Ajax)
    [HttpGet("QuickViewModal/{id:int}")]
    public async Task<IActionResult> QuickViewModal(int id)
    {
        var sp = await _sanPhamRepo.GetByUidAsync(id);
        if (sp == null || !sp.HienThi)
            return Content("<div class='p-4 text-center text-danger'>Sản phẩm không tồn tại hoặc đã tạm ẩn.</div>");

        var specs = await _sanPhamRepo.GetProductSpecificationsAsync(id);
        var vm = new ProductDetailViewModel
        {
            SanPham = sp,
            ThongSo = specs
        };

        return PartialView("_QuickViewModalContent", vm);
    }

    // GET /SanPham/SoSanhModal?ids=1,2,3 (Modal so sánh đối chiếu cùng lúc tối đa 3 sản phẩm)
    [HttpGet("SoSanhModal")]
    public async Task<IActionResult> SoSanhModal(string? ids)
    {
        var vm = await BuildCompareViewModel(ids);
        return PartialView("_CompareModalContent", vm);
    }

    // GET /SanPham/SoSanh?ids=1,2,3 (Trang so sánh đầy đủ)
    [HttpGet("SoSanh")]
    public async Task<IActionResult> SoSanh(string? ids)
    {
        var vm = await BuildCompareViewModel(ids);
        return View("SoSanh", vm);
    }

    private async Task<CompareViewModel> BuildCompareViewModel(string? ids)
    {
        var vm = new CompareViewModel();
        if (string.IsNullOrWhiteSpace(ids)) return vm;

        var uidList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => int.TryParse(s.Trim(), out var u) ? u : 0)
                         .Where(u => u > 0)
                         .Distinct()
                         .Take(3)
                         .ToList();

        if (uidList.Count == 0) return vm;

        var products = (await _sanPhamRepo.GetByUidsAsync(uidList))
                       .Where(p => p.HienThi)
                       .ToList();

        vm.SanPhams = products;

        // Tải thông số kỹ thuật cho từng sản phẩm
        var specsDict = new Dictionary<int, IReadOnlyList<ThongSoKyThuat>>();
        foreach (var sp in products)
        {
            specsDict[sp.Uid] = await _sanPhamRepo.GetProductSpecificationsAsync(sp.Uid);
        }

        // Gom toàn bộ tên thuộc tính độc nhất theo thứ tự xuất hiện
        var allSpecNames = specsDict.Values
            .SelectMany(list => list)
            .OrderBy(s => s.Ordering)
            .Select(s => s.TenThongSo)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var specName in allSpecNames)
        {
            var row = new CompareSpecRow { TenThongSo = specName };
            foreach (var sp in products)
            {
                var match = specsDict[sp.Uid].FirstOrDefault(s => s.TenThongSo.Equals(specName, StringComparison.OrdinalIgnoreCase));
                row.GiaTriTheoSanPham[sp.Uid] = match?.GiaTri ?? "-";
            }
            vm.HangThongSo.Add(row);
        }

        return vm;
    }

    // POST /SanPham/ThemVaoGio
    [HttpPost("ThemVaoGio")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemVaoGio([FromForm] int uid, [FromForm] int soLuong = 1, [FromForm] bool muaNgay = false)
    {
        var sp = await _sanPhamRepo.GetByUidAsync(uid);
        if (sp == null || !sp.HienThi || sp.TonKho <= 0)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã hết hàng trong kho." });
        }

        soLuong = Math.Clamp(soLuong, 1, Math.Min(WebConstants.SoLuongToiDa, sp.TonKho));
        var gio = GioHangSession.Doc(HttpContext.Session);
        var item = gio.FirstOrDefault(g => g.UidSanPham == uid);
        if (item != null)
        {
            item.SoLuong = Math.Clamp(item.SoLuong + soLuong, 1, Math.Min(WebConstants.SoLuongToiDa, sp.TonKho));
        }
        else
        {
            gio.Add(new GioHangItem { UidSanPham = uid, SoLuong = soLuong });
        }
        GioHangSession.Ghi(HttpContext.Session, gio);

        var tongSoLuong = gio.Sum(g => g.SoLuong);

        if (muaNgay)
        {
            return Json(new
            {
                success = true,
                redirectUrl = Url.Content("~/DatHang"),
                totalCount = tongSoLuong
            });
        }

        return Json(new
        {
            success = true,
            message = $"Đã thêm {soLuong} sản phẩm \"{sp.TenSanPham}\" vào giỏ hàng!",
            totalCount = tongSoLuong
        });
    }

    // GET /SanPham/GoiY?q=i5
    [HttpGet("GoiY")]
    public async Task<IActionResult> GoiY(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 1)
            return Json(new { success = true, data = Array.Empty<object>() });

        var results = await _sanPhamRepo.SearchSuggestionsAsync(q.Trim(), limit: 6);

        var data = results.Select(p => new
        {
            uid = p.Uid,
            ma = p.MaSanPham,
            ten = p.TenSanPham,
            gia = p.GiaBan,
            giaFormatted = p.GiaBan.ToVnd(),
            giaGocFormatted = p.GiaGoc > p.GiaBan ? p.GiaGoc.ToVnd() : null,
            anh = p.AnhDaiDien,
            icon = CategoryIconHelper.GetIcon(p.MaDanhMuc),
            url = Url.Content($"~/SanPham/ChiTiet/{p.Uid}")
        });

        return Json(new { success = true, data });
    }
}
