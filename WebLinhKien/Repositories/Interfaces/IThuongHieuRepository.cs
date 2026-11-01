using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

public partial interface IThuongHieuRepository
{
    Task<IReadOnlyList<ThuongHieu>> GetAllAsync(bool chiHienThi = true);
    Task<ThuongHieu?> GetByMaAsync(string maThuongHieu);
    Task<bool> InsertAsync(ThuongHieu thuongHieu);
    Task<bool> UpdateAsync(ThuongHieu thuongHieu);
    Task<bool> DeleteAsync(string maThuongHieu);
}
