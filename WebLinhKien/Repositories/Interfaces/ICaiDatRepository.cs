using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

public partial interface ICaiDatRepository
{
    Task<IReadOnlyList<CaiDat>> GetAllAsync();
    Task<string?> GetGiaTriAsync(string khoa);
    Task<int> GetIntAsync(string khoa, int macDinh = 0);
    Task<decimal> GetDecimalAsync(string khoa, decimal macDinh = 0);
    /// <summary>Cập nhật giá trị; nếu khóa chưa có thì thêm mới.</summary>
    Task SetAsync(string khoa, string giaTri);
}
