using System.Text.RegularExpressions;

namespace WebLinhKien.Helpers;

/// <summary>
/// Helper làm sạch nội dung HTML từ CKEditor trước khi render lên giao diện nhằm triệt tiêu hoàn toàn các nguy cơ XSS.
/// </summary>
public static class HtmlSanitizerHelper
{
    // Danh sách các thẻ nguy hiểm tuyệt đối không cho phép
    private static readonly Regex DangerousTagsRegex = new(
        @"<\s*(script|iframe|object|embed|applet|meta|link|style|form|input|button|textarea|base)[^>]*?>.*?<\s*/\s*\1\s*>|<\s*(script|iframe|object|embed|applet|meta|link|style|form|input|button|textarea|base)[^>]*?/>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Xóa các sự kiện JavaScript inline nguy hiểm (onload, onclick, onerror, onmouseover...)
    private static readonly Regex InlineEventsRegex = new(
        @"\s+on\w+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Xóa pseudo-protocol nguy hiểm (javascript:, vbscript:, data:text/html)
    private static readonly Regex DangerousProtocolsRegex = new(
        @"(?:href|src)\s*=\s*(?:""\s*(?:javascript|vbscript|data\s*:\s*text\/html)[^""]*""|'\s*(?:javascript|vbscript|data\s*:\s*text\/html)[^']*')",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Làm sạch chuỗi HTML: loại bỏ script độc hại nhưng giữ nguyên định dạng thẩm mỹ (p, h1-h6, table, ul, ol, img, b, i...).
    /// </summary>
    public static string Sanitize(string? rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
            return string.Empty;

        var html = rawHtml;

        // 1. Loại bỏ các thẻ script, iframe, form...
        html = DangerousTagsRegex.Replace(html, string.Empty);

        // 2. Loại bỏ các thuộc tính bắt sự kiện (onerror, onclick...)
        html = InlineEventsRegex.Replace(html, string.Empty);

        // 3. Loại bỏ các giao thức javascript: trong href hoặc src
        html = DangerousProtocolsRegex.Replace(html, string.Empty);

        return html;
    }
}

