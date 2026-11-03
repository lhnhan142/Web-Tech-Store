// Kết nối WebSocket /ws/admin, hiện thông báo nổi + chuông (qua Admin.notify trong admin.js)
// và phát sự kiện để từng trang tự cập nhật (xem cuối file).
(function () {
    'use strict';

    var WS_PATH = '/ws/admin';
    var MAX_FAILED_ATTEMPTS = 10;   // hết phiên đăng nhập thì thôi không thử lại mãi

    var socket = null;
    var failedAttempts = 0;
    var closingPage = false;
    function handleMessage(msg) {
        if (!msg || !msg.type) return;

        // Admin.notify (admin.js) vừa hiện thông báo nổi vừa thêm vào chuông trên thanh điều hướng
        if (msg.type === 'NEW_ORDER') {
            window.Admin.notify({
                type: 'order',
                title: 'Có đơn hàng mới',
                text: msg.maDon + ' · ' + msg.tenKhachHang,
                lines: ['Mã đơn: ' + msg.maDon, 'Khách: ' + msg.tenKhachHang, 'Tổng tiền: ' + window.Admin.money(msg.tongTien)],
                href: '/Admin/DonHang?q=' + encodeURIComponent(msg.maDon)
            });
        } else if (msg.type === 'LOW_STOCK') {
            window.Admin.notify({
                type: 'stock',
                title: 'Sản phẩm sắp hết hàng',
                text: msg.tenSanPham + ' · còn ' + msg.tonKho,
                lines: [msg.tenSanPham, 'Còn lại: ' + msg.tonKho],
                href: '#'
            });
        }

        // NEW_ORDER -> "admin:new-order", LOW_STOCK -> "admin:low-stock"
        var eventName = 'admin:' + String(msg.type).toLowerCase().replace(/_/g, '-');
        window.dispatchEvent(new CustomEvent(eventName, { detail: msg }));
    }

    function connect() {
        var scheme = location.protocol === 'https:' ? 'wss://' : 'ws://';
        socket = new WebSocket(scheme + location.host + WS_PATH);

        socket.onopen = function () { failedAttempts = 0; };

        socket.onmessage = function (e) {
            try { handleMessage(JSON.parse(e.data)); } catch (err) { /* bỏ qua tin không hợp lệ */ }
        };

        socket.onclose = function () {
            if (closingPage || failedAttempts >= MAX_FAILED_ATTEMPTS) return;
            var delay = Math.min(30000, 1000 * Math.pow(2, failedAttempts));
            failedAttempts++;
            setTimeout(connect, delay);   // tự kết nối lại: 1s, 2s, 4s ... tối đa 30s
        };
    }

    window.addEventListener('beforeunload', function () {
        closingPage = true;
        if (socket) socket.close();
    });

    connect();
})();

/* Cách một trang tự cập nhật khi có đơn mới (ví dụ trang danh sách đơn):

   window.addEventListener('admin:new-order', function (e) {
       console.log(e.detail.maDon);   // rồi tải lại danh sách hoặc chèn thêm một dòng
   });
*/
