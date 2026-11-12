/**
 * TECHZONE – BUILD PC
 * Tự chọn linh kiện, kiểm tra tương thích, gợi ý theo ngân sách và thêm cả bộ vào giỏ.
 */
(function () {
    'use strict';

    const STEPS = ['CPU', 'MAINBOARD', 'RAM', 'VGA', 'SSD', 'PSU', 'CASE', 'TANNHIET'];
    const REQUIRED = ['CPU', 'MAINBOARD', 'RAM', 'VGA', 'SSD', 'PSU', 'CASE'];
    const STEP_LABELS = {
        CPU: 'CPU',
        MAINBOARD: 'Mainboard',
        RAM: 'RAM',
        VGA: 'VGA',
        SSD: 'SSD',
        PSU: 'Nguồn (PSU)',
        CASE: 'Vỏ case',
        TANNHIET: 'Tản nhiệt'
    };
    const STEP_ICONS = {
        CPU: 'fa-microchip',
        MAINBOARD: 'fa-server',
        RAM: 'fa-memory',
        VGA: 'fa-display',
        SSD: 'fa-hard-drive',
        PSU: 'fa-bolt',
        CASE: 'fa-desktop',
        TANNHIET: 'fa-fan',
        PHUKIEN: 'fa-plug'
    };
    const REQUEST_KEYS = {
        CPU: 'cpu',
        MAINBOARD: 'mainboard',
        RAM: 'ram',
        VGA: 'vga',
        SSD: 'ssd',
        PSU: 'psu',
        CASE: 'case',
        TANNHIET: 'tanNhiet'
    };

    let plansData = [];
    const SEARCH_STATE = {};
    const ALL_OPTIONS = {};
    const PLACEHOLDER_OPTIONS = {};

    function byId(id) {
        return document.getElementById(id);
    }

    function formatVnd(value) {
        return Number(value || 0).toLocaleString('vi-VN') + ' đ';
    }

    function numOrNull(value) {
        if (value === undefined || value === null || value === '') return null;
        const n = Number(value);
        return Number.isNaN(n) ? null : n;
    }

    function splitList(value) {
        return (value || '').split('|').map(x => x.trim()).filter(Boolean);
    }

    function productUrl(uid) {
        const holder = document.querySelector('[data-chi-tiet-url]');
        const base = holder ? holder.dataset.chiTietUrl : '/SanPham/ChiTiet/';
        return base + uid;
    }

    function normalizeText(value) {
        return (value || '').toString().toLowerCase()
            .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd');
    }

    function searchIndexFor(option) {
        return normalizeText([
            option.dataset.name,
            option.dataset.brand,
            option.dataset.socket,
            option.dataset.ram,
            option.dataset.form,
            (option.dataset.specs || '').replace(/\|/g, ' ')
        ].join(' '));
    }

    function rebuildSelectOptions(ma) {
        const select = getSelect(ma);
        const state = SEARCH_STATE[ma];
        if (!select || !state) return;

        const selectedValue = select.value;
        const all = ALL_OPTIONS[ma] || [];
        const matches = all.filter(option => {
            if (state.brands.length && state.brands.indexOf((option.dataset.brand || '').toLowerCase()) < 0) {
                return false;
            }
            if (!state.tokens.length) return true;
            const haystack = option.dataset.search || '';
            return state.tokens.every(token => haystack.indexOf(token) >= 0);
        });

        const selected = all.find(option => option.value === selectedValue);
        const matchCount = matches.length;
        if (selected && matches.indexOf(selected) < 0) matches.unshift(selected);

        select.innerHTML = '';
        if (PLACEHOLDER_OPTIONS[ma]) select.appendChild(PLACEHOLDER_OPTIONS[ma]);
        matches.forEach(option => select.appendChild(option));
        if (selectedValue && select.querySelector('option[value="' + selectedValue + '"]')) {
            select.value = selectedValue;
        }

        const note = document.querySelector('[data-search-note="' + ma + '"]');
        if (note) {
            const filtering = state.tokens.length > 0 || state.brands.length > 0;
            note.classList.toggle('d-none', !filtering);
            if (filtering) {
                note.textContent = matchCount
                    ? 'Hiện ' + matchCount + '/' + all.length + ' lựa chọn phù hợp.'
                    : 'Không tìm thấy linh kiện phù hợp, thử từ khóa khác.';
            }
        }

        const clearButton = document.querySelector('[data-step-search-clear="' + ma + '"]');
        if (clearButton) clearButton.classList.toggle('d-none', !state.term);
    }

    function buildSearchUi(ma) {
        SEARCH_STATE[ma] = { term: '', tokens: [], brands: [] };

        const input = document.querySelector('[data-step-search="' + ma + '"]');
        if (!input) return;

        input.addEventListener('input', function () {
            const state = SEARCH_STATE[ma];
            state.term = input.value;
            state.tokens = normalizeText(input.value).split(/\s+/).filter(Boolean);
            rebuildSelectOptions(ma);
            applyFilters();
        });

        const clearButton = document.querySelector('[data-step-search-clear="' + ma + '"]');
        if (clearButton) {
            clearButton.addEventListener('click', function () {
                input.value = '';
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.focus();
            });
        }

        const holder = document.querySelector('[data-brand-chips="' + ma + '"]');
        if (!holder) return;

        const counts = new Map();
        (ALL_OPTIONS[ma] || []).forEach(option => {
            const label = (option.dataset.brand || '').trim();
            if (!label) return;
            const key = label.toLowerCase();
            const entry = counts.get(key) || { label: label, count: 0 };
            entry.count++;
            counts.set(key, entry);
        });

        if (counts.size < 2) {
            holder.classList.add('d-none');
            return;
        }

        holder.innerHTML = Array.from(counts.entries())
            .sort((a, b) => a[1].label.localeCompare(b[1].label, 'vi'))
            .map(([key, entry]) => '<button type="button" class="tz-build-chip" data-chip-brand="' +
                escapeHtml(key) + '">' + escapeHtml(entry.label) + '<small>' + entry.count + '</small></button>')
            .join('');

        holder.addEventListener('click', function (event) {
            const chip = event.target.closest('[data-chip-brand]');
            if (!chip) return;
            const key = chip.dataset.chipBrand;
            const state = SEARCH_STATE[ma];
            const index = state.brands.indexOf(key);
            if (index >= 0) state.brands.splice(index, 1);
            else state.brands.push(key);
            chip.classList.toggle('is-active', state.brands.indexOf(key) >= 0);
            rebuildSelectOptions(ma);
            applyFilters();
        });
    }

    function clearSearchFilters() {
        Object.keys(SEARCH_STATE).forEach(ma => {
            SEARCH_STATE[ma] = { term: '', tokens: [], brands: [] };
            const input = document.querySelector('[data-step-search="' + ma + '"]');
            if (input) input.value = '';
            const clearButton = document.querySelector('[data-step-search-clear="' + ma + '"]');
            if (clearButton) clearButton.classList.add('d-none');
            document.querySelectorAll('[data-brand-chips="' + ma + '"] .tz-build-chip.is-active')
                .forEach(chip => chip.classList.remove('is-active'));
            rebuildSelectOptions(ma);
        });
    }

    function getSelect(ma) {
        return byId('tz-build-select-' + ma);
    }

    function getSelectedOption(ma) {
        const select = getSelect(ma);
        if (!select) return null;
        const option = select.selectedOptions[0];
        return option && option.value ? option : null;
    }

    function selectedInfo(ma) {
        const option = getSelectedOption(ma);
        if (!option) return null;
        return {
            uid: Number(option.value),
            name: option.dataset.name || '',
            price: numOrNull(option.dataset.price) || 0,
            originalPrice: numOrNull(option.dataset.originalPrice) || 0,
            brand: option.dataset.brand || '',
            image: option.dataset.image || '',
            stock: numOrNull(option.dataset.stock) || 0,
            warranty: numOrNull(option.dataset.warranty),
            socket: option.dataset.socket || '',
            ram: option.dataset.ram || '',
            form: option.dataset.form || '',
            tdp: numOrNull(option.dataset.tdp),
            tieuThu: numOrNull(option.dataset.tieuThu),
            watt: numOrNull(option.dataset.watt),
            vram: numOrNull(option.dataset.vram),
            length: numOrNull(option.dataset.length),
            maxLength: numOrNull(option.dataset.maxLength),
            forms: splitList(option.dataset.forms),
            sockets: splitList(option.dataset.sockets),
            specs: splitList(option.dataset.specs)
        };
    }

    function accessoryList() {
        return Array.from(document.querySelectorAll('.tz-build-accessory:checked')).map(input => ({
            uid: Number(input.value),
            kind: input.dataset.kind || '',
            name: input.dataset.name || '',
            price: numOrNull(input.dataset.price) || 0,
            stock: numOrNull(input.dataset.stock) || 0,
            connect: input.dataset.connect || '',
            resolution: input.dataset.resolution || '',
            hz: numOrNull(input.dataset.hz)
        }));
    }

    function normalizeSocket(value) {
        return (value || '').toUpperCase().replace(/\s+/g, '');
    }

    // Bỏ tiền tố loại hàng trong tên sản phẩm để câu thông báo không bị lặp từ.
    function shortName(name, prefixes) {
        const ten = (name || '').trim();
        for (const p of prefixes) {
            if (ten.length > p.length + 1 && ten.toLowerCase().startsWith((p + ' ').toLowerCase())) {
                return ten.slice(p.length + 1).trim();
            }
        }
        return ten;
    }

    function socketEquals(a, b) {
        const na = normalizeSocket(a);
        const nb = normalizeSocket(b);
        return !na || !nb || na === nb;
    }

    function socketMatchesAny(socket, candidates) {
        const target = normalizeSocket(socket);
        if (!target) return true;
        const list = (candidates || []).map(normalizeSocket).filter(Boolean);
        if (!list.length) return true;
        return list.some(x => x === target);
    }

    function ddrTokens(value) {
        return ((value || '').toUpperCase().match(/DDR\d+/g)) || [];
    }

    function ramCompatible(a, b) {
        const ta = ddrTokens(a);
        const tb = ddrTokens(b);
        if (!ta.length || !tb.length) return true;
        return ta.some(x => tb.includes(x));
    }

    function formRank(value) {
        const s = (value || '').toUpperCase();
        if (!s) return 0;
        if (s.indexOf('E-ATX') >= 0 || s.indexOf('EATX') >= 0) return 4;
        if (s.indexOf('MICRO') >= 0 || s.indexOf('M-ATX') >= 0 || s.indexOf('MATX') >= 0) return 2;
        if (s.indexOf('MINI') >= 0 || s.indexOf('ITX') >= 0) return 1;
        if (s.indexOf('ATX') >= 0) return 3;
        return 0;
    }

    function caseSupportsForm(caseInfo, mainboardForm) {
        const mbRank = formRank(mainboardForm);
        if (!mbRank) return true;
        const forms = caseInfo.forms.length ? caseInfo.forms : (caseInfo.form ? [caseInfo.form] : []);
        const ranks = forms.map(formRank).filter(Boolean);
        if (!ranks.length) return true;
        return Math.max.apply(null, ranks) >= mbRank;
    }

    function powerEstimate() {
        const cpu = selectedInfo('CPU');
        const vga = selectedInfo('VGA');
        const cpuTdp = cpu ? (cpu.tdp !== null ? cpu.tdp : 95) : 0;
        let vgaConsume = 0;
        if (vga) {
            if (vga.tieuThu !== null && vga.tieuThu > 0) {
                vgaConsume = vga.tieuThu;
            } else if (vga.watt !== null && vga.watt > 0) {
                vgaConsume = Math.max(75, Math.round(vga.watt * 0.4));
            } else {
                vgaConsume = 150;
            }
        }
        const estimate = cpuTdp + vgaConsume + 100;
        const vgaRequired = vga && vga.watt !== null ? vga.watt : 0;
        return {
            estimate: estimate,
            required: Math.max(estimate, vgaRequired)
        };
    }

    function evaluate() {
        const items = {};
        STEPS.forEach(ma => { items[ma] = selectedInfo(ma); });
        const accessories = accessoryList();
        const missing = REQUIRED.filter(ma => !items[ma]).map(ma => STEP_LABELS[ma]);
        const errors = [];
        const warnings = [];

        const cpu = items.CPU;
        const mainboard = items.MAINBOARD;
        const ram = items.RAM;
        const vga = items.VGA;
        const psu = items.PSU;
        const caseInfo = items.CASE;
        const cooler = items.TANNHIET;

        if (cpu && mainboard && !socketMatchesAny(cpu.socket, [mainboard.socket].concat(mainboard.sockets))) {
            errors.push('CPU "' + shortName(cpu.name, ['CPU']) + '" không lắp vừa với mainboard "' + shortName(mainboard.name, ['Mainboard', 'Bo mạch chủ']) + '" đã chọn. Hãy đổi một trong hai linh kiện này.');
        }
        if (mainboard && ram && !ramCompatible(mainboard.ram, ram.ram)) {
            errors.push('Thanh RAM "' + shortName(ram.name, ['RAM']) + '" không dùng được cho mainboard "' + shortName(mainboard.name, ['Mainboard', 'Bo mạch chủ']) + '" đã chọn. Hãy chọn thanh RAM khác.');
        }
        if (mainboard && caseInfo && !caseSupportsForm(caseInfo, mainboard.form)) {
            errors.push('Mainboard "' + shortName(mainboard.name, ['Mainboard', 'Bo mạch chủ']) + '" không vừa với vỏ case "' + shortName(caseInfo.name, ['Vỏ case', 'Case']) + '" đã chọn. Hãy chọn vỏ case lớn hơn.');
        }
        if (vga && caseInfo && vga.length !== null && caseInfo.maxLength !== null && vga.length > caseInfo.maxLength) {
            errors.push('Card màn hình "' + shortName(vga.name, ['Card màn hình', 'VGA']) + '" dài ' + vga.length + ' mm, nhưng vỏ case "' + shortName(caseInfo.name, ['Vỏ case', 'Case']) + '" chỉ chứa được card tối đa ' + caseInfo.maxLength + ' mm. Hãy chọn card ngắn hơn hoặc vỏ case lớn hơn.');
        }
        if (cooler && cpu && cooler.sockets.length && !socketMatchesAny(cpu.socket, cooler.sockets)) {
            errors.push('Tản nhiệt "' + shortName(cooler.name, ['Tản nhiệt']) + '" không lắp vừa với CPU "' + shortName(cpu.name, ['CPU']) + '" đã chọn. Hãy chọn tản nhiệt khác.');
        }

        const power = powerEstimate();
        if (psu) {
            if (psu.watt === null) {
                warnings.push('Nguồn "' + shortName(psu.name, ['Nguồn máy tính', 'Nguồn', 'PSU']) + '" chưa có thông tin công suất, hãy kiểm tra lại thông số sản phẩm.');
            } else if (psu.watt < power.required) {
                errors.push('Nguồn "' + shortName(psu.name, ['Nguồn máy tính', 'Nguồn', 'PSU']) + '" chỉ có ' + psu.watt + 'W, nhưng cả bộ máy cần tối thiểu ' + power.required + 'W. Hãy chọn nguồn từ ' + power.required + 'W trở lên.');
            } else if (psu.watt - power.required < 100) {
                warnings.push('Nguồn "' + shortName(psu.name, ['Nguồn máy tính', 'Nguồn', 'PSU']) + '" ' + psu.watt + 'W chỉ dư ' + (psu.watt - power.required) + 'W so với mức cần ' + power.required + 'W. Nên chọn nguồn công suất cao hơn.');
            }
        }
        if (mainboard && !mainboard.socket) {
            warnings.push('Mainboard "' + shortName(mainboard.name, ['Mainboard', 'Bo mạch chủ']) + '" chưa có đủ thông tin nên chưa kiểm tra được với CPU, hãy xem lại thông số sản phẩm.');
        }

        const itemTotal = STEPS.reduce((sum, ma) => sum + (items[ma] ? items[ma].price : 0), 0);
        const accessoryTotal = accessories.reduce((sum, x) => sum + x.price, 0);

        return {
            items: items,
            accessories: accessories,
            missing: missing,
            errors: errors,
            warnings: warnings,
            total: itemTotal + accessoryTotal,
            count: STEPS.filter(ma => items[ma]).length + accessories.length,
            power: power
        };
    }

    function renderSelectedPreview(ma, info) {
        const box = document.querySelector('[data-selected-preview="' + ma + '"]');
        if (!box) return;

        if (!info) {
            box.classList.add('d-none');
            box.innerHTML = '';
            return;
        }

        const icon = STEP_ICONS[ma] || 'fa-microchip';
        const image = info.image
            ? '<img src="' + escapeHtml(info.image) + '" alt="' + escapeHtml(info.name) + '" loading="lazy" onerror="tzImgError(this)" />'
              + '<span class="tz-thumb-fallback" style="display: none;"><i class="fa-solid ' + icon + '"></i></span>'
            : '<span class="tz-thumb-fallback" style="display: flex;"><i class="fa-solid ' + icon + '"></i></span>';

        const meta = [];
        if (info.brand) meta.push('<span>' + escapeHtml(info.brand) + '</span>');
        if (info.warranty) meta.push('<span>BH ' + info.warranty + ' tháng</span>');
        if (info.stock > 0) meta.push('<span>Còn ' + info.stock + '</span>');

        const url = productUrl(info.uid);
        box.innerHTML =
            '<a class="tz-build-selected-link" href="' + url + '" target="_blank" rel="noopener" title="Xem thông số chi tiết">' +
                '<span class="tz-build-selected-thumb">' + image + '</span>' +
                '<span class="tz-build-selected-body">' +
                    '<span class="tz-build-selected-name">' + escapeHtml(info.name) + '</span>' +
                    '<span class="tz-build-selected-meta">' + meta.join('<span>·</span>') + '</span>' +
                    '<span class="tz-build-selected-price">' + formatVnd(info.price) + '</span>' +
                    (info.specs.length ? '<span class="tz-build-selected-specs">' + escapeHtml(info.specs.slice(0, 2).join(' • ')) + '</span>' : '') +
                    '<span class="tz-build-selected-view"><i class="fa-solid fa-arrow-up-right-from-square"></i> Xem thông số chi tiết</span>' +
                '</span>' +
            '</a>';

        box.classList.remove('d-none');
    }

    function setSummaryRow(selector, html, modifiers) {
        const row = document.querySelector('.tz-build-summary-row[data-summary="' + selector + '"] .tz-build-summary-value');
        if (!row) return;
        row.classList.remove('is-empty', 'is-out');
        if (modifiers) modifiers.forEach(c => row.classList.add(c));
        row.innerHTML = html;
    }

    function updateSummary() {
        const state = evaluate();
        STEPS.forEach(ma => {
            const info = state.items[ma];
            if (!info) {
                setSummaryRow(ma, 'Chưa chọn', ['is-empty']);
            } else {
                setSummaryRow(ma,
                    '<a class="tz-build-summary-link" href="' + productUrl(info.uid) + '" target="_blank" rel="noopener" title="Xem thông số chi tiết">' + escapeHtml(info.name) + '</a>' +
                    '<span class="tz-build-summary-price">' + formatVnd(info.price) + '</span>',
                    info.stock <= 0 ? ['is-out'] : null);
            }
        });

        if (state.accessories.length) {
            const accessoryTotal = state.accessories.reduce((sum, x) => sum + x.price, 0);
            setSummaryRow('PHUKIEN',
                '<span>' + state.accessories.length + ' phụ kiện</span>' +
                '<span class="tz-build-summary-price">' + formatVnd(accessoryTotal) + '</span>');
        } else {
            setSummaryRow('PHUKIEN', 'Chưa chọn', ['is-empty']);
        }

        byId('tz-build-total').textContent = formatVnd(state.total);
        byId('tz-build-count').textContent = String(state.count);

        const psu = state.items.PSU;
        let powerText = '~' + state.power.estimate + ' W';
        if (psu && psu.watt !== null) {
            powerText += ' / nguồn ' + psu.watt + ' W';
        }
        byId('tz-build-power').textContent = powerText;

        byId('tz-build-add').disabled = state.missing.length > 0 || state.errors.length > 0;

        const accessoryCount = byId('tz-build-accessory-count');
        if (accessoryCount) {
            accessoryCount.textContent = state.accessories.length + ' đã chọn';
        }

        return state;
    }

    function updateClientHint(state) {
        const blocks = [];
        if (state.count > 0) {
            if (state.missing.length) {
                blocks.push({ type: 'warn', title: 'Còn thiếu linh kiện bắt buộc', items: [state.missing.join(', ')] });
            }
            if (state.errors.length) {
                blocks.push({ type: 'error', title: 'Cấu hình chưa tương thích', items: state.errors });
            }
        }
        renderMessages(blocks);
    }

    function updateAll() {
        STEPS.forEach(ma => renderSelectedPreview(ma, selectedInfo(ma)));
        updateClientHint(updateSummary());
    }

    function renderMessages(blocks) {
        const box = byId('tz-build-messages');
        if (!box) return;
        if (!blocks || !blocks.length) {
            box.innerHTML = '';
            return;
        }

        box.innerHTML = blocks.map(block => {
            const type = block.type === 'error' ? 'is-error' : block.type === 'ok' ? 'is-ok' : 'is-warn';
            const icon = block.type === 'error' ? 'fa-circle-xmark'
                : block.type === 'ok' ? 'fa-circle-check'
                : 'fa-triangle-exclamation';
            const items = (block.items || []).filter(Boolean);
            const list = items.length > 1
                ? '<ul>' + items.map(x => '<li>' + escapeHtml(x) + '</li>').join('') + '</ul>'
                : (items.length === 1 ? '<div>' + escapeHtml(items[0]) + '</div>' : '');

            return '<div class="tz-build-alert ' + type + '">' +
                '<div class="tz-build-alert-title"><i class="fa-solid ' + icon + ' me-1"></i>' + escapeHtml(block.title || '') + '</div>' +
                list +
                '</div>';
        }).join('');
    }

    function blocksFromServer(data) {
        const blocks = [];
        if (data.thieu && data.thieu.length) {
            blocks.push({ type: 'error', title: 'Thiếu linh kiện bắt buộc', items: data.thieu });
        }
        if (data.loi && data.loi.length) {
            blocks.push({ type: 'error', title: 'Không tương thích', items: data.loi });
        }
        if (data.kiemTraPhuKien && data.kiemTraPhuKien.length) {
            const errors = [];
            const warnings = [];
            data.kiemTraPhuKien.forEach(item => {
                const text = item.ten + ': ' + item.thongDiep;
                if (item.trangThai === 'error') errors.push(text);
                else if (item.trangThai === 'warn') warnings.push(text);
            });
            if (errors.length) blocks.push({ type: 'error', title: 'Phụ kiện không phù hợp', items: errors });
            if (warnings.length) blocks.push({ type: 'warn', title: 'Lưu ý về phụ kiện', items: warnings });
        }
        if (data.canhBao && data.canhBao.length) {
            blocks.push({ type: 'warn', title: 'Cảnh báo', items: data.canhBao });
        }
        if (!blocks.length) {
            blocks.push({ type: 'ok', title: 'Cấu hình hợp lệ', items: [data.message || 'Sẵn sàng thêm vào giỏ hàng.'] });
        }
        return blocks;
    }

    function applyFilters() {
        const cpu = selectedInfo('CPU');
        const mainboard = selectedInfo('MAINBOARD');
        const ram = selectedInfo('RAM');
        const vga = selectedInfo('VGA');
        const caseInfo = selectedInfo('CASE');
        const power = powerEstimate();

        filterSelect('CPU', option => {
            if (!mainboard) return true;
            if (socketMatchesAny(option.dataset.socket, [mainboard.socket].concat(mainboard.sockets))) return true;
            return 'dùng chân cắm ' + (option.dataset.socket || 'chưa rõ') + ', khác chân cắm ' + (mainboard.socket || 'chưa rõ') + ' của mainboard đã chọn';
        });

        filterSelect('MAINBOARD', option => {
            const reasons = [];
            if (cpu && !socketMatchesAny(cpu.socket, [option.dataset.socket].concat(splitList(option.dataset.sockets)))) {
                reasons.push('dùng chân cắm ' + (option.dataset.socket || 'chưa rõ') + ', khác chân cắm ' + (cpu.socket || 'chưa rõ') + ' của CPU đã chọn');
            }
            if (ram && !ramCompatible(ram.ram, option.dataset.ram)) {
                reasons.push('chỉ dùng RAM ' + (option.dataset.ram || 'chưa rõ') + ', trong khi RAM đã chọn là ' + (ram.ram || 'chưa rõ'));
            }
            if (caseInfo && !caseSupportsForm(caseInfo, option.dataset.form)) {
                reasons.push('kích thước ' + (option.dataset.form || 'chưa rõ') + ' không vừa vỏ case đã chọn');
            }
            return reasons.length ? reasons.join('; ') : true;
        });

        filterSelect('RAM', option => {
            if (!mainboard) return true;
            if (ramCompatible(mainboard.ram, option.dataset.ram)) return true;
            return 'là RAM ' + (option.dataset.ram || 'chưa rõ') + ', nhưng mainboard đã chọn chỉ dùng RAM ' + (mainboard.ram || 'chưa rõ');
        });

        filterSelect('VGA', option => {
            if (!caseInfo || caseInfo.maxLength === null) return true;
            const length = numOrNull(option.dataset.length);
            if (length === null || length <= caseInfo.maxLength) return true;
            return 'dài ' + length + ' mm, vượt quá mức tối đa ' + caseInfo.maxLength + ' mm của vỏ case đã chọn';
        });

        filterSelect('PSU', option => {
            const watt = numOrNull(option.dataset.watt);
            if (watt === null || watt >= power.required) return true;
            return 'có ' + watt + ' W, thấp hơn mức cần ' + power.required + ' W của cấu hình';
        });

        filterSelect('CASE', option => {
            const reasons = [];
            if (mainboard && !caseSupportsForm({
                form: option.dataset.form,
                forms: splitList(option.dataset.forms)
            }, mainboard.form)) {
                reasons.push('không lắp được mainboard kích thước ' + (mainboard.form || 'đã chọn'));
            }
            const maxLength = numOrNull(option.dataset.maxLength);
            if (vga && vga.length !== null && maxLength !== null && vga.length > maxLength) {
                reasons.push('chỉ chứa được card dài tối đa ' + maxLength + ' mm, trong khi card đã chọn dài ' + vga.length + ' mm');
            }
            return reasons.length ? reasons.join('; ') : true;
        });

        filterSelect('TANNHIET', option => {
            if (!cpu) return true;
            const sockets = splitList(option.dataset.sockets);
            if (!sockets.length || socketMatchesAny(cpu.socket, sockets)) return true;
            return 'không hỗ trợ chân cắm ' + (cpu.socket || 'của CPU') + ' của CPU đã chọn';
        });

        STEPS.forEach(renderPicker);
    }

    function filterSelect(ma, check) {
        const select = getSelect(ma);
        if (!select) return;

        const disabledReasons = [];
        Array.from(select.options).forEach(option => {
            if (!option.value) return;

            const baseLabel = option.dataset.baseLabel || option.textContent.trim();
            option.dataset.baseLabel = baseLabel;
            option.textContent = baseLabel;

            if (option.selected) {
                option.disabled = option.dataset.stockDisabled === '1';
                return;
            }
            if (option.dataset.stockDisabled === '1') {
                option.disabled = true;
                option.title = 'Sản phẩm đã hết hàng.';
                return;
            }

            const reason = check(option);
            option.disabled = reason !== true;
            if (option.disabled) {
                const text = typeof reason === 'string' ? reason : 'không tương thích';
                option.textContent = baseLabel + ' — Không tương thích: ' + text;
                option.title = text;
                disabledReasons.push(text);
            } else {
                option.title = '';
            }
        });

        updateFilterNote(ma, disabledReasons);
    }

    function renderPicker(ma) {
        const select = getSelect(ma);
        const holder = document.querySelector('[data-picker="' + ma + '"]');
        if (!select || !holder) return;

        const icon = STEP_ICONS[ma] || 'fa-microchip';
        const detailBase = (document.querySelector('.tz-container[data-chi-tiet-url]') || {}).dataset
            ? document.querySelector('.tz-container[data-chi-tiet-url]').dataset.chiTietUrl : '';
        const parts = [];

        if (select.value) {
            parts.push('<button type="button" class="tz-build-pick-clear" data-pick-clear="' + ma + '">' +
                '<i class="fa-solid fa-xmark me-1"></i>Bỏ chọn</button>');
        }

        Array.from(select.options).forEach(option => {
            if (!option.value) return;
            const uid = option.value;
            const disabled = option.disabled;
            const selected = option.selected;
            const reason = disabled ? (option.title || 'Không tương thích') : '';
            const name = option.dataset.name || option.textContent;
            const price = numOrNull(option.dataset.price);
            const stock = Number(option.dataset.stock || 0);
            const metaBits = [];
            if (option.dataset.brand) metaBits.push(escapeHtml(option.dataset.brand));
            const warranty = Number(option.dataset.warranty || 0);
            if (warranty > 0) metaBits.push('Bảo hành ' + warranty + ' tháng');
            metaBits.push(stock > 0 ? 'Còn ' + stock : 'Hết hàng');

            parts.push(
                '<button type="button" class="tz-build-pick' + (disabled ? ' is-off' : '') + (selected ? ' is-active' : '') + '"' +
                    ' data-pick-uid="' + uid + '" data-pick-step="' + ma + '"' +
                    ' title="' + escapeHtml(disabled ? reason : name) + '">' +
                    '<span class="tz-build-pick-thumb">' +
                        (option.dataset.image
                            ? '<img src="' + escapeHtml(option.dataset.image) + '" alt="" loading="lazy" onerror="tzImgError(this)">' +
                              '<span class="tz-thumb-fallback" style="display:none"><i class="fa-solid ' + icon + '"></i></span>'
                            : '<span class="tz-thumb-fallback" style="display:flex"><i class="fa-solid ' + icon + '"></i></span>') +
                    '</span>' +
                    '<span class="tz-build-pick-info">' +
                        '<span class="tz-build-pick-name">' + escapeHtml(name) + '</span>' +
                        '<span class="tz-build-pick-meta">' + metaBits.join(' · ') + '</span>' +
                        '<span class="tz-build-pick-price">' + (price !== null ? formatVnd(price) : '') + '</span>' +
                        (disabled ? '<span class="tz-build-pick-reason"><i class="fa-solid fa-circle-xmark me-1"></i>' + escapeHtml(reason) + '</span>' : '') +
                    '</span>' +
                    (selected ? '<span class="tz-build-pick-check"><i class="fa-solid fa-check"></i></span>' : '') +
                    (detailBase
                        ? '<a class="tz-build-pick-detail" href="' + escapeHtml(detailBase + uid) + '" target="_blank" rel="noopener" ' +
                          'title="Xem thông số chi tiết" onclick="event.stopPropagation()">' +
                          '<i class="fa-solid fa-arrow-up-right-from-square"></i></a>'
                        : '') +
                '</button>'
            );
        });

        if (!parts.length) {
            parts.push('<div class="tz-build-pick-empty text-muted small p-3 border rounded-2 w-100">Không có linh kiện nào.</div>');
        }

        holder.innerHTML = parts.join('');
    }

    function updateFilterNote(ma, reasons) {
        const note = document.querySelector('[data-filter-note="' + ma + '"]');
        if (!note) return;

        if (!reasons.length) {
            note.classList.add('d-none');
            note.textContent = '';
            return;
        }

        note.innerHTML = '<i class="fa-solid fa-circle-info me-1"></i>' +
            escapeHtml('Đã khóa ' + reasons.length + ' lựa chọn không phù hợp với linh kiện bạn đã chọn — mở danh sách bên dưới để xem lý do chi tiết từng món.');
        note.classList.remove('d-none');
    }

    function buildRequest() {
        const request = {};
        STEPS.forEach(ma => {
            const info = selectedInfo(ma);
            request[REQUEST_KEYS[ma]] = info ? info.uid : null;
        });
        request.phuKien = accessoryList().map(x => x.uid);
        return request;
    }

    function requestFromPlan(plan) {
        const request = {
            cpu: null, mainboard: null, ram: null, vga: null,
            ssd: null, psu: null, case: null, tanNhiet: null, phuKien: []
        };
        (plan.items || []).forEach(item => {
            const key = REQUEST_KEYS[item.loai];
            if (key) request[key] = item.uid;
        });
        return request;
    }

    async function postJson(url, body) {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify(body)
        });
        if (!response.ok) {
            throw new Error('Máy chủ trả về mã lỗi ' + response.status);
        }
        return response.json();
    }

    function setLoading(button, loading, loadingText) {
        if (!button) return;
        if (loading) {
            if (!button.dataset.originalHtml) {
                button.dataset.originalHtml = button.innerHTML;
            }
            button.disabled = true;
            button.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> ' + escapeHtml(loadingText || 'Đang xử lý...');
        } else {
            button.innerHTML = button.dataset.originalHtml || button.innerHTML;
            button.disabled = false;
            if (button.id === 'tz-build-add') {
                updateSummary();
            }
        }
    }

    function updateHeaderCartCount(total) {
        const badge = byId('tz-header-cart-count');
        if (badge && total !== undefined && total !== null) {
            badge.textContent = total;
        }
        const mobileCart = document.querySelector('.tz-mobile-drawer a[href*="DatHang"]');
        if (mobileCart && total !== undefined && total !== null) {
            mobileCart.innerHTML = '<i class="fa-solid fa-cart-shopping"></i> Giỏ hàng (' + total + ')';
        }
    }

    function handleCheckResult(data) {
        renderMessages(blocksFromServer(data));

        if (data.tongTienText) {
            byId('tz-build-total').textContent = data.tongTienText;
        }
        if (data.congSuatUocTinhW !== null && data.congSuatUocTinhW !== undefined) {
            let power = '~' + data.congSuatUocTinhW + ' W';
            if (data.congSuatPsuW) power += ' / nguồn ' + data.congSuatPsuW + ' W';
            byId('tz-build-power').textContent = power;
        }

        const addButton = byId('tz-build-add');
        addButton.disabled = !data.hopLe;
        if (!data.hopLe) {
            byId('tz-build-cart-link').classList.add('d-none');
        }
    }

    async function handleCheck() {
        const button = byId('tz-build-check');
        setLoading(button, true, 'Đang kiểm tra...');
        try {
            const data = await postJson('/BuildPc/KiemTra', buildRequest());
            handleCheckResult(data);
            if (data.hopLe) {
                TzToast.success('Tương thích', data.message || 'Cấu hình hợp lệ.');
            } else {
                TzToast.error('Chưa hợp lệ', data.message || 'Vui lòng kiểm tra các cảnh báo.');
            }
        } catch (error) {
            renderMessages([{ type: 'error', title: 'Lỗi kết nối', items: [error.message] }]);
            TzToast.error('Lỗi', 'Không thể kiểm tra cấu hình. Vui lòng thử lại.');
        } finally {
            setLoading(button, false);
        }
    }

    async function handleAdd(request, button, successCallback) {
        setLoading(button, true, 'Đang thêm...');
        try {
            const data = await postJson('/BuildPc/ThemCaBo', request);
            if (!data.success) {
                renderMessages(blocksFromServer(data));
                TzToast.error('Không thể thêm', data.message || 'Cấu hình chưa hợp lệ.');
                return;
            }

            updateHeaderCartCount(data.totalCount);
            byId('tz-build-cart-link').classList.remove('d-none');
            renderMessages([{ type: 'ok', title: 'Đã thêm vào giỏ hàng', items: [data.message] }]);
            if (data.canhBao && data.canhBao.length) {
                renderMessages([
                    { type: 'ok', title: 'Đã thêm vào giỏ hàng', items: [data.message] },
                    { type: 'warn', title: 'Lưu ý', items: data.canhBao }
                ]);
            }
            TzToast.success('Giỏ hàng', data.message || 'Đã thêm bộ PC vào giỏ hàng!');
            if (successCallback) successCallback(data);
        } catch (error) {
            renderMessages([{ type: 'error', title: 'Lỗi kết nối', items: [error.message] }]);
            TzToast.error('Lỗi', 'Không thể thêm vào giỏ hàng. Vui lòng thử lại.');
        } finally {
            setLoading(button, false);
        }
    }

    function resetSelections(clearMessages) {
        clearSearchFilters();
        STEPS.forEach(ma => {
            const select = getSelect(ma);
            if (select) select.value = '';
        });
        Array.from(document.querySelectorAll('.tz-build-accessory:checked')).forEach(input => {
            input.checked = false;
        });
        applyFilters();
        updateAll();
        byId('tz-build-cart-link').classList.add('d-none');
        if (clearMessages !== false) {
            renderMessages([]);
        }
    }

    function switchToManualTab() {
        const tab = byId('tz-build-manual-tab');
        if (!tab || !window.bootstrap || !window.bootstrap.Tab) return;
        window.bootstrap.Tab.getOrCreateInstance(tab).show();
    }

    function renderPlans(data) {
        const container = byId('tz-build-suggest-results');
        const empty = byId('tz-build-suggest-empty');
        const messageBox = byId('tz-build-suggest-message');

        plansData = data.data || [];

        if (!plansData.length) {
            container.innerHTML = '';
            empty.classList.remove('d-none');
            messageBox.innerHTML = '<div class="tz-build-alert is-warn">' +
                '<i class="fa-solid fa-triangle-exclamation me-1"></i>' +
                escapeHtml(data.message || 'Không tìm được cấu hình phù hợp với ngân sách này.') +
                '</div>';
            return;
        }

        empty.classList.add('d-none');
        messageBox.innerHTML = '<div class="tz-build-alert is-ok">' +
            '<i class="fa-solid fa-circle-check me-1"></i>' +
            escapeHtml(data.message || ('Đã tìm thấy ' + plansData.length + ' phương án.')) +
            '</div>';

        container.innerHTML = plansData.map((plan, index) => {
            const rows = (plan.items || []).map(item =>
                '<tr>' +
                    '<td class="tz-build-plan-cat"><i class="fa-solid ' + escapeHtml(item.icon) + ' me-1"></i>' + escapeHtml(item.tenLoai) + '</td>' +
                    '<td class="tz-build-plan-name">' + escapeHtml(item.ten) + '</td>' +
                    '<td class="tz-build-plan-price">' + escapeHtml(item.giaText) + '</td>' +
                '</tr>'
            ).join('');

            const warnings = (plan.canhBao && plan.canhBao.length)
                ? '<div class="tz-build-alert is-warn mb-2"><i class="fa-solid fa-triangle-exclamation me-1"></i>' +
                  escapeHtml(plan.canhBao.join(' ')) + '</div>'
                : '';

            return '<div class="tz-build-option-card" data-plan-index="' + index + '">' +
                '<div class="d-flex align-items-start justify-content-between gap-3 flex-wrap mb-2">' +
                    '<div>' +
                        '<div class="tz-build-option-title">' + escapeHtml(plan.ten) + '</div>' +
                        '<p class="tz-build-option-desc">' + escapeHtml(plan.moTa) + '</p>' +
                    '</div>' +
                    '<div class="text-end">' +
                        '<div class="tz-build-option-total">' + escapeHtml(plan.tongTienText) + '</div>' +
                        '<div class="tz-build-option-remain">Còn lại ' + escapeHtml(plan.conLaiText) + '</div>' +
                    '</div>' +
                '</div>' +
                warnings +
                '<div class="table-responsive">' +
                    '<table class="table table-sm tz-build-plan-table"><tbody>' + rows + '</tbody></table>' +
                '</div>' +
                '<div class="d-flex flex-wrap gap-2 mt-3">' +
                    '<button type="button" class="tz-btn tz-btn-outline tz-btn-sm" data-plan-apply="' + index + '">' +
                        '<i class="fa-solid fa-pen-to-square"></i> Áp dụng cấu hình</button>' +
                    '<button type="button" class="tz-btn tz-btn-accent tz-btn-sm" data-plan-add="' + index + '">' +
                        '<i class="fa-solid fa-cart-plus"></i> Thêm bộ này vào giỏ</button>' +
                '</div>' +
            '</div>';
        }).join('');
    }

    function applyPlan(index) {
        const plan = plansData[index];
        if (!plan) return;

        resetSelections(false);
        const skipped = [];

        (plan.items || []).forEach(item => {
            const select = getSelect(item.loai);
            if (!select) {
                skipped.push(item.ten);
                return;
            }
            const option = Array.from(select.options).find(opt => Number(opt.value) === Number(item.uid));
            if (!option) {
                skipped.push(item.ten);
                return;
            }
            option.disabled = false;
            select.value = String(item.uid);
        });

        applyFilters();
        updateAll();
        switchToManualTab();

        const summary = byId('tz-build-summary');
        if (summary && summary.scrollIntoView) {
            summary.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }

        if (skipped.length) {
            renderMessages([{ type: 'warn', title: 'Một số linh kiện không còn khả dụng', items: skipped }]);
            TzToast.info('Áp dụng cấu hình', 'Một số linh kiện đã hết hàng hoặc không còn phù hợp.');
        } else {
            renderMessages([{ type: 'ok', title: 'Đã áp dụng cấu hình', items: [plan.ten + ' – ' + plan.tongTienText] }]);
            TzToast.success('Áp dụng cấu hình', 'Đã điền ' + plan.ten + ' vào bảng chọn linh kiện.');
        }
    }

    async function suggest() {
        const input = byId('tz-build-budget-input');
        const button = byId('tz-build-suggest');
        const raw = (input.value || '').replace(/\D/g, '');
        const budget = raw ? Number(raw) : 0;

        if (!budget) {
            byId('tz-build-suggest-message').innerHTML = '<div class="tz-build-alert is-warn">' +
                '<i class="fa-solid fa-triangle-exclamation me-1"></i>Vui lòng nhập ngân sách trước khi gợi ý.</div>';
            return;
        }

        const purpose = document.querySelector('input[name="tz-build-purpose"]:checked');
        setLoading(button, true, 'Đang gợi ý...');
        try {
            const data = await postJson('/BuildPc/GoiY', {
                nganSach: budget,
                mucDich: purpose ? purpose.value : 'gaming'
            });
            if (!data.success) {
                plansData = [];
                byId('tz-build-suggest-results').innerHTML = '';
                byId('tz-build-suggest-empty').classList.remove('d-none');
                byId('tz-build-suggest-message').innerHTML = '<div class="tz-build-alert is-warn">' +
                    '<i class="fa-solid fa-triangle-exclamation me-1"></i>' +
                    escapeHtml(data.message || 'Không tìm được cấu hình phù hợp.') +
                    '</div>';
                return;
            }
            renderPlans(data);
            TzToast.success('Gợi ý cấu hình', 'Đã tìm thấy ' + (data.data ? data.data.length : 0) + ' phương án phù hợp.');
        } catch (error) {
            byId('tz-build-suggest-message').innerHTML = '<div class="tz-build-alert is-error">' +
                '<i class="fa-solid fa-circle-xmark me-1"></i>' + escapeHtml(error.message) + '</div>';
            TzToast.error('Lỗi', 'Không thể lấy gợi ý cấu hình. Vui lòng thử lại.');
        } finally {
            setLoading(button, false);
        }
    }

    function initBudgetInput() {
        const input = byId('tz-build-budget-input');
        if (!input) return;

        input.addEventListener('input', function () {
            const raw = this.value.replace(/\D/g, '');
            this.value = raw ? Number(raw).toLocaleString('vi-VN') : '';
        });

        Array.from(document.querySelectorAll('.tz-build-budget-preset')).forEach(button => {
            button.addEventListener('click', function () {
                const value = Number(this.dataset.budget || 0);
                input.value = value.toLocaleString('vi-VN');
            });
        });

        Array.from(document.querySelectorAll('input[name="tz-build-purpose"]')).forEach(radio => {
            radio.addEventListener('change', function () {
                Array.from(document.querySelectorAll('input[name="tz-build-purpose"]')).forEach(item => {
                    const label = item.closest('.tz-filter-radio');
                    if (label) label.classList.toggle('active', item.checked);
                });
            });
        });
    }

    function init() {
        const summary = byId('tz-build-summary');
        if (!summary) return;

        STEPS.forEach(ma => {
            const select = getSelect(ma);
            if (!select) return;
            PLACEHOLDER_OPTIONS[ma] = select.querySelector('option[value=""]');
            const options = Array.from(select.options).filter(option => option.value);
            options.forEach(option => {
                option.dataset.baseLabel = option.textContent.trim();
                if (option.disabled) option.dataset.stockDisabled = '1';
                option.dataset.search = searchIndexFor(option);
            });
            ALL_OPTIONS[ma] = options;

            select.addEventListener('change', function () {
                applyFilters();
                updateAll();
            });

            buildSearchUi(ma);
            renderPicker(ma);
        });

        Array.from(document.querySelectorAll('.tz-build-accessory')).forEach(input => {
            input.addEventListener('change', updateAll);
        });

        byId('tz-build-check').addEventListener('click', handleCheck);
        byId('tz-build-add').addEventListener('click', function () {
            handleAdd(buildRequest(), this);
        });
        byId('tz-build-reset').addEventListener('click', function () {
            resetSelections(true);
            TzToast.info('Build PC', 'Đã xóa toàn bộ linh kiện đã chọn.');
        });

        byId('tz-build-suggest').addEventListener('click', suggest);

        document.querySelectorAll('[data-picker]').forEach(holder => {
            holder.addEventListener('click', function (event) {
                if (event.target.closest('.tz-build-pick-detail')) return;

                const clearButton = event.target.closest('[data-pick-clear]');
                if (clearButton) {
                    const ma = clearButton.dataset.pickClear;
                    const select = getSelect(ma);
                    if (select) {
                        select.value = '';
                        select.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                    return;
                }

                const card = event.target.closest('.tz-build-pick');
                if (!card || card.classList.contains('is-off')) return;
                const select = getSelect(card.dataset.pickStep);
                if (!select) return;
                select.value = card.dataset.pickUid;
                select.dispatchEvent(new Event('change', { bubbles: true }));
            });
        });

        const results = byId('tz-build-suggest-results');
        results.addEventListener('click', function (event) {
            const applyButton = event.target.closest('[data-plan-apply]');
            if (applyButton) {
                applyPlan(Number(applyButton.dataset.planApply));
                return;
            }

            const addButton = event.target.closest('[data-plan-add]');
            if (addButton) {
                const plan = plansData[Number(addButton.dataset.planAdd)];
                if (plan) handleAdd(requestFromPlan(plan), addButton);
            }
        });

        initBudgetInput();
        applyFilters();
        updateAll();
    }

    document.addEventListener('DOMContentLoaded', init);
})();
