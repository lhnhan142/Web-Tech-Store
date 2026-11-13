using WebLinhKien.Models.ViewModels.BuildPc;

namespace WebLinhKien.Services;

public interface IBuildPcService
{
    /// <summary>Dữ liệu cho trang /BuildPc: các bước chọn, phụ kiện, ngân sách mặc định.</summary>
    Task<BuildPcIndexViewModel> LayTrangAsync();

    /// <summary>Kiểm tra cấu hình đang chọn: thiếu linh kiện, xung đột tương thích, tồn kho, phụ kiện.</summary>
    Task<BuildPcChonKetQua> KiemTraAsync(BuildPcChonRequest? chon);

    /// <summary>Gợi ý ít nhất 2 cấu hình nằm trong ngân sách, ưu tiên sản phẩm còn hàng và tương thích.</summary>
    Task<BuildPcGoiYKetQua> GoiYTheoNganSachAsync(BuildPcGoiYRequest? request);
}
