using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

/// <summary>Đọc dữ liệu cho PC Builder: sản phẩm và thông số kỹ thuật theo danh mục / theo uid.</summary>
public interface IBuildPcRepository
{
    Task<IReadOnlyList<SanPham>> GetByCategoriesAsync(IReadOnlyCollection<string> maDanhMuc);

    Task<IReadOnlyList<ThongSoKyThuat>> GetThongSoTheoDanhMucAsync(IReadOnlyCollection<string> maDanhMuc);

    Task<IReadOnlyList<SanPham>> GetByUidsAsync(IReadOnlyCollection<int> uids);

    Task<IReadOnlyList<ThongSoKyThuat>> GetThongSoTheoUidsAsync(IReadOnlyCollection<int> uids);
}
