/* =====================================================================
   dat-hang.js - Xử lý trang đặt hàng
   1) CONFIG: các con số cấu hình (phí giao hàng, ngưỡng miễn phí...)
   2) DEMO_CART: giỏ hàng mẫu để xem thử giao diện
   3) Hiển thị giỏ hàng, tính tiền, kiểm tra dữ liệu, gửi đơn, hộp thoại thành công
   Chạy 2 chế độ: mở file .html tĩnh thì tự là chế độ xem thử (demo); trong dự án, trang Razor
   đưa dữ liệu thật vào window.DAT_HANG (xem Views/DatHang/Index.cshtml) nên tự chuyển sang chế độ thật.
   ===================================================================== */
(() => {
    'use strict';

    // ------------------------------------------------------------------
    // 1) CẤU HÌNH
    // ------------------------------------------------------------------
    // Dữ liệu do trang Razor đưa vào. Mở file .html tĩnh thì rỗng -> dùng giá trị mặc định (xem thử).
    const SERVER = window.DAT_HANG || {};

    const CONFIG = {
        demo: SERVER.demo ?? true,  // true: giả lập server; false: gửi thật tới endpoint
        endpoint: SERVER.endpoint ?? '/DatHang',   // POST JSON, kèm header RequestVerificationToken
        phiGiaoHang: SERVER.phiGiaoHang ?? 30000,          // trong dự án: lấy từ CaiDats
        nguongMienPhi: SERVER.nguongMienPhi ?? 500000,     // trong dự án: lấy từ CaiDats
        soLuongToiDa: SERVER.soLuongToiDa ?? 10,           // khớp WebConstants.SoLuongToiDa ở server
        toastMs: 5000,
        nganHang: {                 // TODO: thay bằng thông tin thật (hoặc lấy từ CaiDats)
            ten: 'Ngân hàng (điền tên)',
            soTaiKhoan: '0123456789',
            chuTaiKhoan: 'CONG TY WEBLINHKIEN'
        }
    };

    // ------------------------------------------------------------------
    // 2) GIỎ HÀNG MẪU (khi ghép: dữ liệu này do server đưa ra từ giỏ hàng trong session)
    // ------------------------------------------------------------------
    const DEMO_CART = [
        { uid: 13, ten: 'Chuột gaming Logitech G102 LIGHTSYNC', gia: 390000, soLuong: 1, tonKho: 50, icon: 'fa-computer-mouse' },
        { uid: 5,  ten: 'RAM Kingston Fury Beast 16GB DDR4 3200MHz', gia: 790000, soLuong: 1, tonKho: 4, icon: 'fa-memory' }
    ];

    // ------------------------------------------------------------------
    // Tiện ích
    // ------------------------------------------------------------------
    const $ = (selector, root = document) => root.querySelector(selector);
    const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
    const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
    const vndFormat = new Intl.NumberFormat('vi-VN');
    const formatVnd = (n) => vndFormat.format(n) + ' đ';

    const form = $('#dh-form');
    let cart = [];
    let submitting = false;
    let toastTimer = null;

    // ------------------------------------------------------------------
    // Giỏ hàng: hiển thị và chỉnh số lượng
    // ------------------------------------------------------------------
    function loadDemoCart() {
        cart = DEMO_CART.map((item) => ({ ...item }));
        renderCart();
    }

    // Dùng textContent (không dùng innerHTML) vì tên sản phẩm là dữ liệu từ database, tránh XSS.
    function buildItem(item) {
        const li = document.createElement('li');
        li.className = 'dh-item';
        li.dataset.uid = item.uid;

        const thumb = document.createElement('div');
        thumb.className = 'dh-item__thumb';
        // Khi ghép: thay bằng <img src="@sp.AnhDaiDien" alt="..."> nếu muốn dùng ảnh thật
        thumb.innerHTML = `<i class="fa-solid ${item.icon || 'fa-box'}"></i>`;

        const body = document.createElement('div');

        const name = document.createElement('p');
        name.className = 'dh-item__name';
        name.textContent = item.ten;

        const price = document.createElement('div');
        price.className = 'dh-item__price';
        price.textContent = formatVnd(item.gia);

        body.append(name, price);

        if (item.tonKho <= 5) {
            const stock = document.createElement('div');
            stock.className = 'dh-item__stock';
            stock.textContent = `Chỉ còn ${item.tonKho} sản phẩm`;
            body.appendChild(stock);
        }

        const max = Math.min(CONFIG.soLuongToiDa, item.tonKho);
        const row = document.createElement('div');
        row.className = 'dh-item__row';
        row.innerHTML = `
            <div class="dh-qty">
                <button type="button" data-action="dec" aria-label="Giảm số lượng" ${item.soLuong <= 1 ? 'disabled' : ''}><i class="fa-solid fa-minus"></i></button>
                <span aria-live="polite">${item.soLuong}</span>
                <button type="button" data-action="inc" aria-label="Tăng số lượng" ${item.soLuong >= max ? 'disabled' : ''}><i class="fa-solid fa-plus"></i></button>
            </div>
            <span class="dh-item__line">${formatVnd(item.gia * item.soLuong)}</span>
            <button type="button" class="dh-item__remove" data-action="remove" aria-label="Xóa khỏi đơn hàng"><i class="fa-regular fa-trash-can"></i></button>`;
        body.appendChild(row);

        li.append(thumb, body);
        return li;
    }

    function renderCart() {
        const list = $('#dh-items');
        list.replaceChildren(...cart.map(buildItem));

        $('#dh-empty').hidden = cart.length > 0;
        $('#dh-ship').hidden = cart.length === 0;
        $('#dh-count').textContent = cart.reduce((sum, i) => sum + i.soLuong, 0);
        updateTotals();
        updateSubmitState();
    }

    // Tính tiền. Đây chỉ là hiển thị; server luôn tính lại từ database.
    function calcTotals() {
        const subtotal = cart.reduce((sum, i) => sum + i.gia * i.soLuong, 0);
        const fee = subtotal === 0 || subtotal >= CONFIG.nguongMienPhi ? 0 : CONFIG.phiGiaoHang;
        return { subtotal, fee, total: subtotal + fee };
    }

    function updateTotals() {
        const { subtotal, fee, total } = calcTotals();
        $('#dh-subtotal').textContent = formatVnd(subtotal);
        $('#dh-fee').textContent = fee === 0 && subtotal > 0 ? 'Miễn phí' : formatVnd(fee);
        $('#dh-total').textContent = formatVnd(total);
        $('#dh-mobile-total').textContent = formatVnd(total);

        const ship = $('#dh-ship');
        const text = $('#dh-ship-text');
        const bar = $('#dh-ship-bar');
        if (subtotal >= CONFIG.nguongMienPhi) {
            ship.classList.add('is-free');
            text.innerHTML = '<i class="fa-solid fa-circle-check"></i> Bạn được miễn phí giao hàng.';
        } else {
            ship.classList.remove('is-free');
            text.textContent = `Mua thêm ${formatVnd(CONFIG.nguongMienPhi - subtotal)} để được miễn phí giao hàng.`;
        }
        bar.style.width = Math.min(100, (subtotal / CONFIG.nguongMienPhi) * 100) + '%';
    }

    function updateSubmitState() {
        $$('[data-submit]').forEach((btn) => { btn.disabled = cart.length === 0 || submitting; });
    }

    $('#dh-items').addEventListener('click', (e) => {
        const btn = e.target.closest('[data-action]');
        if (!btn) return;
        const uid = Number(btn.closest('.dh-item').dataset.uid);
        const item = cart.find((i) => i.uid === uid);
        if (!item) return;

        const max = Math.min(CONFIG.soLuongToiDa, item.tonKho);
        switch (btn.dataset.action) {
            case 'inc': item.soLuong = Math.min(max, item.soLuong + 1); break;
            case 'dec': item.soLuong = Math.max(1, item.soLuong - 1); break;
            case 'remove': cart = cart.filter((i) => i.uid !== uid); break;
        }
        renderCart();
    });

    $('#dh-reload-demo').addEventListener('click', loadDemoCart);

    // ------------------------------------------------------------------
    // Kiểm tra dữ liệu (quy tắc giống server: FluentValidation)
    // ------------------------------------------------------------------
    const RULES = {
        tenKhachHang: (v) => {
            const s = v.trim();
            return s.length < 2 || s.length > 100 ? 'Họ tên phải từ 2 đến 100 ký tự.' : '';
        },
        sdt: (v) => {
            return /^(0|84)[35789]\d{8}$/.test(v)
                ? '' : 'Số điện thoại di động không hợp lệ (ví dụ 0912345678).';
        },
        diaChi: (v) => {
            const s = v.trim();
            return s.length < 5 || s.length > 200 ? 'Địa chỉ phải từ 5 đến 200 ký tự.' : '';
        },
        ghiChu: (v) => (v.length > 300 ? 'Ghi chú tối đa 300 ký tự.' : '')
    };

    function setError(name, message) {
        const field = $(`[data-field="${name}"]`);
        const input = $(`#${name}`);
        $(`#err-${name}`).textContent = message;
        field.classList.toggle('has-error', Boolean(message));
        input.setAttribute('aria-invalid', message ? 'true' : 'false');
    }

    function validateField(name) {
        const message = RULES[name]($(`#${name}`).value);
        setError(name, message);
        return message === '';
    }

    // Trả về tên ô sai đầu tiên, hoặc null nếu hợp lệ hết
    function validateAll() {
        let firstInvalid = null;
        Object.keys(RULES).forEach((name) => {
            if (!validateField(name) && !firstInvalid) firstInvalid = name;
        });
        return firstInvalid;
    }

    // Ô số điện thoại chỉ nhận chữ số: ký tự khác (chữ, khoảng trắng, dấu) bị loại ngay, kể cả khi dán vào.
    // Phải đăng ký TRƯỚC vòng lặp bên dưới để giá trị được làm sạch rồi mới kiểm tra.
    $('#sdt').addEventListener('input', (e) => {
        const digits = e.target.value.replace(/\D/g, '');
        if (digits !== e.target.value) e.target.value = digits;
    });

    Object.keys(RULES).forEach((name) => {
        const input = $(`#${name}`);
        input.addEventListener('blur', () => { if (input.value !== '' || name !== 'ghiChu') validateField(name); });
        input.addEventListener('input', () => {
            // Đang hiện lỗi thì kiểm tra lại ngay khi gõ để lỗi biến mất sớm
            if ($(`[data-field="${name}"]`).classList.contains('has-error')) validateField(name);
        });
    });

    $('#ghiChu').addEventListener('input', (e) => { $('#dh-ghichu-count').textContent = e.target.value.length; });

    $$('input[name="maHinhThucThanhToan"]').forEach((radio) => {
        radio.addEventListener('change', () => {
            $('#dh-transfer-hint').hidden = radio.value !== 'CHUYEN_KHOAN' || !radio.checked;
        });
    });

    // ------------------------------------------------------------------
    // Gửi đơn
    // ------------------------------------------------------------------
    // Server trả lỗi theo từng ô dạng { "tenKhachHang": ["..."], "sdt": ["..."] } -> hiện ngay dưới đúng ô đó
    function applyServerErrors(errors) {
        if (!errors || typeof errors !== 'object') return;
        let first = null;
        Object.entries(errors).forEach(([key, messages]) => {
            if (!RULES[key]) return;
            setError(key, [].concat(messages)[0]);
            if (!first) first = key;
        });
        if (first) $(`#${first}`).focus();
    }

    // Cấu trúc khớp DatHangRequest trong README của dự án
    function collectPayload() {
        return {
            tenKhachHang: $('#tenKhachHang').value.trim(),
            sdt: $('#sdt').value,
            diaChi: $('#diaChi').value.trim(),
            maHinhThucThanhToan: $('input[name="maHinhThucThanhToan"]:checked').value,
            ghiChu: $('#ghiChu').value.trim(),
            items: cart.map((i) => ({ uidSanPham: i.uid, soLuong: i.soLuong }))
        };
    }

    // Giả lập server (chỉ chạy khi CONFIG.demo = true)
    function fakeServer() {
        const { total } = calcTotals();
        const d = new Date();
        const pad = (n, len = 2) => String(n).padStart(len, '0');
        const maDon = 'DH' + String(d.getFullYear()).slice(2) + pad(d.getMonth() + 1) + pad(d.getDate())
            + pad(d.getHours()) + pad(d.getMinutes()) + pad(d.getSeconds()) + pad(d.getMilliseconds(), 3);
        return {
            status: 200,
            message: `Đặt hàng thành công! Mã đơn của bạn là ${maDon}. Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất.`,
            error: null,
            data: { maDon, tongThanhToan: total }
        };
    }

    async function sendOrder(payload) {
        if (CONFIG.demo) {
            await sleep(1200);
            return fakeServer(payload);
        }

        const token = $('input[name="__RequestVerificationToken"]')?.value ?? '';
        const response = await fetch(CONFIG.endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify(payload)
        });
        const data = await response.json().catch(() => ({}));
        return { ...data, status: data.status ?? response.status };
    }

    function setLoading(loading) {
        submitting = loading;
        form.setAttribute('aria-busy', String(loading));
        $$('[data-submit]').forEach((btn) => {
            $('.dh-btn__label', btn).hidden = loading;
            $('.dh-spinner', btn).hidden = !loading;
        });
        updateSubmitState();
    }

    function showFormError(message) {
        $('#dh-form-error-text').textContent = message;
        $('#dh-form-error').hidden = false;
        $('#dh-form-error').scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (submitting || cart.length === 0) return;
        $('#dh-form-error').hidden = true;

        const firstInvalid = validateAll();
        if (firstInvalid) {
            $(`#${firstInvalid}`).focus();
            showToast('Vui lòng kiểm tra lại thông tin.', 'error');
            return;
        }

        const payload = collectPayload();
        setLoading(true);
        try {
            const result = await sendOrder(payload);
            if (result.status === 200) {
                showSuccess(result, payload);
            } else {
                applyServerErrors(result.error);
                showFormError(result.message || 'Đặt hàng không thành công. Vui lòng thử lại.');
            }
        } catch (err) {
            showFormError('Không kết nối được máy chủ. Vui lòng thử lại.');
        } finally {
            setLoading(false);
        }
    });

    // ------------------------------------------------------------------
    // Hộp thoại thành công
    // ------------------------------------------------------------------
    function infoRow(label, value) {
        const row = document.createElement('div');
        const dt = document.createElement('dt');
        const dd = document.createElement('dd');
        dt.textContent = label;
        dd.textContent = value;
        row.append(dt, dd);
        return row;
    }

    function bankRow(label, value, copyable) {
        const row = infoRow(label, value);
        if (copyable) {
            const dd = $('dd', row);
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'dh-copy';
            btn.dataset.copy = value;
            btn.setAttribute('aria-label', 'Sao chép ' + label);
            btn.innerHTML = '<i class="fa-regular fa-copy"></i>';
            dd.appendChild(btn);
        }
        return row;
    }

    function showSuccess(result, payload) {
        const maDon = result.data?.maDon ?? '';
        const tong = result.data?.tongThanhToan ?? calcTotals().total;
        const isTransfer = payload.maHinhThucThanhToan === 'CHUYEN_KHOAN';

        $('#dh-modal-message').textContent = result.message;

        const info = $('#dh-modal-info');
        info.replaceChildren();
        if (maDon) info.append(infoRow('Mã đơn hàng', maDon));
        info.append(
            infoRow('Người nhận', payload.tenKhachHang),
            infoRow('Số điện thoại', payload.sdt),
            infoRow('Tổng thanh toán', formatVnd(tong)),
            infoRow('Hình thức', isTransfer ? 'Chuyển khoản ngân hàng' : 'Thanh toán khi nhận hàng (COD)')
        );

        const transfer = $('#dh-transfer');
        transfer.hidden = !isTransfer;
        if (isTransfer) {
            const bank = $('#dh-bank');
            bank.replaceChildren(
                bankRow('Ngân hàng', CONFIG.nganHang.ten, false),
                bankRow('Số tài khoản', CONFIG.nganHang.soTaiKhoan, true),
                bankRow('Chủ tài khoản', CONFIG.nganHang.chuTaiKhoan, false),
                bankRow('Số tiền', String(tong), true),
                bankRow('Nội dung', maDon, true)
            );
            drawFakeQr($('#dh-qr'), maDon + tong);
        }

        cart = [];
        renderCart();
        form.reset();
        $('#dh-ghichu-count').textContent = '0';
        $('#dh-transfer-hint').hidden = true;

        $('#dh-modal').hidden = false;
        document.body.classList.add('dh-noscroll');
        $('#dh-modal-close').focus();
    }

    function closeModal() {
        $('#dh-modal').hidden = true;
        document.body.classList.remove('dh-noscroll');
    }

    $('#dh-modal-close').addEventListener('click', (e) => {
        e.preventDefault();
        closeModal();
        if (!CONFIG.demo) window.location.href = SERVER.trangChu ?? '/';   // chế độ thật: về trang chủ
    });
    document.addEventListener('keydown', (e) => { if (e.key === 'Escape' && !$('#dh-modal').hidden) closeModal(); });

    // Sao chép nội dung chuyển khoản / số tài khoản
    $('#dh-bank').addEventListener('click', async (e) => {
        const btn = e.target.closest('[data-copy]');
        if (!btn) return;
        try {
            await navigator.clipboard.writeText(btn.dataset.copy);
            showToast('Đã sao chép.', 'success');
        } catch (err) {
            showToast('Không sao chép được, hãy chọn và chép thủ công.', 'error');
        }
    });

    // ------------------------------------------------------------------
    // Mã QR MINH HỌA (chỉ để xem giao diện, KHÔNG quét được)
    // Khi ghép vào dự án, mã QR thật do server tạo (QRCoder trong Helpers/Utils.cs).
    // ------------------------------------------------------------------
    function drawFakeQr(canvas, seedText) {
        const N = 29, S = 8, Q = 2;                       // 29 ô, mỗi ô 8px, viền trắng 2 ô
        const size = (N + Q * 2) * S;
        canvas.width = canvas.height = size;
        const ctx = canvas.getContext('2d');
        if (!ctx) return;                                  // môi trường không hỗ trợ canvas

        let h = 2166136261;
        for (const ch of seedText) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619) >>> 0; }
        const rand = () => {                               // mulberry32
            h = (h + 0x6D2B79F5) >>> 0;
            let t = h;
            t = Math.imul(t ^ (t >>> 15), t | 1);
            t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
            return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
        };

        ctx.fillStyle = '#fff';
        ctx.fillRect(0, 0, size, size);
        ctx.fillStyle = '#0f172a';

        const inFinder = (x, y) => (x < 8 && y < 8) || (x >= N - 8 && y < 8) || (x < 8 && y >= N - 8);
        for (let y = 0; y < N; y++) {
            for (let x = 0; x < N; x++) {
                if (!inFinder(x, y) && rand() > 0.5) ctx.fillRect((x + Q) * S, (y + Q) * S, S, S);
            }
        }
        [[0, 0], [N - 7, 0], [0, N - 7]].forEach(([fx, fy]) => {
            ctx.fillStyle = '#0f172a';
            ctx.fillRect((fx + Q) * S, (fy + Q) * S, 7 * S, 7 * S);
            ctx.fillStyle = '#fff';
            ctx.fillRect((fx + 1 + Q) * S, (fy + 1 + Q) * S, 5 * S, 5 * S);
            ctx.fillStyle = '#0f172a';
            ctx.fillRect((fx + 2 + Q) * S, (fy + 2 + Q) * S, 3 * S, 3 * S);
        });
    }

    // ------------------------------------------------------------------
    // Thông báo nhỏ góc phải trên, tự ẩn sau 5 giây
    // ------------------------------------------------------------------
    function showToast(message, type) {
        const toast = $('#dh-toast');
        toast.textContent = message;
        toast.className = 'dh-toast' + (type ? ' is-' + type : '');
        toast.hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => { toast.hidden = true; }, CONFIG.toastMs);
    }

    // ------------------------------------------------------------------
    // Khởi động
    // ------------------------------------------------------------------
    if (Array.isArray(SERVER.cart)) {
        // Chế độ thật: giỏ hàng do server đưa vào, không có nút nạp giỏ mẫu
        cart = SERVER.cart.map((item) => ({ ...item }));
        $('#dh-reload-demo').hidden = true;
        renderCart();
    } else {
        loadDemoCart();   // chế độ xem thử
    }
})();
