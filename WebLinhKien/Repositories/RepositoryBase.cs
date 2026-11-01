using WebLinhKien.Data;

namespace WebLinhKien.Repositories;

/// <summary>Lớp nền cho mọi repository: giữ DapperContext để tạo kết nối.</summary>
public abstract class RepositoryBase
{
    protected DapperContext Context { get; }

    protected RepositoryBase(DapperContext context) => Context = context;
}
