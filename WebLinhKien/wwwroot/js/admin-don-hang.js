// Trang danh sách đơn hàng: khi có đơn mới (sự kiện do admin-notifications.js phát ra từ WebSocket)
// thì tải lại phần danh sách, giữ nguyên bộ lọc và trang đang xem, rồi nháy sáng dòng đơn mới.
(function () {
    'use strict';

    var box = document.getElementById('danhsach-don');
    if (!box) return;

    var timer = null;

    function highlight(maDon) {
        if (!maDon) return;
        var rows = box.querySelectorAll('tr[data-ma-don]');
        for (var i = 0; i < rows.length; i++) {
            if (rows[i].getAttribute('data-ma-don') === maDon) {
                var row = rows[i];
                row.classList.add('bg-yellow-100');
                setTimeout(function () { row.classList.remove('bg-yellow-100'); }, 4000);
                return;
            }
        }
    }

    function refresh(maDon) {
        // Cùng địa chỉ đang xem (giữ ?tinhTrang=...&q=...&page=...); header này báo server chỉ trả phần danh sách
        fetch(location.pathname + location.search, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            credentials: 'same-origin'
        })
            .then(function (r) {
                // Hết phiên đăng nhập thì server chuyển sang trang đăng nhập: không được nhét trang đó vào danh sách
                if (!r.ok || r.redirected) throw new Error('refresh failed');
                return r.text();
            })
            .then(function (html) {
                box.innerHTML = html;
                highlight(maDon);
            })
            .catch(function () { /* bỏ qua: lần sau tải lại trang là thấy */ });
    }

    window.addEventListener('admin:new-order', function (e) {
        clearTimeout(timer);   // nhiều đơn dồn dập thì chỉ tải lại một lần
        timer = setTimeout(function () { refresh(e.detail && e.detail.maDon); }, 300);
    });
})();
