using WebLinhKien.Models.Entities;

namespace WebLinhKien.Repositories.Interfaces;

public partial interface ITinhTrangDonHangRepository
{
    Task<IReadOnlyList<TinhTrangDonHang>> GetAllAsync();
}
