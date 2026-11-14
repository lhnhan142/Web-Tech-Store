# WebLinhKien – Website bán linh kiện máy tính

Website bán linh kiện và phụ kiện máy tính (CPU, mainboard, RAM, VGA, ổ cứng, nguồn, case, màn hình...) viết bằng **ASP.NET Core MVC (.NET 10)**, **Dapper** và **SQL Server**. Trang khách hàng có danh mục, tìm kiếm, lọc theo thương hiệu/giá, xem thông số kỹ thuật, giỏ hàng và đặt hàng. Trang quản trị có **CKEditor** để soạn mô tả sản phẩm và nhận **thông báo đơn hàng mới theo thời gian thực** qua WebSocket.

## Tính năng

### Trang khách hàng

- **Trang chủ** hiển thị sản phẩm nổi bật, sản phẩm mới và khuyến mãi lấy từ database.
- **Danh mục và thương hiệu**: lọc theo loại linh kiện, thương hiệu, khoảng giá; sắp xếp theo giá/mới nhất; phân trang.
- **Tìm kiếm** sản phẩm theo tên hoặc mã sản phẩm.
- **Chi tiết sản phẩm**: bộ ảnh, giá gốc/giá bán, tình trạng kho, **bảng thông số kỹ thuật** và phần mô tả HTML (soạn từ CKEditor).
- **Giỏ hàng** lưu trong session, cập nhật số lượng không cần tải lại trang.
- **Đặt hàng** (COD hoặc chuyển khoản):
  - gửi bằng `fetch`, có spinner trong lúc gửi;
  - validate ở cả client (JavaScript) và server (FluentValidation);
  - chống CSRF bằng `[ValidateAntiForgeryToken]`;
  - giá và tổng tiền luôn tính lại ở server từ database;
  - kiểm tra tồn kho và trừ kho trong cùng một transaction.
- **Tiện ích tạo mã QR** chuyển khoản ngân hàng (QRCoder + SkiaSharp) trong `Helpers/Utils.cs`.

### Trang quản trị (`/Admin`)

- **Đăng nhập** bằng cookie authentication, mật khẩu được băm (hash) trước khi lưu.
- **Quản lý sản phẩm, danh mục, thương hiệu**: thêm, sửa, xóa, bật/tắt hiển thị, upload ảnh.
- **CKEditor** cho phần mô tả sản phẩm (có upload ảnh trực tiếp vào bài viết).
- **Quản lý thông số kỹ thuật** của từng sản phẩm theo dạng key/value (ví dụ: Socket = LGA1700, Dung lượng = 16GB).
- **Quản lý đơn hàng**: xem chi tiết, đổi tình trạng đơn (Mới → Đang xử lý → Đang giao → Đã nhận / Hoàn trả).
- **Thông báo thời gian thực (WebSocket)**: khi có đơn hàng mới, trang quản trị hiện thông báo và cập nhật danh sách đơn ngay, không cần tải lại trang.
- **Cảnh báo sắp hết hàng** khi tồn kho thấp hơn ngưỡng cấu hình.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core MVC (.NET 10) |
| Truy cập dữ liệu | Dapper + Microsoft.Data.SqlClient (SQL Server) |
| Validation | FluentValidation.DependencyInjectionExtensions |
| Thời gian thực | WebSocket (middleware `UseWebSockets` của ASP.NET Core) |
| Giao diện | Razor View, Tailwind CSS v4 (bản browser), Font Awesome |
| Soạn thảo nội dung | CKEditor (trang quản trị) |
| JavaScript | Vanilla JS, jQuery + Slick carousel (banner, sản phẩm nổi bật) |
| Mã QR | QRCoder, SkiaSharp |

> Dapper không có migration. Cấu trúc bảng và dữ liệu mẫu nằm trong thư mục `Database/` dưới dạng script SQL (xem phần *Tạo database*).

## Cấu trúc thư mục

```
WebLinhKien/
├── WebLinhKien.slnx
├── Database/               # schema.sql (tạo bảng), seed.sql (dữ liệu mẫu)
└── WebLinhKien/
    ├── Areas/
    │   └── Admin/          # Controllers, Views của trang quản trị
    ├── Controllers/        # HomeController, SanPhamController, GioHangController, DatHang...
    ├── Helpers/            # WebConstants, Utils (tạo mã QR, hash mật khẩu)
    ├── Data/               # DapperContext (tạo IDbConnection)
    ├── Models/
    │   ├── Entities/       # Các lớp ánh xạ với bảng trong database
    │   ├── Requests/       # DatHangRequest, SanPhamRequest...
    │   └── Responses/      # Responses (Status, Message, Error)
    ├── Repositories/       # ISanPhamRepository / SanPhamRepository... – toàn bộ câu lệnh SQL (Dapper)
    ├── Services/           # Xử lý nghiệp vụ: giỏ hàng, đặt hàng, tồn kho
    ├── WebSockets/         # WebSocketHandler, ConnectionManager (quản lý kết nối admin)
    ├── Validators/         # FluentValidation
    ├── Views/              # Razor views phía khách hàng
    └── wwwroot/            # css, js, images, uploads
```

Controller không viết SQL trực tiếp. Mọi truy vấn nằm trong `Repositories` (Dapper), nghiệp vụ nằm trong `Services`, cả hai được inject vào controller qua interface.

## Database

Các bảng chỉ dùng khóa chính, không thiết lập relationship. Việc ghép dữ liệu giữa các bảng được làm bằng câu lệnh `JOIN` trong Dapper hoặc trong code C#.

| Bảng | Khóa chính | Mô tả |
|---|---|---|
| `DanhMucs` | `MaDanhMuc` | Loại linh kiện: CPU, Mainboard, RAM, VGA, SSD... |
| `ThuongHieus` | `MaThuongHieu` | Thương hiệu: Intel, AMD, ASUS, Kingston... |
| `SanPhams` | `Uid` | Tên, mã SP, `MaDanhMuc`, `MaThuongHieu`, giá gốc, giá bán, tồn kho, ảnh đại diện, `MoTa` (HTML từ CKEditor) |
| `HinhAnhSanPhams` | `Uid` | Bộ ảnh của từng sản phẩm |
| `ThongSoKyThuats` | `Uid` | Thông số dạng key/value gắn với `UidSanPham` |
| `DonDatHangs` | `Uid` | Đơn đặt hàng: thông tin khách, tổng tiền, hình thức thanh toán, `MaTinhTrang` |
| `ChiTietDonHangs` | `Uid` | Từng dòng sản phẩm trong đơn (lưu lại đơn giá tại thời điểm mua) |
| `TinhTrangDonHangs` | `MaTinhTrang` | Danh mục tình trạng đơn hàng |
| `TaiKhoans` | `Uid` | Tài khoản quản trị (mật khẩu đã hash) |
| `CaiDats` | `Uid` | Cấu hình dạng key/value (phí giao hàng, ngưỡng miễn phí, ngưỡng sắp hết hàng...) |

## Cài đặt và chạy

### Yêu cầu

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (bản Express hoặc LocalDB đều được)
- SQL Server Management Studio (hoặc Azure Data Studio) để chạy script SQL

### 1. Clone project

```bash
git clone <url-repo>
cd WebLinhKien/WebLinhKien
```

### 2. Cấu hình chuỗi kết nối

Ứng dụng đọc chuỗi kết nối tên `WebLinhKienCST`. Nên lưu bằng **User Secrets** để không đẩy mật khẩu lên GitHub:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:WebLinhKienCST" "Server=localhost;Database=WebLinhKien;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

### 3. Tạo database

Tạo database `WebLinhKien` rồi chạy lần lượt hai script trong thư mục `Database/`:

1. `schema.sql` – tạo các bảng;
2. `seed.sql` – thêm dữ liệu mẫu (danh mục, thương hiệu, một số sản phẩm, tài khoản admin).

Danh mục tình trạng đơn hàng (đơn mới đặt có mã `MOI`) nằm trong `seed.sql`:

```sql
INSERT INTO TinhTrangDonHangs (MaTinhTrang, TenTinhTrang, Ordering) VALUES
(N'MOI',            N'Mới đặt hàng',   1),
(N'DANG_XU_LY',     N'Đang xử lý',     2),
(N'DANG_GIAO_HANG', N'Đang giao hàng', 3),
(N'DA_NHAN',        N'Đã nhận',        4),
(N'HOAN_TRA',       N'Hoàn trả',       5),
(N'DOI_HANG',       N'Đổi hàng',       6);
```

### 4. Chạy ứng dụng

```bash
dotnet run
```

Mở địa chỉ **https** được in ra trong terminal. Cookie session đặt `SecurePolicy = Always` nên cần chạy bằng https. Trang quản trị nằm ở `/Admin`.

## Đặt hàng

Form đặt hàng gửi `POST /DatHang` dạng JSON, kèm header `RequestVerificationToken` lấy từ `@Html.AntiForgeryToken()` trong form. Đơn gồm nhiều sản phẩm lấy từ giỏ hàng.

**Request** (`DatHangRequest`):

```json
{
  "tenKhachHang": "Nguyễn Văn A",
  "sdt": "0912345678",
  "diaChi": "12 Lê Lợi, Phường Bến Thành, TP. Hồ Chí Minh",
  "maHinhThucThanhToan": "COD",
  "ghiChu": "Giao giờ hành chính",
  "items": [
    { "uidSanPham": 12, "soLuong": 1 },
    { "uidSanPham": 35, "soLuong": 2 }
  ]
}
```

`maHinhThucThanhToan` nhận một trong hai giá trị: `COD` hoặc `CHUYEN_KHOAN`.

**Response** (`Responses`):

```json
{
  "status": 200,
  "message": "Đặt hàng thành công! Mã đơn của bạn là DH261101103015123. ...",
  "error": null
}
```

Khi dữ liệu không hợp lệ, server trả `400` với `message` là lỗi đầu tiên và `error` là toàn bộ lỗi. Khi sản phẩm không đủ tồn kho, server trả `409` kèm tên sản phẩm bị thiếu.

**Quy tắc validate** (giống nhau ở client và server):

- mỗi sản phẩm có số lượng từ 1 đến 10 và không vượt quá tồn kho;
- họ tên từ 2 đến 100 ký tự;
- số điện thoại di động Việt Nam;
- địa chỉ từ 5 đến 200 ký tự;
- ghi chú tối đa 300 ký tự.

Phí giao hàng và ngưỡng miễn phí lưu trong bảng `CaiDats` (mặc định: 30.000đ, miễn phí cho đơn từ 500.000đ) và được server tính lại mỗi lần đặt hàng. Phần hiển thị ở client đọc cùng giá trị đó từ `wwwroot/js/data.js`, khi đổi cần sửa cả hai nơi.

## Thông báo thời gian thực (WebSocket)

Trang quản trị mở kết nối tới `wss://<host>/ws/admin` ngay sau khi đăng nhập. Endpoint này **chỉ chấp nhận người đã đăng nhập admin**.

Luồng hoạt động:

1. Khách đặt hàng thành công → `DatHangService` lưu đơn vào database.
2. Service gọi `WebSocketHandler.BroadcastAsync(...)` để gửi một message JSON tới tất cả kết nối admin đang mở.
3. Trang quản trị nhận message, hiện thông báo góc phải trên, tăng số đơn chờ xử lý và chèn đơn mới vào danh sách.

Ví dụ message:

```json
{
  "type": "NEW_ORDER",
  "maDon": "DH261101103015123",
  "tenKhachHang": "Nguyễn Văn A",
  "tongTien": 18500000
}
```

Các loại message dự kiến: `NEW_ORDER` (đơn mới), `LOW_STOCK` (sắp hết hàng). Client tự kết nối lại sau vài giây nếu bị mất kết nối.

## CKEditor trong trang quản trị

- Dùng cho ô **Mô tả sản phẩm**; nội dung được lưu dạng HTML trong cột `SanPhams.MoTa`.
- Ảnh chèn trong bài được upload qua endpoint riêng của Admin, lưu vào `wwwroot/uploads/` (giới hạn loại file và dung lượng).
- Khi hiển thị ở trang khách hàng cần **làm sạch HTML** (sanitize) trước khi render bằng `@Html.Raw` để tránh XSS.

## Ghi chú

- Dữ liệu cửa hàng (tên, địa chỉ, số điện thoại, banner...) nằm trong `wwwroot/js/data.js`. Các mục ghi `TODO` là dữ liệu tạm.
- Tailwind CSS đang dùng bản chạy trên trình duyệt để tiện học tập. Khi triển khai thật nên build bằng Tailwind CLI.
- Câu lệnh SQL luôn dùng **tham số hóa** của Dapper (`@TenThamSo`), không nối chuỗi, để tránh SQL injection.
- Không đưa chuỗi kết nối, mật khẩu admin thật hoặc thư mục `uploads/` lên GitHub (thêm vào `.gitignore`).

## Kế hoạch thực hiện (dự kiến)

| Giai đoạn | Nội dung |
|---|---|
| 1 | Thiết kế database, viết `schema.sql`, `seed.sql`, cấu hình `DapperContext` |
| 2 | Trang khách hàng: trang chủ, danh mục, chi tiết sản phẩm, tìm kiếm/lọc |
| 3 | Giỏ hàng và đặt hàng (validate, kiểm tra tồn kho, mã QR chuyển khoản) |
| 4 | Trang quản trị: đăng nhập, CRUD sản phẩm/danh mục/thương hiệu, tích hợp CKEditor |
| 5 | Quản lý đơn hàng và WebSocket thông báo đơn mới |
| 6 | Hoàn thiện: phân trang, cảnh báo sắp hết hàng, kiểm thử, triển khai |

## Chatbot tư vấn (thành viên D)

Khung chat nổi ở góc phải trang khách hàng, hỏi từng bước (bấm nút hoặc gõ tự do):

- **Ghép cấu hình PC**: chọn mục đích (văn phòng, gaming, đồ họa) và ngân sách (ví dụ "gaming 15 triệu"). Cấu hình gồm CPU, mainboard, RAM, VGA (trừ máy văn phòng), SSD, nguồn, case và tản nhiệt nếu còn ngân sách; tổng tiền không vượt ngân sách và đã kiểm tra tương thích (socket, loại RAM, kích thước case, độ dài card, công suất nguồn). Ngân sách quá thấp thì hiển thị cấu hình rẻ nhất hiện có và ghi rõ phần vượt. Dưới cấu hình có hai nút: "Thêm cả bộ vào giỏ" và "Đặt hàng ngay" (chuyển tới trang đặt hàng); giá và tồn kho được kiểm lại ở server khi thêm, món hết hàng thì bỏ qua và báo lại.
- **Tìm linh kiện**: chọn loại (CPU, RAM, VGA...), mức giá (dưới X, từ X đến Y, trên X) và hãng, ví dụ "ram kingston dưới 2 triệu".
- Hai chế độ trên tính bằng code (không nhờ AI tính tiền), nên đúng giá, đúng tồn kho và chạy được khi chưa có khóa API. Câu hỏi tự do khác nhờ mô hình AI, chỉ được chọn trong sản phẩm thật; mã sản phẩm AI trả về luôn được kiểm lại với database.

Việc ghép cấu hình đọc thông số theo quy ước trong bảng `ThongSoKyThuats` (Socket, TDP, Loại RAM, Form factor, Công suất, Chiều dài VGA...), thiếu thông số thì dùng tên sản phẩm để đoán.

Khóa API (chỉ cần cho câu hỏi tự do):

```
cd WebLinhKien
dotnet user-secrets set "Chatbot:ApiKey" "<khóa API>"
# tùy chọn: dotnet user-secrets set "Chatbot:Model" "claude-haiku-5-5"
```

Không đưa khóa API vào `appsettings*.json`. Giới hạn 20 lượt hỏi (kể cả bấm nút) mỗi phút cho mỗi IP.
