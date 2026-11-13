(() => {
  const API = '/api/dia-chi';
  const q = (s) => document.querySelector(s);
  const tinhEl = q('#dc-tinh'), xaEl = q('#dc-xa'), duong = q('#dc-duong');
  const out = q('#diaChi'), box = q('#dc-box'), free = q('#dc-free');
  if (!tinhEl || !xaEl || !duong || !out || !box || !free) {
    console.error('[dia-chi] Thiếu phần tử trong view'); return;
  }
  if (!window.TomSelect) { console.error('[dia-chi] Chưa nạp Tom Select'); return; }

  const base = { maxOptions: null, create: false, valueField: 'value', labelField: 'text', searchField: ['text'] };
  const tinh = new TomSelect(tinhEl, { ...base, placeholder: 'Tỉnh / Thành phố' });
  const xa = new TomSelect(xaEl, { ...base, placeholder: 'Phường / Xã' });
  xa.disable();

  const setList = (ts, list) => {
  ts.clear(true); ts.clearOptions();
  ts.addOptions([...list]
    .sort((a, b) => (a.ten ?? a.name).localeCompare(b.ten ?? b.name, 'vi'))
    .map(i => ({ value: i.code, text: i.name })));
};
  const text = (ts) => { const v = ts.getValue(); return v ? (ts.options[v]?.text ?? '') : ''; };
  const ghep = () => {
    out.value = [duong.value.trim(), text(xa), text(tinh)].filter(Boolean).join(', ');
  };
  const fallback = (why) => {
    console.error('[dia-chi] Quay về ô nhập tay:', why);
    box.style.display = 'none';
    free.style.display = '';
    out.value = '';
  };
  const getJson = async (url) => {
    const r = await fetch(url);
    if (!r.ok) throw new Error(`${url} -> HTTP ${r.status}`);
    const data = await r.json();
    if (!Array.isArray(data) || data.length === 0) throw new Error(`${url} -> danh sách rỗng`);
    return data;
  };

  getJson(`${API}/tinh`).then(list => setList(tinh, list)).catch(fallback);

  tinh.on('change', async (v) => {
    xa.clear(true); xa.clearOptions(); xa.disable(); ghep();
    if (!v) return;
    try {
      setList(xa, await getJson(`${API}/tinh/${v}/xa`));
      xa.enable();
    } catch (e) { fallback(e); }
  });
  xa.on('change', ghep);
  duong.addEventListener('input', ghep);
})();