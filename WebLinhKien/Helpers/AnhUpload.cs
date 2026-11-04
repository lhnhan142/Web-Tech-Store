namespace WebLinhKien.Helpers;

/// <summary>
/// Lưu và xóa ảnh tải lên trong wwwroot/uploads. Chỉ nhận jpg, png, gif, webp (kiểm tra cả phần đầu file,
/// không tin đuôi file), tối đa 5 MB, tên file do server tự đặt.
/// </summary>
public static class AnhUpload
{
    public const string ThuMucGoc = "uploads";
    public const long DungLuongToiDa = 5 * 1024 * 1024;

    private static readonly HashSet<string> DuoiChoPhep = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    /// <summary>Trả về (đường dẫn web như "/uploads/san-pham/abc.jpg", null) hoặc (null, thông báo lỗi).</summary>
    public static async Task<(string? DuongDan, string? Loi)> LuuAsync(IFormFile file, string webRoot, string thuMucCon)
    {
        if (file is null || file.Length == 0) return (null, "File rỗng.");
        if (file.Length > DungLuongToiDa) return (null, $"Ảnh {TenHienThi(file)} lớn hơn 5 MB.");

        var duoi = Path.GetExtension(file.FileName);
        if (!DuoiChoPhep.Contains(duoi)) return (null, $"Ảnh {TenHienThi(file)} phải là jpg, png, gif hoặc webp.");

        var dauFile = new byte[12];
        await using (var doc = file.OpenReadStream())
        {
            var daDoc = await doc.ReadAsync(dauFile.AsMemory(0, dauFile.Length));
            if (daDoc < dauFile.Length || !LaAnhThat(dauFile))
                return (null, $"File {TenHienThi(file)} không phải ảnh hợp lệ.");
        }

        var thuMuc = Path.Combine(webRoot, ThuMucGoc, thuMucCon);
        Directory.CreateDirectory(thuMuc);

        var tenMoi = Guid.NewGuid().ToString("N") + duoi.ToLowerInvariant();
        await using (var ra = File.Create(Path.Combine(thuMuc, tenMoi)))
        {
            await file.CopyToAsync(ra);
        }

        return ($"/{ThuMucGoc}/{thuMucCon}/{tenMoi}", null);
    }

    /// <summary>Xóa file ảnh trên đĩa. Chỉ xóa file nằm trong wwwroot/uploads; đường dẫn ngoài (http, thư mục khác) bị bỏ qua.</summary>
    public static void Xoa(string? duongDan, string webRoot)
    {
        if (string.IsNullOrWhiteSpace(duongDan) || !duongDan.StartsWith($"/{ThuMucGoc}/", StringComparison.Ordinal)) return;

        try
        {
            var goc = Path.GetFullPath(Path.Combine(webRoot, ThuMucGoc)) + Path.DirectorySeparatorChar;
            var file = Path.GetFullPath(Path.Combine(webRoot, duongDan.TrimStart('/')));
            if (file.StartsWith(goc, StringComparison.OrdinalIgnoreCase) && File.Exists(file)) File.Delete(file);
        }
        catch (IOException) { /* file đang bị khóa hoặc đã mất: bỏ qua, không làm hỏng thao tác chính */ }
        catch (UnauthorizedAccessException) { }
    }

    private static string TenHienThi(IFormFile f) => Path.GetFileName(f.FileName);

    private static bool LaAnhThat(byte[] b) =>
        (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) ||                                   // JPEG
        (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) ||                   // PNG
        (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) ||                   // GIF8
        (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
         b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50);                   // RIFF....WEBP
}
