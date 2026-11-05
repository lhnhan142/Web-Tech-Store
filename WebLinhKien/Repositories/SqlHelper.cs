namespace WebLinhKien.Repositories;

internal static class SqlHelper
{
    // Thoát ký tự đặc biệt của LIKE để người dùng gõ % hoặc _ vẫn tìm như chữ thường
    public static string EscapeLike(string s) =>
        s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
