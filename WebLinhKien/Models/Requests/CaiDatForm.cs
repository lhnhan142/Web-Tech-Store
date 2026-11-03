using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebLinhKien.Helpers;

namespace WebLinhKien.Models.Requests;

public class CaiDatForm
{
    public List<CaiDatItemForm> Items { get; set; } = new();
}

public class CaiDatItemForm
{
    // Hai trường này được gửi lên từ form
    public string Khoa { get; set; } = string.Empty;
    public string? GiaTri { get; set; }

    // Các trường chỉ để hiển thị, server tự điền, không nhận từ trình duyệt
    [BindNever] public string Nhan { get; set; } = string.Empty;
    [BindNever] public string? MoTa { get; set; }
    [BindNever] public CaiDatLoai Loai { get; set; }
}
