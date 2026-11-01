// Kết nối WebSocket /ws/admin, hiện thông báo góc phải trên (tự ẩn sau 5 giây)
// và phát sự kiện để từng trang tự cập nhật (xem cuối file).
(function () {
    'use strict';

    var WS_PATH = '/ws/admin';
    var MAX_FAILED_ATTEMPTS = 10;   // hết phiên đăng nhập thì thôi không thử lại mãi

    var socket = null;
    var failedAttempts = 0;
    var closingPage = false;
    var toastBox = null;

    function formatVnd(n) {
        return new Intl.NumberFormat('vi-VN').format(Number(n) || 0) + ' đ';
    }

    function ensureToastBox() {
        if (toastBox && document.body.contains(toastBox)) return toastBox;
        toastBox = document.createElement('div');
        toastBox.className = 'fixed top-4 right-4 z-50 flex flex-col gap-2';
        document.body.appendChild(toastBox);
        return toastBox;
    }

    // Dùng textContent (không dùng innerHTML) vì tên khách hàng là dữ liệu người dùng nhập, tránh XSS.
    function showToast(title, lines, warn) {
        var el = document.createElement('div');
        el.className = 'w-72 rounded-lg border-l-4 bg-white p-3 shadow-lg ' +
            (warn ? 'border-amber-500' : 'border-blue-600');

        var t = document.createElement('div');
        t.className = 'text-sm font-semibold text-slate-800';
        t.textContent = title;
        el.appendChild(t);

        lines.forEach(function (line) {
            var p = document.createElement('div');
            p.className = 'text-sm text-slate-600';
            p.textContent = line;
            el.appendChild(p);
        });

        ensureToastBox().appendChild(el);
        setTimeout(function () { el.remove(); }, 5000);
    }

    function handleMessage(msg) {
        if (!msg || !msg.type) return;

        if (msg.type === 'NEW_ORDER') {
            showToast('Có đơn hàng mới', [
                'Mã đơn: ' + msg.maDon,
                'Khách: ' + msg.tenKhachHang,
                'Tổng tiền: ' + formatVnd(msg.tongTien)
            ], false);
        } else if (msg.type === 'LOW_STOCK') {
            showToast('Sản phẩm sắp hết hàng', [
                msg.tenSanPham,
                'Còn lại: ' + msg.tonKho
            ], true);
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
