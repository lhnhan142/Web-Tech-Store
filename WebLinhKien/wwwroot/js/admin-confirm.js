// Nút có thuộc tính data-confirm="Nội dung hỏi" sẽ hỏi lại trước khi gửi form.
// Có SweetAlert2 (window.Swal) thì dùng hộp thoại đẹp, không có thì dùng confirm() của trình duyệt.
(function () {
    'use strict';

    document.addEventListener('submit', function (e) {
        var btn = e.submitter;
        var form = e.target;
        if (!btn || !btn.dataset.confirm) return;

        // Lần gửi lại sau khi người dùng đã đồng ý trong hộp thoại SweetAlert
        if (form.dataset.confirmed === '1') {
            form.dataset.confirmed = '';
            return;
        }

        var message = btn.dataset.confirm;

        if (window.Swal) {
            e.preventDefault();
            window.Swal.fire({
                title: message,               // title là chữ thường, không phải HTML nên an toàn
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Đồng ý',
                cancelButtonText: 'Hủy'
            }).then(function (result) {
                if (result.isConfirmed) {
                    form.dataset.confirmed = '1';
                    form.requestSubmit(btn);
                }
            });
        } else if (!window.confirm(message)) {
            e.preventDefault();
        }
    });
})();
