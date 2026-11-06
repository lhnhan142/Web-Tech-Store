/**
 * TECHZONE – PC COMPONENTS STORE
 * Client JavaScript Library (client.js)
 * Tương tác UI: Header, Mega menu, Mobile drawer, Toast, Back-to-top, Image fallback,
 * Live search, Compare, Recently Viewed, Quick View Modal.
 */

const STORAGE_KEY_COMPARE = 'techzone_compare';
const STORAGE_KEY_RECENT = 'techzone_recently_viewed';

document.addEventListener('DOMContentLoaded', function () {
    initStickyHeader();
    initMobileDrawer();
    initBackToTop();
    initImageFallbacks();
    initLiveSearch();
    tzUpdateCompareUI();
});

/**
 * Global Utilities
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

/**
 * 1. Sticky Header & Shadow on Scroll
 */
function initStickyHeader() {
    const navbar = document.querySelector('.tz-navbar');
    if (!navbar) return;

    window.addEventListener('scroll', function () {
        if (window.scrollY > 40) {
            navbar.classList.add('tz-scrolled');
        } else {
            navbar.classList.remove('tz-scrolled');
        }
    }, { passive: true });
}

/**
 * 2. Mobile Drawer / Offcanvas Menu
 */
function initMobileDrawer() {
    const toggleBtn = document.querySelector('.tz-mobile-toggle');
    const drawer = document.querySelector('.tz-mobile-drawer');
    const overlay = document.querySelector('.tz-drawer-overlay');
    const closeBtn = document.querySelector('.tz-drawer-close');

    if (!toggleBtn || !drawer || !overlay) return;

    function openDrawer() {
        drawer.classList.add('open');
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeDrawer() {
        drawer.classList.remove('open');
        overlay.classList.remove('open');
        document.body.style.overflow = '';
    }

    toggleBtn.addEventListener('click', openDrawer);
    if (closeBtn) closeBtn.addEventListener('click', closeDrawer);
    overlay.addEventListener('click', closeDrawer);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && drawer.classList.contains('open')) {
            closeDrawer();
        }
    });
}

/**
 * 3. Back to Top Button
 */
function initBackToTop() {
    const btn = document.querySelector('.tz-back-to-top');
    if (!btn) return;

    window.addEventListener('scroll', function () {
        if (window.scrollY > 300) {
            btn.classList.add('show');
        } else {
            btn.classList.remove('show');
        }
    }, { passive: true });

    btn.addEventListener('click', function () {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

/**
 * 4. Image Fallback Handling (Tránh vỡ layout khi ảnh chưa có trên đĩa)
 */
function initImageFallbacks() {
    document.querySelectorAll('.tz-img-fallback').forEach(img => {
        img.addEventListener('error', function () {
            this.style.display = 'none';
            const fallbackBox = this.parentElement.querySelector('.tz-thumb-fallback');
            if (fallbackBox) {
                fallbackBox.style.display = 'flex';
            }
        });
    });
}

window.tzImgError = function (img) {
    if (!img) return;
    img.style.display = 'none';
    const parent = img.closest('.tz-card-thumb') || img.parentElement;
    if (parent) {
        const fallback = parent.querySelector('.tz-thumb-fallback');
        if (fallback) fallback.style.display = 'flex';
    }
};

/**
 * 5. Toast Notification System
 * TzToast.success('Thành công', 'Đã thêm vào giỏ hàng!');
 * TzToast.error('Lỗi', 'Không thể thực hiện hành động này.');
 * TzToast.info('Thông báo', 'Hệ thống đang cập nhật.');
 */
window.TzToast = {
    show: function (title, message, type = 'info', duration = 3500) {
        let container = document.getElementById('tz-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'tz-toast-container';
            container.className = 'tz-toast-container';
            document.body.appendChild(container);
        }

        const iconClass = type === 'success' ? 'fa-circle-check'
            : type === 'error' ? 'fa-circle-xmark'
            : 'fa-circle-info';

        const toast = document.createElement('div');
        toast.className = `tz-toast tz-toast-${type}`;
        toast.innerHTML = `
            <div class="tz-toast-icon">
                <i class="fa-solid ${iconClass}"></i>
            </div>
            <div class="tz-toast-body">
                <div class="tz-toast-title">${escapeHtml(title)}</div>
                <p class="tz-toast-message">${escapeHtml(message)}</p>
            </div>
            <button type="button" class="tz-toast-close" aria-label="Close">
                <i class="fa-solid fa-xmark"></i>
            </button>
        `;

        container.appendChild(toast);

        requestAnimationFrame(() => {
            toast.classList.add('show');
        });

        const closeBtn = toast.querySelector('.tz-toast-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => removeToast(toast));
        }

        const timer = setTimeout(() => {
            removeToast(toast);
        }, duration);

        function removeToast(el) {
            clearTimeout(timer);
            el.classList.remove('show');
            setTimeout(() => {
                if (el.parentElement) el.parentElement.removeChild(el);
            }, 300);
        }
    },

    success: function (title, message, duration) {
        this.show(title, message, 'success', duration);
    },

    error: function (title, message, duration) {
        this.show(title, message, 'error', duration);
    },

    info: function (title, message, duration) {
        this.show(title, message, 'info', duration);
    }
};

/**
 * 6. Live Search Gợi Ý Nhanh (Debounce 250ms)
 */
function initLiveSearch() {
    const searchInput = document.getElementById('tz-search-input');
    const suggestionsBox = document.getElementById('tz-search-suggestions');
    if (!searchInput || !suggestionsBox) return;

    let debounceTimer = null;

    searchInput.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        const query = this.value.trim();

        if (query.length === 0) {
            suggestionsBox.style.display = 'none';
            suggestionsBox.innerHTML = '';
            return;
        }

        debounceTimer = setTimeout(() => {
            fetch(`/SanPham/GoiY?q=${encodeURIComponent(query)}`)
                .then(res => res.json())
                .then(res => {
                    if (!res || !res.success || !res.data || res.data.length === 0) {
                        suggestionsBox.innerHTML = `
                            <div class="p-3 text-center text-muted small">
                                <i class="fa-solid fa-circle-exclamation me-1"></i>
                                Không tìm thấy linh kiện nào khớp với "<strong>${escapeHtml(query)}</strong>"
                            </div>
                        `;
                        suggestionsBox.style.display = 'block';
                        return;
                    }

                    let html = '';
                    res.data.forEach(item => {
                        const thumb = item.anh
                            ? `<img src="${item.anh}" alt="${escapeHtml(item.ten)}" onerror="this.style.display='none';this.nextElementSibling.style.display='flex';" />
                               <i class="fa-solid ${item.icon} text-muted" style="display:none;font-size:1.25rem;"></i>`
                            : `<i class="fa-solid ${item.icon} text-muted" style="font-size:1.25rem;"></i>`;

                        html += `
                            <a href="${item.url}" class="tz-suggestion-item">
                                <div class="tz-suggestion-thumb">
                                    ${thumb}
                                </div>
                                <div class="tz-suggestion-info">
                                    <div class="tz-suggestion-title">${escapeHtml(item.ten)}</div>
                                    <div class="tz-suggestion-meta">
                                        <span class="tz-suggestion-price">${item.giaFormatted}</span>
                                        <span class="tz-suggestion-sku">SKU: ${item.ma}</span>
                                    </div>
                                </div>
                            </a>
                        `;
                    });

                    html += `
                        <div class="tz-suggestion-footer" onclick="document.getElementById('tz-header-search-form').submit()">
                            <span>Xem tất cả kết quả cho "${escapeHtml(query)}" &rarr;</span>
                        </div>
                    `;

                    suggestionsBox.innerHTML = html;
                    suggestionsBox.style.display = 'block';
                })
                .catch(() => {
                    suggestionsBox.style.display = 'none';
                });
        }, 250);
    });

    // Ẩn gợi ý khi click ra ngoài
    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target) && !suggestionsBox.contains(e.target)) {
            suggestionsBox.style.display = 'none';
        }
    });

    // Hiện lại gợi ý nếu input vẫn còn từ khóa và được focus
    searchInput.addEventListener('focus', function () {
        if (this.value.trim().length > 0 && suggestionsBox.innerHTML.trim().length > 0) {
            suggestionsBox.style.display = 'block';
        }
    });
}

/**
 * 8. COMPARE PRODUCTS – SO SÁNH SẢN PHẨM (Tối đa 3 sản phẩm)
 */
function tzGetCompare() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY_COMPARE);
        return raw ? JSON.parse(raw) : [];
    } catch {
        return [];
    }
}

function tzSaveCompare(list) {
    try {
        localStorage.setItem(STORAGE_KEY_COMPARE, JSON.stringify(list));
    } catch (e) {
        console.error(e);
    }
    tzUpdateCompareUI();
}

window.tzToggleCompare = function (btn, item) {
    if (!item || !item.uid) return;
    let list = tzGetCompare();
    const exists = list.some(p => p.uid === item.uid);
    if (exists) {
        list = list.filter(p => p.uid !== item.uid);
        tzSaveCompare(list);
        TzToast.info('So sánh', `Đã xóa "${item.ten}" khỏi danh sách so sánh.`);
    } else {
        if (list.length >= 3) {
            TzToast.error('So sánh', 'Bạn chỉ có thể so sánh tối đa 3 sản phẩm cùng lúc!');
            return;
        }
        list.push(item);
        tzSaveCompare(list);
        TzToast.success('So sánh', `Đã thêm "${item.ten}" vào danh sách so sánh (${list.length}/3)!`);
    }
};

window.tzRemoveCompare = function (uid) {
    let list = tzGetCompare();
    const item = list.find(p => p.uid === uid);
    list = list.filter(p => p.uid !== uid);
    tzSaveCompare(list);
    if (item) {
        TzToast.info('So sánh', `Đã xóa "${item.ten}" khỏi so sánh.`);
    }

    const modalEl = document.getElementById('tzCompareModal');
    if (modalEl && modalEl.classList.contains('show')) {
        if (list.length === 0) {
            bootstrap.Modal.getInstance(modalEl)?.hide();
        } else {
            tzLoadCompareModalContent();
        }
    }
};

window.tzClearCompare = function () {
    tzSaveCompare([]);
    const modalEl = document.getElementById('tzCompareModal');
    if (modalEl && modalEl.classList.contains('show')) {
        bootstrap.Modal.getInstance(modalEl)?.hide();
    }
    TzToast.info('So sánh', 'Đã xóa toàn bộ danh sách so sánh.');
};

function tzUpdateCompareUI() {
    const list = tzGetCompare();
    const uids = new Set(list.map(p => p.uid));

    // Update Floating Compare Bar
    const bar = document.getElementById('tz-compare-bar');
    const countSpan = document.getElementById('tz-compare-count');
    const previewBox = document.getElementById('tz-compare-items-preview');

    if (bar && countSpan && previewBox) {
        countSpan.textContent = list.length;
        if (list.length > 0) {
            bar.style.display = 'block';
            let previewHtml = '';
            list.forEach(p => {
                const thumb = p.anh
                    ? `<img src="${p.anh}" alt="${escapeHtml(p.ten)}" onerror="this.style.display='none';" />`
                    : `<i class="fa-solid fa-microchip text-muted" style="font-size:0.9rem;"></i>`;
                previewHtml += `
                    <div class="tz-compare-item-chip" title="${escapeHtml(p.ten)}">
                        ${thumb}
                        <button type="button" class="tz-compare-chip-remove" onclick="tzRemoveCompare(${p.uid})" title="Bỏ chọn">
                            <i class="fa-solid fa-xmark"></i>
                        </button>
                    </div>
                `;
            });
            previewBox.innerHTML = previewHtml;
        } else {
            bar.style.display = 'none';
            previewBox.innerHTML = '';
        }
    }

    // Update all Compare buttons on current page
    document.querySelectorAll('[data-compare-uid]').forEach(btn => {
        const uid = parseInt(btn.getAttribute('data-compare-uid'));
        if (uids.has(uid)) {
            btn.classList.add('active', 'text-warning');
            btn.setAttribute('title', 'Đã thêm vào so sánh (Click để xóa)');
        } else {
            btn.classList.remove('active', 'text-warning');
            btn.setAttribute('title', 'So sánh cấu hình');
        }
    });
}

function tzLoadCompareModalContent() {
    const modalBody = document.getElementById('tz-compare-modal-body');
    if (!modalBody) return;

    const list = tzGetCompare();
    if (list.length === 0) {
        modalBody.innerHTML = `
            <div class="p-5 text-center text-muted">
                <i class="fa-solid fa-code-compare fa-3x mb-3" style="opacity: 0.3;"></i>
                <p>Chưa có sản phẩm nào trong danh sách so sánh.</p>
            </div>
        `;
        return;
    }

    modalBody.innerHTML = `
        <div class="p-5 text-center">
            <div class="spinner-border text-primary mb-2" role="status">
                <span class="visually-hidden">Đang tải...</span>
            </div>
            <div class="text-muted small">Đang đối chiếu thông số kỹ thuật...</div>
        </div>
    `;

    const ids = list.map(p => p.uid).join(',');
    fetch(`/SanPham/SoSanhModal?ids=${encodeURIComponent(ids)}`)
        .then(r => r.text())
        .then(html => {
            modalBody.innerHTML = html;
        })
        .catch(() => {
            modalBody.innerHTML = `<div class="p-4 text-center text-danger">Không thể tải dữ liệu so sánh. Vui lòng thử lại.</div>`;
        });
}

window.tzOpenCompareModal = function () {
    const list = tzGetCompare();
    if (list.length === 0) {
        TzToast.info('So sánh', 'Vui lòng chọn ít nhất 1 sản phẩm để so sánh.');
        return;
    }
    const modalEl = document.getElementById('tzCompareModal');
    if (modalEl && window.bootstrap) {
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
        tzLoadCompareModalContent();
    }
};

/**
 * 9. RECENTLY VIEWED – SẢN PHẨM ĐÃ XEM (Tối đa 8 sản phẩm)
 */
window.tzRecordRecentlyViewed = function (item) {
    if (!item || !item.uid) return;
    try {
        let list = [];
        const raw = localStorage.getItem(STORAGE_KEY_RECENT);
        if (raw) list = JSON.parse(raw);
        list = list.filter(p => p.uid !== item.uid);
        list.unshift(item);
        if (list.length > 8) {
            list = list.slice(0, 8);
        }
        localStorage.setItem(STORAGE_KEY_RECENT, JSON.stringify(list));
    } catch (e) {
        console.error(e);
    }
};

window.tzRenderRecentlyViewed = function (excludeUid) {
    const grid = document.getElementById('tz-recently-viewed-grid');
    const section = document.getElementById('tz-recently-viewed-section');
    if (!grid || !section) return;

    try {
        const raw = localStorage.getItem(STORAGE_KEY_RECENT);
        let list = raw ? JSON.parse(raw) : [];
        if (excludeUid) {
            list = list.filter(p => p.uid !== excludeUid);
        }

        if (list.length === 0) {
            section.style.display = 'none';
            return;
        }

        section.style.display = 'block';
        let html = '';
        list.forEach(p => {
            const iconClass = p.icon || 'fa-microchip';
            const thumb = p.anh
                ? `<img src="${p.anh}" alt="${escapeHtml(p.ten)}" onerror="this.style.display='none';this.nextElementSibling.style.display='flex';" />
                   <i class="fa-solid ${iconClass} text-muted" style="display:none;font-size:1.5rem;"></i>`
                : `<i class="fa-solid ${iconClass} text-muted" style="font-size:1.5rem;"></i>`;

            html += `
                <div class="col-6 col-md-4 col-lg-3">
                    <div class="tz-recent-card position-relative">
                        <a href="${p.url}" class="tz-recent-thumb" title="${escapeHtml(p.ten)}">
                            ${thumb}
                        </a>
                        <div class="tz-recent-title">
                            <a href="${p.url}" class="text-dark text-decoration-none" title="${escapeHtml(p.ten)}">
                                ${escapeHtml(p.ten)}
                            </a>
                        </div>
                        <div class="tz-recent-price">${p.gia || ''}</div>
                    </div>
                </div>
            `;
        });
        grid.innerHTML = html;
    } catch (e) {
        console.error(e);
        section.style.display = 'none';
    }
};

window.tzClearRecentlyViewed = function () {
    try {
        localStorage.removeItem(STORAGE_KEY_RECENT);
        const section = document.getElementById('tz-recently-viewed-section');
        if (section) section.style.display = 'none';
        TzToast.info('Đã xem', 'Đã xóa toàn bộ lịch sử sản phẩm đã xem.');
    } catch (e) {
        console.error(e);
    }
};

/**
 * 10. QUICK VIEW – XEM NHANH CHI TIẾT SẢN PHẨM
 */
window.tzOpenQuickView = function (uid) {
    const modalEl = document.getElementById('tzQuickViewModal');
    const modalBody = document.getElementById('tz-quickview-modal-body');
    if (!modalEl || !modalBody) return;

    modalBody.innerHTML = `
        <div class="p-5 text-center">
            <div class="spinner-border text-primary mb-3" role="status">
                <span class="visually-hidden">Đang tải...</span>
            </div>
            <div class="text-muted small">Đang tải chi tiết linh kiện...</div>
        </div>
    `;

    if (window.bootstrap) {
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    }

    fetch(`/SanPham/QuickViewModal/${uid}`)
        .then(r => r.text())
        .then(html => {
            modalBody.innerHTML = html;
        })
        .catch(() => {
            modalBody.innerHTML = `<div class="p-4 text-center text-danger">Không thể tải thông tin sản phẩm. Vui lòng thử lại.</div>`;
        });
};

window.tzQuickViewAddToCart = function (uid) {
    const token = getAntiForgeryToken();
    const formData = new FormData();
    formData.append('uid', uid);
    formData.append('soLuong', 1);
    formData.append('muaNgay', false);
    if (token) formData.append('__RequestVerificationToken', token);

    fetch('/SanPham/ThemVaoGio', {
        method: 'POST',
        body: formData
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            const badge = document.getElementById('tz-header-cart-count');
            if (badge && data.totalCount !== undefined) {
                badge.textContent = data.totalCount;
            }

            const modalEl = document.getElementById('tzQuickViewModal');
            if (modalEl && window.bootstrap) {
                bootstrap.Modal.getInstance(modalEl)?.hide();
            }

            TzToast.success('Giỏ hàng', data.message || 'Đã thêm sản phẩm vào giỏ hàng thành công!');
        } else {
            TzToast.error('Không thành công', data.message || 'Không thể thêm vào giỏ hàng.');
        }
    })
    .catch(() => {
        TzToast.error('Lỗi', 'Không thể kết nối đến máy chủ.');
    });
};
