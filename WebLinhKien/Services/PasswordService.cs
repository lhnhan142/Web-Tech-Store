using Microsoft.AspNetCore.Identity;
using WebLinhKien.Models.Entities;

namespace WebLinhKien.Services;

/// <summary>
/// Dùng PasswordHasher có sẵn của ASP.NET Core (PBKDF2 + salt ngẫu nhiên, có phiên bản
/// định dạng). Không tự viết thuật toán băm. Chỉ dùng lớp băm, không cần cài ASP.NET Identity.
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<TaiKhoan> _hasher = new();
    private readonly string _dummyHash;

    public PasswordService()
    {
        _dummyHash = _hasher.HashPassword(new TaiKhoan(), Guid.NewGuid().ToString("N"));
    }

    public string Hash(string matKhau) => _hasher.HashPassword(new TaiKhoan(), matKhau);

    public bool Verify(TaiKhoan? taiKhoan, string matKhau)
    {
        if (taiKhoan is null)
        {
            _hasher.VerifyHashedPassword(new TaiKhoan(), _dummyHash, matKhau);
            return false;
        }

        var ketQua = _hasher.VerifyHashedPassword(taiKhoan, taiKhoan.MatKhauHash, matKhau);
        return ketQua != PasswordVerificationResult.Failed;
    }
}
