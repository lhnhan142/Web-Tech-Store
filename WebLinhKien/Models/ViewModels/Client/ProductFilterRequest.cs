using System.Globalization;

namespace WebLinhKien.Models.ViewModels.Client;

/// <summary>
/// Model nhận các tham số lọc, tìm kiếm, sắp xếp và phân trang từ Query String trên URL.
/// Ví dụ: /SanPham?danhMuc=CPU&thuongHieu=INTEL&minPrice=1000000&maxPrice=5000000&sort=gia-tang&page=2&pageSize=12
/// </summary>
public class ProductFilterRequest
{
    /// <summary>Từ khóa tìm kiếm (tên hoặc mã sản phẩm)</summary>
    public string? Q { get; set; }

    /// <summary>Mã danh mục cần lọc (CPU, VGA, RAM...)</summary>
    public string? DanhMuc { get; set; }

    /// <summary>Mã thương hiệu cần lọc (INTEL, AMD, ASUS...)</summary>
    public string? ThuongHieu { get; set; }

    /// <summary>Giá thấp nhất</summary>
    public decimal? MinPrice { get; set; }

    /// <summary>Giá cao nhất</summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>Khoảng giá nhanh được chọn (vd: "0-1000000", "1000000-5000000", "tren-20000000")</summary>
    public string? PriceRange { get; set; }

    /// <summary>Chỉ hiển thị sản phẩm còn hàng (TonKho > 0)</summary>
    public bool? ConHang { get; set; }

    /// <summary>Tiêu chí sắp xếp: "default", "gia-tang", "gia-giam", "moi-nhat", "ten-az"</summary>
    public string? Sort { get; set; } = "default";

    /// <summary>Trang hiện tại (bắt đầu từ 1)</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số sản phẩm trên 1 trang: 12, 24, 36</summary>
    public int PageSize { get; set; } = 12;

    /// <summary>Hàm tiện ích chuyển đổi chuỗi giá (hỗ trợ phân cách hàng nghìn bằng dấu chấm hoặc phẩy) sang decimal</summary>
    public static decimal? ParsePrice(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var cleaned = input.Trim()
                           .Replace("đ", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("vnd", "", StringComparison.OrdinalIgnoreCase)
                           .Replace(".", "")
                           .Replace(",", "")
                           .Trim();

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result >= 0 ? result : 0;
        }
        return null;
    }

    /// <summary>Chuẩn hóa dữ liệu đầu vào và phân tích PriceRange nếu có</summary>
    public void Normalize()
    {
        Q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();
        DanhMuc = string.IsNullOrWhiteSpace(DanhMuc) ? null : DanhMuc.Trim().ToUpperInvariant();
        ThuongHieu = string.IsNullOrWhiteSpace(ThuongHieu) ? null : ThuongHieu.Trim().ToUpperInvariant();
        Sort = string.IsNullOrWhiteSpace(Sort) ? "default" : Sort.Trim().ToLowerInvariant();

        if (Page < 1) Page = 1;
        if (PageSize != 12 && PageSize != 24 && PageSize != 36) PageSize = 12;

        // Phân tích khoảng giá nhanh nếu người dùng click chọn mức giá có sẵn
        if (!string.IsNullOrWhiteSpace(PriceRange))
        {
            var parts = PriceRange.Split('-');
            if (parts.Length == 2)
            {
                if (decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var min)) MinPrice = min;
                if (decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var max)) MaxPrice = max;
            }
            else if (PriceRange.StartsWith("tren-", StringComparison.OrdinalIgnoreCase) &&
                     decimal.TryParse(PriceRange.Substring(5), NumberStyles.Any, CultureInfo.InvariantCulture, out var minAbove))
            {
                MinPrice = minAbove;
                MaxPrice = null;
            }
            else if (PriceRange.StartsWith("duoi-", StringComparison.OrdinalIgnoreCase) &&
                     decimal.TryParse(PriceRange.Substring(5), NumberStyles.Any, CultureInfo.InvariantCulture, out var maxBelow))
            {
                MinPrice = 0;
                MaxPrice = maxBelow;
            }
        }

        if (MinPrice.HasValue && MinPrice.Value < 0) MinPrice = 0;
        if (MaxPrice.HasValue && MaxPrice.Value < 0) MaxPrice = 0;
    }

    /// <summary>Chuyển các điều kiện lọc hiện tại thành Dictionary để giữ nguyên trên Pagination link</summary>
    public Dictionary<string, string?> ToRouteDictionary()
    {
        var dict = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(Q)) dict["q"] = Q;
        if (!string.IsNullOrEmpty(DanhMuc)) dict["danhMuc"] = DanhMuc;
        if (!string.IsNullOrEmpty(ThuongHieu)) dict["thuongHieu"] = ThuongHieu;

        // Chỉ đưa vào 1 phương thức lọc giá để tránh xung đột hoặc ghi đè khi chuyển trang
        if (!string.IsNullOrEmpty(PriceRange))
        {
            dict["priceRange"] = PriceRange;
        }
        else
        {
            if (MinPrice.HasValue) dict["minPrice"] = MinPrice.Value.ToString("0");
            if (MaxPrice.HasValue) dict["maxPrice"] = MaxPrice.Value.ToString("0");
        }

        if (ConHang.HasValue) dict["conHang"] = ConHang.Value.ToString().ToLowerInvariant();
        if (!string.IsNullOrEmpty(Sort) && Sort != "default") dict["sort"] = Sort;
        if (PageSize != 12) dict["pageSize"] = PageSize.ToString();
        return dict;
    }
}

