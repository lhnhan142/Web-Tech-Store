// Biểu đồ cột và biểu đồ tròn bằng SVG thuần, không cần thư viện ngoài.
// Cách dùng trong HTML:
//   <div class="ad-chart" data-chart="bar"
//        data-labels='["CPU","RAM"]' data-values='[12000000,8000000]'
//        data-colors='["#3b82f6","#14b8a6"]' data-format="vnd" data-ytitle="Doanh thu"></div>
//   <div class="ad-chart" data-chart="donut" data-labels='[...]' data-values='[...]' data-colors='[...]'></div>
// Muốn vẽ lại bằng code (ví dụ đổi bộ lọc): AdminCharts.render(element, { labels, values, colors, format, ytitle })
(function () {
    'use strict';

    var NS = 'http://www.w3.org/2000/svg';
    var PALETTE = ['#3b82f6', '#14b8a6', '#f59e0b', '#8b5cf6', '#ef4444', '#64748b', '#ec4899', '#84cc16'];

    function el(name, attrs, text) {
        var node = document.createElementNS(NS, name);
        for (var k in attrs) node.setAttribute(k, attrs[k]);
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function full(v, format) {
        var n = new Intl.NumberFormat('vi-VN').format(Math.round(v));
        return format === 'vnd' ? n + ' đ' : n;
    }

    // Số gọn cho trục và nhãn: 1,2 tr / 500k
    function short(v, format) {
        if (format !== 'vnd') return new Intl.NumberFormat('vi-VN').format(v);
        if (v >= 1e9) return trim(v / 1e9) + ' tỷ';
        if (v >= 1e6) return trim(v / 1e6) + ' tr';
        if (v >= 1e3) return trim(v / 1e3) + 'k';
        return String(v);
    }
    function trim(n) { return String(Math.round(n * 10) / 10).replace('.', ','); }

    // Tìm bước chia "đẹp" cho trục tung (1, 2, 2.5, 5 x 10^n)
    function niceScale(max) {
        if (max <= 0) return { max: 1, step: 1 };
        var rough = max / 4;
        var pow = Math.pow(10, Math.floor(Math.log10(rough)));
        var f = rough / pow;
        var step = (f <= 1 ? 1 : f <= 2 ? 2 : f <= 2.5 ? 2.5 : f <= 5 ? 5 : 10) * pow;
        return { max: Math.ceil(max / step) * step, step: step };
    }

    function tip(container) {
        var t = container.querySelector('.ad-tip');
        if (!t) {
            t = document.createElement('div');
            t.className = 'ad-tip';
            container.appendChild(t);
        }
        return t;
    }

    function bindTip(container, node, html) {
        var t = tip(container);
        node.addEventListener('mouseenter', function () {
            var c = container.getBoundingClientRect();
            var r = node.getBoundingClientRect();
            t.textContent = html;
            t.style.left = (r.left - c.left + r.width / 2) + 'px';
            t.style.top = (r.top - c.top - 6) + 'px';
            t.classList.add('is-on');
        });
        node.addEventListener('mouseleave', function () { t.classList.remove('is-on'); });
    }

    function bar(container, cfg) {
        var labels = cfg.labels, values = cfg.values;
        var colors = cfg.colors && cfg.colors.length ? cfg.colors : PALETTE;
        var W = Math.max(container.clientWidth, 280);
        var H = cfg.height || 260;
        var m = { top: 24, right: 12, bottom: 44, left: cfg.ytitle ? 62 : 52 };
        var iw = W - m.left - m.right, ih = H - m.top - m.bottom;
        var sc = niceScale(Math.max.apply(null, values));

        var svg = el('svg', { viewBox: '0 0 ' + W + ' ' + H, role: 'img', 'aria-label': cfg.ytitle || 'Biểu đồ cột' });

        for (var v = 0; v <= sc.max + 1e-9; v += sc.step) {
            var y = m.top + ih - (v / sc.max) * ih;
            svg.appendChild(el('line', { x1: m.left, x2: W - m.right, y1: y, y2: y, stroke: v === 0 ? '#9aa5ba' : '#e6eaf2', 'stroke-width': 1 }));
            svg.appendChild(el('text', { x: m.left - 8, y: y + 4, 'text-anchor': 'end' }, short(v, cfg.format)));
        }
        if (cfg.ytitle) {
            svg.appendChild(el('text', { transform: 'translate(13,' + (m.top + ih / 2) + ') rotate(-90)', 'text-anchor': 'middle' }, cfg.ytitle));
        }

        var band = iw / labels.length;
        var bw = Math.min(56, band * 0.5);
        labels.forEach(function (label, i) {
            var h = (values[i] / sc.max) * ih;
            var x = m.left + band * i + (band - bw) / 2;
            var y = m.top + ih - h;
            var rect = el('rect', { x: x, y: y, width: bw, height: Math.max(h, 0), rx: 3, fill: colors[i % colors.length], 'class': 'ad-chart__bar' });
            svg.appendChild(rect);
            svg.appendChild(el('text', { x: x + bw / 2, y: y - 6, 'text-anchor': 'middle', 'class': 'ad-chart__val' }, short(values[i], cfg.format)));
            svg.appendChild(el('text', { x: m.left + band * i + band / 2, y: H - m.bottom + 18, 'text-anchor': 'middle' }, label));
            bindTip(container, rect, label + ': ' + full(values[i], cfg.format));
        });

        container.insertBefore(svg, container.firstChild);
    }

    function donut(container, cfg) {
        var values = cfg.values, labels = cfg.labels;
        var colors = cfg.colors && cfg.colors.length ? cfg.colors : PALETTE;
        var total = values.reduce(function (a, b) { return a + b; }, 0);
        var S = 170, r = 62, sw = 24, c = 2 * Math.PI * r;

        var svg = el('svg', { viewBox: '0 0 ' + S + ' ' + S, role: 'img', 'aria-label': 'Biểu đồ tròn' });
        svg.appendChild(el('circle', { cx: S / 2, cy: S / 2, r: r, fill: 'none', stroke: '#eef1f6', 'stroke-width': sw }));

        var offset = 0;
        values.forEach(function (v, i) {
            if (!v) return;
            var len = (v / total) * c;
            var seg = el('circle', {
                cx: S / 2, cy: S / 2, r: r, fill: 'none', stroke: colors[i % colors.length], 'stroke-width': sw,
                'stroke-dasharray': Math.max(len - 2, 0) + ' ' + (c - Math.max(len - 2, 0)),
                'stroke-dashoffset': -offset,
                transform: 'rotate(-90 ' + S / 2 + ' ' + S / 2 + ')', 'class': 'ad-chart__bar'
            });
            svg.appendChild(seg);
            bindTip(container, seg, labels[i] + ': ' + v + ' (' + Math.round(v / total * 100) + '%)');
            offset += len;
        });

        svg.appendChild(el('text', { x: S / 2, y: S / 2 + 4, 'text-anchor': 'middle', style: 'font-size:26px;font-weight:600;fill:#1b2433' }, String(total)));
        svg.appendChild(el('text', { x: S / 2, y: S / 2 + 22, 'text-anchor': 'middle' }, cfg.center || 'đơn'));
        container.insertBefore(svg, container.firstChild);

        var legend = container.parentNode.querySelector('.ad-legend');
        if (legend) {
            legend.innerHTML = '';
            labels.forEach(function (label, i) {
                var li = document.createElement('li');
                var dot = document.createElement('i');
                dot.style.background = colors[i % colors.length];
                var name = document.createElement('span');
                name.textContent = label;
                var val = document.createElement('b');
                val.textContent = values[i];
                li.appendChild(dot); li.appendChild(name); li.appendChild(val);
                legend.appendChild(li);
            });
        }
    }

    function render(container, cfg) {
        var old = container.querySelector('svg');
        if (old) old.remove();
        container.__cfg = cfg;
        if (cfg.type === 'donut') donut(container, cfg); else bar(container, cfg);
    }

    function readConfig(node) {
        function json(name, fallback) {
            try { return JSON.parse(node.getAttribute(name)); } catch (e) { return fallback; }
        }
        return {
            type: node.dataset.chart,
            labels: json('data-labels', []),
            values: json('data-values', []),
            colors: json('data-colors', null),
            format: node.dataset.format || 'int',
            ytitle: node.dataset.ytitle || '',
            center: node.dataset.center || '',
            height: Number(node.dataset.height) || 0
        };
    }

    function initAll() {
        document.querySelectorAll('[data-chart]').forEach(function (node) { render(node, readConfig(node)); });
    }

    var timer = null;
    window.addEventListener('resize', function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
            document.querySelectorAll('[data-chart]').forEach(function (node) {
                if (node.__cfg && node.__cfg.type !== 'donut') render(node, node.__cfg);
            });
        }, 120);
    });

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initAll); else initAll();

    window.AdminCharts = { render: render, full: full, short: short };
})();
