using Microsoft.Data.SqlClient;

namespace WebLinhKien.Data;

/// <summary>
/// Tạo kết nối SQL Server cho Dapper. Đăng ký Singleton; mỗi lần cần truy vấn
/// gọi CreateConnection() và dùng "using" để đóng kết nối.
/// Trả về SqlConnection (không phải IDbConnection) để dùng được OpenAsync và
/// BeginTransactionAsync khi cần transaction (ví dụ: đặt hàng + trừ tồn kho).
/// </summary>
public class DapperContext
{
    public const string ConnectionName = "WebLinhKienCST";

    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Thiếu chuỗi kết nối '{ConnectionName}'. " +
                "Cấu hình bằng: dotnet user-secrets set \"ConnectionStrings:" + ConnectionName + "\" \"...\"");
    }

    public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
}
