using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

public partial interface IDanhMucRepository
{
    Task<IReadOnlyList<DanhMuc>> GetAllAsync(bool chiHienThi = true);
    Task<DanhMuc?> GetByMaAsync(string maDanhMuc);
    Task<bool> InsertAsync(DanhMuc danhMuc);
    Task<bool> UpdateAsync(DanhMuc danhMuc);
    Task<bool> DeleteAsync(string maDanhMuc);
}
