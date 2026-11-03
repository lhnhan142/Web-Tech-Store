namespace WebLinhKien.Repositories.Interfaces;

public partial interface ISanPhamRepository
{
    /// <summary>Số sản phẩm theo từng danh mục (khóa không phân biệt hoa thường).</summary>
    Task<Dictionary<string, int>> DemTheoDanhMucAsync();

    /// <summary>Số sản phẩm theo từng thương hiệu (bỏ qua sản phẩm chưa có thương hiệu).</summary>
    Task<Dictionary<string, int>> DemTheoThuongHieuAsync();
}
