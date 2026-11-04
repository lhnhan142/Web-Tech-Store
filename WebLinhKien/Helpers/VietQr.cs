using System.Text;
using QRCoder;
using SkiaSharp;

namespace WebLinhKien.Helpers;

/// <summary>
/// Tạo mã VietQR (chuẩn EMV QR, NAPAS 247) cho chuyển khoản ngân hàng.
/// QRCoder tạo ma trận QR, SkiaSharp vẽ thành ảnh PNG. Chuyển từ Utils.cs của dự án WebBanSach.
/// </summary>
public static class VietQr
{
    // Khóa trong bảng CaiDats (thêm bằng Database/cai-dat-thanh-toan.sql, sửa được ở trang Cài đặt của admin)
    public const string KeyMaNganHang = "QrMaNganHang";     // ví dụ VCB, MB, OCB, ACB
    public const string KeyTenNganHang = "NganHangTen";     // tên hiển thị cho khách
    public const string KeySoTaiKhoan = "SoTaiKhoan";
    public const string KeyChuTaiKhoan = "ChuTaiKhoan";

    /// <summary>Tạo chuỗi nội dung của mã QR. Ném ArgumentException nếu không biết mã ngân hàng.</summary>
    public static string TaoNoiDung(string maNganHang, string soTaiKhoan, long soTien, string noiDung)
    {
        var bin = LayBin(maNganHang);
        if (bin.Length == 0) throw new ArgumentException($"Không tìm thấy mã ngân hàng: {maNganHang}");

        var taiKhoan = "00" + Do(bin) + "01" + Do(soTaiKhoan);                       // BIN + số tài khoản
        var merchant = "0010A000000727" + "01" + Do(taiKhoan) + "0208QRIBFTTA";      // NAPAS, chuyển nhanh về tài khoản
        var truong38 = "38" + Do(merchant);
        var truong62 = "62" + Do("08" + Do(noiDung));                                // nội dung chuyển khoản
        var soTienStr = soTien.ToString();

        var khongCrc = "000201" + "010212" + truong38 + "5303704" + "54" + Do(soTienStr) + "5802VN" + truong62 + "6304";
        return khongCrc + Crc16(khongCrc);
    }

    /// <summary>Vẽ mã QR thành ảnh PNG (nền trắng, viền yên tĩnh 4 ô như chuẩn QR).</summary>
    public static byte[] TaoPng(string noiDung, int kichThuoc = 320, string mauMa = "#111827")
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(noiDung, QRCodeGenerator.ECCLevel.M);
        var matrix = data.ModuleMatrix;

        const int vien = 4;
        var tongO = matrix.Count + vien * 2;
        var canh = Math.Max(1, kichThuoc / tongO);        // mỗi ô một số nguyên pixel để ảnh sắc nét
        var size = canh * tongO;

        using var surface = SKSurface.Create(new SKImageInfo(size, size));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColor.Parse(mauMa), IsAntialias = false };

        for (var y = 0; y < matrix.Count; y++)
            for (var x = 0; x < matrix.Count; x++)
                if (matrix[y][x])
                    canvas.DrawRect(SKRect.Create((x + vien) * canh, (y + vien) * canh, canh, canh), paint);

        using var image = surface.Snapshot();
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    // Trường TLV: độ dài 2 chữ số + giá trị
    private static string Do(string v) => v.Length.ToString("D2") + v;

    private static string Crc16(string text)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc.ToString("X4");
    }

    /// <summary>Mã BIN ngân hàng theo danh mục của NAPAS. Cần thêm ngân hàng nào thì thêm một dòng.</summary>
    private static string LayBin(string ma) => ma.Trim().ToUpperInvariant() switch
    {
        "ICB" or "VIETINBANK" => "970415",
        "VCB" or "VIETCOMBANK" => "970436",
        "BIDV" => "970418",
        "VBA" or "AGRIBANK" => "970405",
        "OCB" => "970448",
        "MB" or "MBBANK" => "970422",
        "TCB" or "TECHCOMBANK" => "970407",
        "ACB" => "970416",
        "VPB" or "VPBANK" => "970432",
        "TPB" or "TPBANK" => "970423",
        "STB" or "SACOMBANK" => "970403",
        "HDB" or "HDBANK" => "970437",
        "VIB" => "970441",
        "SHB" => "970443",
        "EIB" or "EXIMBANK" => "970431",
        "MSB" => "970426",
        "SCB" => "970429",
        "SEAB" or "SEABANK" => "970440",
        "LPB" or "LPBANK" => "970449",
        "NCB" => "970419",
        "ABB" or "ABBANK" => "970425",
        "BAB" or "BACABANK" => "970409",
        "VAB" or "VIETABANK" => "970427",
        "NAB" or "NAMABANK" => "970428",
        "PGB" or "PGBANK" => "970430",
        "KLB" or "KIENLONGBANK" => "970452",
        "CAKE" => "546034",
        "UBANK" => "546035",
        "TIMO" => "963388",
        _ => string.Empty
    };
}
