// Form sản phẩm trong trang quản trị: dòng thông số kỹ thuật, xem trước ảnh đại diện, CKEditor.
(function () {
    'use strict';

    var form = document.querySelector('[data-san-pham-form]');
    if (!form) return;

    // ---------- Thông số kỹ thuật: thêm, xóa dòng và đánh lại chỉ số ----------
    var specs = form.querySelector('[data-specs]');
    var tpl = form.querySelector('[data-spec-template]');

    // ASP.NET chỉ bind danh sách khi chỉ số liên tục 0,1,2... nên phải đánh lại sau mỗi lần thêm/xóa
    function reindex() {
        [].slice.call(specs.querySelectorAll('[data-spec-row]')).forEach(function (row, i) {
            var inputs = row.querySelectorAll('input');
            inputs[0].name = 'ThongSos[' + i + '].TenThongSo';
            inputs[1].name = 'ThongSos[' + i + '].GiaTri';
        });
    }

    form.addEventListener('click', function (e) {
        var add = e.target.closest('[data-spec-add]');
        if (add) {
            var html = tpl.innerHTML.replace(/__i__/g, specs.querySelectorAll('[data-spec-row]').length);
            specs.insertAdjacentHTML('beforeend', html);
            var rows = specs.querySelectorAll('[data-spec-row]');
            rows[rows.length - 1].querySelector('input').focus();
            return;
        }
        var del = e.target.closest('[data-spec-remove]');
        if (del) {
            del.closest('[data-spec-row]').remove();
            reindex();
        }
    });

    // ---------- Xem trước ảnh đại diện ----------
    var fileDaiDien = form.querySelector('input[type="file"][name="AnhDaiDienFile"]');
    var preview = form.querySelector('[data-preview="AnhDaiDienFile"]');
    if (fileDaiDien && preview) {
        fileDaiDien.addEventListener('change', function () {
            var f = fileDaiDien.files && fileDaiDien.files[0];
            if (!f || f.type.indexOf('image/') !== 0) return;
            if (preview.dataset.blobUrl) URL.revokeObjectURL(preview.dataset.blobUrl);
            preview.dataset.blobUrl = URL.createObjectURL(f);
            preview.src = preview.dataset.blobUrl;
            preview.hidden = false;
        });
    }

    // ---------- CKEditor 5: mô tả sản phẩm + upload ảnh ----------
    var area = form.querySelector('textarea[data-ckeditor]');
    if (area && window.ClassicEditor) {
        var token = form.querySelector('input[name="__RequestVerificationToken"]');

        window.ClassicEditor.create(area, {
            simpleUpload: {
                uploadUrl: area.dataset.uploadUrl,
                headers: { 'RequestVerificationToken': token ? token.value : '' }
            }
        }).then(function (editor) {
            // Đưa nội dung về textarea trước khi gửi form
            form.addEventListener('submit', function () { editor.updateSourceElement(); });
        }).catch(function (err) {
            // Không tải được CKEditor (mất mạng...) thì vẫn nhập được bằng ô văn bản thường
            console.error('Không khởi tạo được CKEditor:', err);
        });
    }
})();
