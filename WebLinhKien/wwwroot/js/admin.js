// WebLinhKien - admin.js
// Toàn bộ hành vi của giao diện quản trị: menu thả xuống, sidebar, thông báo nổi,
// hộp thoại xác nhận, lọc/phân trang bảng, form sản phẩm...
// Các phần ghi "DEMO" chỉ để xem thử bản tĩnh, khi ráp vào dự án thì bỏ hoặc thay bằng dữ liệu thật.
(function () {
    'use strict';

    var $ = function (sel, root) { return (root || document).querySelector(sel); };
    var $$ = function (sel, root) { return [].slice.call((root || document).querySelectorAll(sel)); };

    // localStorage có thể bị chặn (cửa sổ ẩn danh...) nên luôn bọc try/catch
    var store = {
        get: function (k) { try { return localStorage.getItem(k); } catch (e) { return null; } },
        set: function (k, v) { try { localStorage.setItem(k, v); } catch (e) { /* bỏ qua */ } }
    };

    function money(n) { return new Intl.NumberFormat('vi-VN').format(Math.round(Number(n) || 0)) + ' đ'; }
    function now() {
        var d = new Date(), p = function (x) { return String(x).padStart(2, '0'); };
        return p(d.getDate()) + '/' + p(d.getMonth() + 1) + '/' + d.getFullYear() + ' ' + p(d.getHours()) + ':' + p(d.getMinutes());
    }

    // ------------------------------------------------------------------
    // Thông báo nổi (toast). Dùng textContent để tránh XSS với dữ liệu người dùng nhập.
    // ------------------------------------------------------------------
    var toastBox = null;
    var TOAST_ICON = { info: 'fa-circle-info', ok: 'fa-circle-check', warn: 'fa-triangle-exclamation', err: 'fa-circle-exclamation' };

    function toast(opt) {
        if (!toastBox) {
            toastBox = document.createElement('div');
            toastBox.className = 'ad-toasts';
            toastBox.setAttribute('aria-live', 'polite');
            document.body.appendChild(toastBox);
        }
        var type = opt.type || 'info';
        var node = document.createElement('div');
        node.className = 'ad-toast is-' + type;
        node.setAttribute('role', 'status');

        var icon = document.createElement('i');
        icon.className = 'fa-solid ad-toast__icon ' + TOAST_ICON[type];
        var body = document.createElement('div');
        body.className = 'ad-toast__body';
        var title = document.createElement('strong');
        title.textContent = opt.title || '';
        body.appendChild(title);
        (opt.lines || (opt.text ? [opt.text] : [])).forEach(function (line) {
            var s = document.createElement('span');
            s.style.display = 'block';
            s.textContent = line;
            body.appendChild(s);
        });
        var x = document.createElement('button');
        x.type = 'button';
        x.className = 'ad-toast__x';
        x.setAttribute('aria-label', 'Đóng thông báo');
        x.innerHTML = '&times;';

        node.appendChild(icon); node.appendChild(body); node.appendChild(x);
        toastBox.appendChild(node);

        function close() {
            node.classList.add('is-leaving');
            setTimeout(function () { node.remove(); }, 220);
        }
        x.addEventListener('click', close);
        setTimeout(close, opt.timeout || 5000);
    }

    // ------------------------------------------------------------------
    // Hộp thoại xác nhận (thay cho window.confirm)
    // ------------------------------------------------------------------
    var modal = null, modalResolve = null, lastFocus = null;

    function buildModal() {
        modal = document.createElement('div');
        modal.className = 'ad-modal';
        modal.hidden = true;
        modal.innerHTML =
            '<div class="ad-modal__panel" role="dialog" aria-modal="true" aria-labelledby="ad-modal-title">' +
            '<div class="ad-modal__icon"><i class="fa-solid fa-triangle-exclamation"></i></div>' +
            '<h2 class="ad-modal__title" id="ad-modal-title"></h2>' +
            '<p class="ad-modal__text"></p>' +
            '<div class="ad-modal__foot">' +
            '<button type="button" class="ad-btn" data-modal-cancel>Hủy</button>' +
            '<button type="button" class="ad-btn" data-modal-ok></button>' +
            '</div></div>';
        document.body.appendChild(modal);
        modal.addEventListener('click', function (e) {
            if (e.target === modal || e.target.closest('[data-modal-cancel]')) closeModal(false);
            else if (e.target.closest('[data-modal-ok]')) closeModal(true);
        });
    }

    function closeModal(result) {
        modal.hidden = true;
        if (lastFocus) lastFocus.focus();
        if (modalResolve) { modalResolve(result); modalResolve = null; }
    }

    function confirmBox(opt) {
        if (!modal) buildModal();
        lastFocus = document.activeElement;
        var danger = opt.danger !== false;
        $('.ad-modal__title', modal).textContent = opt.title || 'Xác nhận';
        $('.ad-modal__text', modal).textContent = opt.text || '';
        var icon = $('.ad-modal__icon', modal);
        icon.classList.toggle('is-info', !danger);
        icon.firstChild.className = 'fa-solid ' + (danger ? 'fa-triangle-exclamation' : 'fa-circle-question');
        var ok = $('[data-modal-ok]', modal);
        ok.textContent = opt.okText || 'Đồng ý';
        ok.className = 'ad-btn ' + (danger ? 'ad-btn--danger-solid' : 'ad-btn--primary');
        modal.hidden = false;
        ok.focus();
        return new Promise(function (resolve) { modalResolve = resolve; });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            if (modal && !modal.hidden) closeModal(false);
            closeDropdowns();
            document.body.classList.remove('side-open');
        }
    });

    // Phần tử có data-confirm="..." sẽ hỏi lại trước khi chạy.
    //   data-confirm-title, data-confirm-ok, data-confirm-info (không đỏ)
    //   data-remove-row + data-toast: chỉ dùng cho bản xem thử (xóa dòng giả lập)
    // Khi ráp vào dự án: nút submit trong form sẽ được gửi thật sau khi người dùng đồng ý.
    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-confirm]');
        if (!trigger || trigger.__confirmed) { if (trigger) trigger.__confirmed = false; return; }
        e.preventDefault();
        e.stopImmediatePropagation();
        confirmBox({
            title: trigger.dataset.confirmTitle,
            text: trigger.dataset.confirm,
            okText: trigger.dataset.confirmOk,
            danger: trigger.dataset.confirmInfo === undefined
        }).then(function (yes) {
            if (!yes) return;
            if (trigger.dataset.removeRow !== undefined) {
                var row = trigger.closest('tr');
                var table = trigger.closest('[data-table]');
                row.classList.add('is-removing');
                setTimeout(function () {
                    row.remove();
                    if (table && table.__refresh) table.__refresh(true);
                }, 250);
                if (trigger.dataset.toast) toast({ type: 'ok', title: trigger.dataset.toast });
            } else if (trigger.dataset.flowGo) {
                trigger.dispatchEvent(new CustomEvent('flow:confirmed', { bubbles: true }));
            } else {
                trigger.__confirmed = true;
                trigger.click();
            }
        });
    }, true);

    // ------------------------------------------------------------------
    // Menu thả xuống (chuông, tìm kiếm, tài khoản)
    // ------------------------------------------------------------------
    function closeDropdowns(except) {
        $$('.ad-dd.is-open').forEach(function (dd) {
            if (dd === except) return;
            dd.classList.remove('is-open');
            var t = $('[data-dd-toggle]', dd);
            if (t) t.setAttribute('aria-expanded', 'false');
        });
    }

    document.addEventListener('click', function (e) {
        var toggle = e.target.closest('[data-dd-toggle]');
        if (toggle) {
            var dd = toggle.closest('.ad-dd');
            var open = !dd.classList.contains('is-open');
            closeDropdowns(dd);
            dd.classList.toggle('is-open', open);
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            if (open) { var f = $('input', dd); if (f) setTimeout(function () { f.focus(); }, 30); }
            return;
        }
        if (!e.target.closest('.ad-menu')) closeDropdowns();
    });

    // Phím tắt "/" để mở ô tìm kiếm nhanh
    document.addEventListener('keydown', function (e) {
        if (e.key !== '/' || /input|textarea|select/i.test(e.target.tagName)) return;
        var t = $('[data-search-toggle]');
        if (t) { e.preventDefault(); t.click(); }
    });

    // ------------------------------------------------------------------
    // Thông báo trong chuông. Hàm này cũng là chỗ để admin-notifications.js (WebSocket)
    // gọi khi có đơn mới / sắp hết hàng: Admin.notify({ type:'order', title, text, href })
    // ------------------------------------------------------------------
    function setBell(n) {
        var dot = $('[data-bell-count]');
        if (!dot) return;
        dot.textContent = n > 9 ? '9+' : String(n);
        dot.hidden = n <= 0;
        var empty = $('[data-notify-empty]');
        if (empty) empty.hidden = $$('.ad-notify__item').length > 0;
    }

    function unreadCount() { return $$('.ad-notify__item.is-unread').length; }

    function notify(opt) {
        var list = $('[data-notify-list]');
        if (list) {
            var li = document.createElement('li');
            var a = document.createElement('a');
            a.className = 'ad-notify__item is-unread';
            a.href = opt.href || '#';
            var ic = document.createElement('span');
            ic.className = 'ad-notify__icon ' + (opt.type === 'stock' ? 'is-stock' : 'is-order');
            ic.innerHTML = '<i class="fa-solid ' + (opt.type === 'stock' ? 'fa-box-open' : 'fa-receipt') + '"></i>';
            var tx = document.createElement('span');
            tx.className = 'ad-notify__text';
            var b = document.createElement('b'); b.textContent = opt.title;
            var s = document.createElement('small'); s.textContent = (opt.text ? opt.text + ' · ' : '') + 'vừa xong';
            tx.appendChild(b); tx.appendChild(s);
            a.appendChild(ic); a.appendChild(tx); li.appendChild(a);
            list.insertBefore(li, list.firstChild);
        }
        setBell(unreadCount());
        toast({ type: opt.type === 'stock' ? 'warn' : 'info', title: opt.title, lines: opt.lines || (opt.text ? [opt.text] : []) });
    }

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-read-all]')) {
            $$('.ad-notify__item.is-unread').forEach(function (n) { n.classList.remove('is-unread'); });
            setBell(0);
        }
    });

    // ------------------------------------------------------------------
    // Sidebar: gập nhóm, thu gọn, ngăn kéo trên điện thoại
    // ------------------------------------------------------------------
    function initSidebar() {
        if (store.get('ad.side.collapsed') === '1' && window.innerWidth >= 992) document.body.classList.add('side-collapsed');

        $$('.ad-side__group').forEach(function (g) {
            var key = 'ad.group.' + (g.dataset.group || '');
            if (g.dataset.group && store.get(key) === '0' && !$('.is-active', g)) g.classList.add('is-closed');
            var head = $('.ad-side__head', g);
            if (!head) return;
            head.setAttribute('aria-expanded', g.classList.contains('is-closed') ? 'false' : 'true');
            head.addEventListener('click', function () {
                var closed = g.classList.toggle('is-closed');
                head.setAttribute('aria-expanded', closed ? 'false' : 'true');
                if (g.dataset.group) store.set(key, closed ? '0' : '1');
            });
        });

        var collapse = $('[data-side-collapse]');
        if (collapse) collapse.addEventListener('click', function () {
            var on = document.body.classList.toggle('side-collapsed');
            store.set('ad.side.collapsed', on ? '1' : '0');
        });

        var burger = $('[data-side-open]');
        if (burger) burger.addEventListener('click', function () { document.body.classList.add('side-open'); });
        var back = $('.ad-backdrop');
        if (back) back.addEventListener('click', function () { document.body.classList.remove('side-open'); });
    }

    // ------------------------------------------------------------------
    // Bảng dữ liệu: tìm kiếm, lọc, phân trang, chọn tất cả, xuất CSV.
    // Ở bản tĩnh lọc bằng JavaScript; khi ráp vào dự án sẽ lọc ở server (query string) như hiện nay.
    // ------------------------------------------------------------------
    function initTable(root) {
        var tbody = $('tbody', root);
        var pageSize = Number(root.dataset.pageSize) || 10;
        var searchInput = $('[data-table-search]', root);
        var filters = $$('[data-filter]', root);
        var state = { page: 1 };

        function allRows() { return $$('tr[data-row]', tbody); }

        function matches(row) {
            var q = searchInput ? searchInput.value.trim().toLowerCase() : '';
            if (q && (row.dataset.search || row.textContent).toLowerCase().indexOf(q) === -1) return false;
            return filters.every(function (f) {
                return !f.value || row.dataset[f.dataset.filter] === f.value;
            });
        }

        function refresh(keepPage) {
            if (!keepPage) state.page = 1;
            var rows = allRows();
            var hit = rows.filter(matches);
            var pages = Math.max(1, Math.ceil(hit.length / pageSize));
            state.page = Math.min(state.page, pages);
            var from = (state.page - 1) * pageSize;

            rows.forEach(function (r) { r.hidden = true; });
            hit.slice(from, from + pageSize).forEach(function (r) { r.hidden = false; });

            $$('[data-count]', root).forEach(function (n) { n.textContent = hit.length; });
            var empty = $('[data-empty]', root);
            if (empty) empty.hidden = hit.length > 0;
            var wrap = $('.ad-tablewrap', root);
            if (wrap) wrap.hidden = hit.length === 0;

            var info = $('[data-pager-info]', root);
            var btns = $('[data-pager-btns]', root);
            var pager = $('.ad-pager', root);
            if (pager) pager.hidden = hit.length === 0;
            if (info) info.textContent = hit.length ? 'Hiển thị ' + (from + 1) + '–' + Math.min(from + pageSize, hit.length) + ' trên ' + hit.length : '';
            if (btns) {
                btns.innerHTML = '';
                var add = function (label, page, opts) {
                    var b = document.createElement('button');
                    b.type = 'button';
                    b.className = 'ad-pager__btn' + (opts && opts.active ? ' is-active' : '');
                    b.innerHTML = label;
                    if (opts && opts.label) b.setAttribute('aria-label', opts.label);
                    if (opts && opts.disabled) b.disabled = true;
                    b.addEventListener('click', function () { state.page = page; refresh(true); });
                    btns.appendChild(b);
                };
                add('<i class="fa-solid fa-chevron-left"></i>', state.page - 1, { disabled: state.page === 1, label: 'Trang trước' });
                for (var p = 1; p <= pages; p++) add(String(p), p, { active: p === state.page });
                add('<i class="fa-solid fa-chevron-right"></i>', state.page + 1, { disabled: state.page === pages, label: 'Trang sau' });
            }
            var all = $('[data-check-all]', root);
            if (all) all.checked = false;
        }
        root.__refresh = refresh;

        if (searchInput) searchInput.addEventListener('input', function () { refresh(); });
        filters.forEach(function (f) { f.addEventListener('change', function () { refresh(); syncSideLinks(); }); });

        var checkAll = $('[data-check-all]', root);
        if (checkAll) checkAll.addEventListener('change', function () {
            $$('tr[data-row]:not([hidden]) input[data-check]', root).forEach(function (c) { c.checked = checkAll.checked; });
        });

        var exp = $('[data-export]', root);
        if (exp) exp.addEventListener('click', function () {
            var heads = $$('thead th', root).filter(function (th) { return !th.hasAttribute('data-noexport'); });
            var idx = $$('thead th', root).map(function (th) { return !th.hasAttribute('data-noexport'); });
            var lines = [heads.map(function (th) { return csv(th.textContent); }).join(',')];
            allRows().filter(matches).forEach(function (r) {
                var cells = $$('td', r).filter(function (td, i) { return idx[i]; });
                lines.push(cells.map(function (td) { return csv(td.textContent); }).join(','));
            });
            var blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
            var a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = (root.dataset.exportName || 'du-lieu') + '.csv';
            document.body.appendChild(a); a.click(); a.remove();
            toast({ type: 'ok', title: 'Đã xuất ' + (lines.length - 1) + ' dòng ra file CSV' });
        });

        function csv(s) { return '"' + s.replace(/\s+/g, ' ').trim().replace(/"/g, '""') + '"'; }

        // Liên kết lọc nhanh ở sidebar (ví dụ: Mới đặt hàng, Sắp hết hàng) dùng phần # trên địa chỉ
        function applyHash() {
            var h = decodeURIComponent((location.hash || '').slice(1));
            var f = filters.filter(function (x) { return x.dataset.hashFilter !== undefined; })[0];
            if (f) f.value = Array.prototype.some.call(f.options, function (o) { return o.value === h; }) ? h : '';
            refresh();
            syncSideLinks();
        }
        function syncSideLinks() {
            var f = filters.filter(function (x) { return x.dataset.hashFilter !== undefined; })[0];
            if (!f) return;
            $$('.ad-side__link[data-hash]').forEach(function (a) {
                a.classList.toggle('is-active', (a.dataset.hash || '') === f.value);
            });
        }
        window.addEventListener('hashchange', applyHash);

        var q = new URLSearchParams(location.search).get('q');
        if (q && searchInput) searchInput.value = q;
        applyHash();
    }

    // ------------------------------------------------------------------
    // Chi tiết đơn hàng: luồng đổi tình trạng (giống TinhTrangDonHangFlow.cs).
    // DEMO: bản thật gửi form POST /Admin/DonHang/DoiTinhTrang/{id} và server kiểm tra lại.
    // ------------------------------------------------------------------
    var FLOW = {
        MOI: ['DANG_XU_LY'],
        DANG_XU_LY: ['DANG_GIAO_HANG', 'HOAN_TRA'],
        DANG_GIAO_HANG: ['DA_NHAN', 'HOAN_TRA'],
        DA_NHAN: ['HOAN_TRA', 'DOI_HANG'],
        DOI_HANG: ['DANG_GIAO_HANG', 'DA_NHAN'],
        HOAN_TRA: []
    };
    var NAMES = {
        MOI: 'Mới đặt hàng', DANG_XU_LY: 'Đang xử lý', DANG_GIAO_HANG: 'Đang giao hàng',
        DA_NHAN: 'Đã nhận', HOAN_TRA: 'Hoàn trả', DOI_HANG: 'Đổi hàng'
    };
    var STEP_INDEX = { MOI: 0, DANG_XU_LY: 1, DANG_GIAO_HANG: 2, DA_NHAN: 3, DOI_HANG: 4, HOAN_TRA: 4 };

    function initOrderFlow(root) {
        var status = root.dataset.status;
        var box = $('[data-flow-buttons]', root);
        var steps = $$('[data-step]', root);
        var timeline = $('[data-timeline]', root);

        function paint() {
            $$('[data-status-badge]').forEach(function (b) {
                b.className = 'ad-badge ad-badge--' + status.toLowerCase();
                b.textContent = NAMES[status];
            });
            var cur = STEP_INDEX[status];
            steps.forEach(function (li, i) {
                li.classList.remove('is-done', 'is-current', 'is-bad');
                if (i < cur) li.classList.add('is-done');
                if (i === cur) li.classList.add(status === 'HOAN_TRA' ? 'is-bad' : 'is-current');
                if (i === 4) {
                    li.hidden = cur !== 4;
                    $('.ad-steps__label', li).textContent = NAMES[status];
                    $('i', li).className = 'fa-solid ' + (status === 'HOAN_TRA' ? 'fa-rotate-left' : 'fa-arrows-rotate');
                }
            });
            box.innerHTML = '';
            var next = FLOW[status];
            if (!next.length) {
                var p = document.createElement('p');
                p.className = 'ad-hint';
                p.style.margin = 0;
                p.textContent = 'Đơn này đã ở tình trạng cuối, không thể đổi tiếp.';
                box.appendChild(p);
                return;
            }
            next.forEach(function (code) {
                var b = document.createElement('button');
                b.type = 'button';
                var bad = code === 'HOAN_TRA';
                b.className = 'ad-btn ad-btn--block ' + (bad ? 'ad-btn--danger' : 'ad-btn--primary');
                b.textContent = 'Chuyển sang: ' + NAMES[code];
                b.dataset.to = code;
                if (bad) {
                    b.dataset.confirm = 'Đơn sẽ chuyển sang Hoàn trả và không đổi tiếp được nữa.';
                    b.dataset.confirmTitle = 'Chuyển đơn sang Hoàn trả?';
                    b.dataset.confirmOk = 'Chuyển sang Hoàn trả';
                    b.dataset.flowGo = '1';
                }
                box.appendChild(b);
            });
        }

        function go(code) {
            status = code;
            if (timeline) {
                var li = document.createElement('li');
                li.innerHTML = '<b></b><small></small>';
                $('b', li).textContent = NAMES[code];
                $('small', li).textContent = now() + ' · bạn vừa đổi';
                timeline.appendChild(li);
            }
            paint();
            toast({ type: 'ok', title: 'Đã cập nhật tình trạng đơn hàng', text: 'Tình trạng mới: ' + NAMES[code] });
        }

        box.addEventListener('click', function (e) {
            var b = e.target.closest('button[data-to]');
            if (b && !b.dataset.flowGo) go(b.dataset.to);
        });
        box.addEventListener('flow:confirmed', function (e) {
            var b = e.target.closest('button[data-to]');
            if (b) go(b.dataset.to);
        });
        paint();
    }

    // ------------------------------------------------------------------
    // Form: ô chỉ nhận chữ số, đếm ký tự, xem trước ảnh, thông số kỹ thuật, kiểm tra bắt buộc
    // ------------------------------------------------------------------
    function initForms() {
        $$('input[data-digits="1"]').forEach(function (input) {
            input.addEventListener('input', function () {
                var d = input.value.replace(/\D/g, '');
                if (d !== input.value) input.value = d;
            });
        });
        $$('input[data-upper]').forEach(function (input) {
            input.addEventListener('input', function () { input.value = input.value.toUpperCase().replace(/[^A-Z0-9_-]/g, ''); });
        });

        $$('[data-counter]').forEach(function (ta) {
            var out = $('[data-counter-out="' + ta.dataset.counter + '"]');
            var upd = function () { if (out) out.textContent = ta.value.length; };
            ta.addEventListener('input', upd); upd();
        });

        // Xem trước giá bán và phần trăm giảm
        var gia = $('[name="GiaBan"]'), goc = $('[name="GiaGoc"]'), pv = $('[data-price-preview]');
        function price() {
            if (!pv || !gia) return;
            var b = Number(gia.value) || 0, g = goc ? Number(goc.value) || 0 : 0;
            var html = 'Khách thấy: <b>' + money(b) + '</b>';
            if (g > b && b > 0) html += ' <span class="ad-strike">' + money(g) + '</span> <span class="ad-badge ad-badge--ok">-' + Math.round((1 - b / g) * 100) + '%</span>';
            else if (g && g < b) html = '<span class="ad-down"><i class="fa-solid fa-triangle-exclamation"></i> Giá gốc đang thấp hơn giá bán.</span>';
            pv.innerHTML = html;
        }
        [gia, goc].forEach(function (i) { if (i) i.addEventListener('input', price); });
        price();

        // Ảnh đại diện
        var file = $('[data-image-input]');
        if (file) file.addEventListener('change', function () {
            var f = file.files && file.files[0];
            if (!f) return;
            if (!/^image\//.test(f.type)) { toast({ type: 'err', title: 'Chỉ nhận file ảnh (jpg, png, webp)' }); file.value = ''; return; }
            var r = new FileReader();
            r.onload = function () {
                var img = $('[data-image-preview]'), ph = $('[data-image-placeholder]');
                img.src = r.result; img.hidden = false; if (ph) ph.hidden = true;
            };
            r.readAsDataURL(f);
        });

        // Thông số kỹ thuật dạng tên - giá trị
        var specs = $('[data-specs]');
        if (specs) {
            var tpl = function () {
                var d = document.createElement('div');
                d.className = 'ad-spec';
                d.innerHTML = '<input class="ad-input" placeholder="Tên thông số (vd: Socket)" />' +
                    '<input class="ad-input" placeholder="Giá trị (vd: LGA1700)" />' +
                    '<button type="button" class="ad-iconlink is-danger" data-remove-spec aria-label="Xóa dòng này"><i class="fa-solid fa-xmark"></i></button>';
                return d;
            };
            var add = $('[data-add-spec]');
            if (add) add.addEventListener('click', function () {
                var row = tpl(); specs.appendChild(row); $('input', row).focus();
            });
            specs.addEventListener('click', function (e) {
                var rm = e.target.closest('[data-remove-spec]');
                if (!rm) return;
                if ($$('.ad-spec', specs).length > 1) rm.closest('.ad-spec').remove();
                else $$('input', rm.closest('.ad-spec')).forEach(function (i) { i.value = ''; });
            });
        }

        // Hiện / ẩn mật khẩu
        $$('[data-toggle-pass]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var input = $('input', btn.parentNode);
                var show = input.type === 'password';
                input.type = show ? 'text' : 'password';
                btn.innerHTML = '<i class="fa-regular ' + (show ? 'fa-eye-slash' : 'fa-eye') + '"></i>';
                btn.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
            });
        });

        // DEMO: kiểm tra các ô bắt buộc rồi báo "đã lưu". Bản thật để server kiểm tra như hiện nay.
        $$('form[data-demo-submit]').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                var bad = null;
                $$('[data-required]', form).forEach(function (input) {
                    var wrap = input.closest('.ad-field');
                    var empty = !String(input.value).trim();
                    wrap.classList.toggle('has-error', empty);
                    if (empty && !bad) bad = input;
                });
                if (bad) {
                    bad.scrollIntoView({ block: 'center', behavior: 'smooth' });
                    bad.focus({ preventScroll: true });
                    toast({ type: 'err', title: 'Chưa lưu được', text: 'Hãy điền các ô còn thiếu được đánh dấu đỏ.' });
                    return;
                }
                if (form.dataset.demoSubmit === 'login') { location.href = 'index.html'; return; }
                toast({ type: 'ok', title: form.dataset.demoSubmit || 'Đã lưu', text: 'Bản xem thử: dữ liệu chưa ghi vào database.' });
            });
            form.addEventListener('input', function (e) {
                var w = e.target.closest('.ad-field');
                if (w && String(e.target.value).trim()) w.classList.remove('has-error');
            });
        });
    }

    // ------------------------------------------------------------------
    // Trang báo cáo: đổi cách nhóm / chỉ số thì vẽ lại biểu đồ và bảng (dữ liệu mẫu)
    // ------------------------------------------------------------------
    function initReport(root) {
        var data = JSON.parse($('#report-data').textContent);
        var group = $('[data-report-group]', root), metric = $('[data-report-metric]', root);
        var chart = $('[data-chart]', root), body = $('[data-report-body]', root);
        var COLORS = ['#f59e0b', '#3b82f6', '#16a34a', '#8b5cf6', '#ef4444', '#14b8a6', '#ec4899', '#64748b', '#84cc16', '#0ea5e9'];
        var META = { revenue: ['Doanh thu', 'vnd'], orders: ['Số đơn', 'int'], qty: ['Số lượng bán', 'int'] };

        function draw() {
            var rows = data[group.value].slice().sort(function (a, b) { return b[metric.value] - a[metric.value]; });
            var total = rows.reduce(function (s, r) { return s + r.revenue; }, 0);
            AdminCharts.render(chart, {
                type: 'bar', labels: rows.map(function (r) { return r.label; }), values: rows.map(function (r) { return r[metric.value]; }),
                colors: COLORS, format: META[metric.value][1], ytitle: META[metric.value][0]
            });
            $('[data-report-title]', root).textContent = META[metric.value][0] + ' theo ' + (group.value === 'category' ? 'danh mục' : 'thương hiệu');
            body.innerHTML = '';
            rows.forEach(function (r) {
                var tr = document.createElement('tr');
                tr.setAttribute('data-row', '');
                var pct = total ? Math.round(r.revenue / total * 1000) / 10 : 0;
                tr.innerHTML = '<td></td><td class="is-num"></td><td class="is-num"></td><td class="is-num"></td><td class="is-num"></td>';
                var td = $$('td', tr);
                td[0].textContent = r.label; td[1].textContent = r.orders; td[2].textContent = r.qty;
                td[3].textContent = money(r.revenue); td[4].textContent = String(pct).replace('.', ',') + '%';
                body.appendChild(tr);
            });
            if (root.__refresh) root.__refresh();
        }
        group.addEventListener('change', draw);
        metric.addEventListener('change', draw);
        draw();
    }

    // ------------------------------------------------------------------
    // Khởi động
    // ------------------------------------------------------------------
    function init() {
        initSidebar();
        initForms();
        $$('[data-table]').forEach(initTable);
        $$('[data-order-flow]').forEach(initOrderFlow);
        $$('[data-report]').forEach(initReport);
        setBell(unreadCount());

        $$('input[data-switch-toast]').forEach(function (sw) {
            sw.addEventListener('change', function () {
                toast({ type: 'ok', title: (sw.checked ? 'Đã hiển thị: ' : 'Đã ẩn: ') + sw.dataset.switchToast });
            });
        });
        $$('[data-demo-toast]').forEach(function (b) {
            b.addEventListener('click', function (e) { e.preventDefault(); toast({ type: 'info', title: b.dataset.demoToast }); });
        });

        // DEMO: giả lập có đơn mới sau vài giây, giống khi WebSocket /ws/admin gửi NEW_ORDER.
        // Khi ráp vào dự án: xóa khối này, để admin-notifications.js gọi Admin.notify(...)
        if (document.body.hasAttribute('data-demo-notify')) {
            setTimeout(function () {
                notify({
                    type: 'order', title: 'Có đơn hàng mới', href: 'don-hang-chi-tiet.html',
                    lines: ['Mã đơn: DH261102134512907', 'Khách: Võ Thị Thu Hà', 'Tổng tiền: ' + money(7720000)],
                    text: 'DH261102134512907 · Võ Thị Thu Hà'
                });
            }, 6000);
        }
    }

    window.Admin = { toast: toast, confirm: confirmBox, notify: notify, money: money };

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();
