// site.js
const navbar = document.querySelector('.topbar');
const logoImg = document.querySelector('.navbar-brand img');

if (navbar && logoImg) {
    const whiteLogo = '/images/faurecia_inspiring_white.png';
    const blueLogo = '/images/faurecia_inspiring_blue.png';

    // Başlangıç: şeffaf navbar ise beyaz logo
    if (!navbar.classList.contains('active')) {
        logoImg.src = whiteLogo;
    }

    navbar.addEventListener('mouseenter', () => {
        navbar.classList.add('active');
        logoImg.src = blueLogo; // beyaz zemin -> mavi logo
    });

    navbar.addEventListener('mouseleave', () => {
        navbar.classList.remove('active');
        logoImg.src = whiteLogo; // koyu/şeffaf zemin -> beyaz logo
    });
}

// ===================== Product Picker (In/Out sayfaları) =====================
(function () {
    // Utils
    const $ = (sel, root = document) => root.querySelector(sel);
    const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));
    const debounce = (fn, ms = 250) => {
        let t; return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), ms); };
    };

    // Sayfa: Stok Giriş/Çıkış formu mu?
    const barcodeInput = document.querySelector('input[name="Barcode"], input#Barcode');
    if (!barcodeInput) return; // yalnız In/Out sayfalarında çalışsın

    // “Ürün Listesi” butonu (varsa id’siyle, yoksa metniyle yakala)
    let openBtn = document.getElementById('productListBtn');
    if (!openBtn) {
        openBtn = $$('button, a').find(el => (el.textContent || '').trim().toLowerCase() === 'ürün listesi');
    }
    if (!openBtn) return;

    // Modal HTML (Bootstrap 5 uyumlu)
    const modalHtml = `
<div class="modal fade" id="productPickerModal" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-dialog-scrollable modal-xl">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Ürün Listesi</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Kapat"></button>
      </div>
      <div class="modal-body">
        <div class="row g-2 mb-3">
          <div class="col-sm-6">
            <input id="pp-search" type="text" class="form-control" placeholder="Barkod / İsim ara...">
          </div>
          <div class="col-sm-4">
            <select id="pp-type" class="form-select">
              <option value="">(Tümü) Tip</option>
            </select>
          </div>
          <div class="col-sm-2 form-check d-flex align-items-center">
            <input id="pp-instock" class="form-check-input me-2" type="checkbox">
            <label class="form-check-label" for="pp-instock">Sadece depodakiler</label>
          </div>
        </div>
        <div class="table-responsive">
          <table class="table table-hover align-middle" id="pp-table">
            <thead>
              <tr>
                <th style="min-width:120px">Barkod</th>
                <th>İsim</th>
                <th>Tip</th>
                <th>Marka</th>
                <th>Model</th>
                <th>Konum</th>
                <th>Üzerinde</th>
              </tr>
            </thead>
            <tbody>
              <tr><td colspan="7" class="text-muted">Yükleniyor...</td></tr>
            </tbody>
          </table>
        </div>
      </div>
      <div class="modal-footer">
        <small class="text-muted me-auto">Bir satıra tıklayarak barkodu forma aktarabilirsiniz.</small>
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Kapat</button>
      </div>
    </div>
  </div>
</div>`;

    // Body’e ekle ve Bootstrap modal nesnesini hazırla
    if (!$('#productPickerModal')) {
        document.body.insertAdjacentHTML('beforeend', modalHtml);
    }
    const bsModal = () => bootstrap.Modal.getOrCreateInstance($('#productPickerModal'));

    // Veri tutucu
    let allProducts = [];   // ham veri (normalize edilmiş)
    let filtered = [];      // filtrelenmiş görünüm

    // JSON getir — /api/products
    async function fetchProducts() {
        const res = await fetch('/api/products', { headers: { 'Accept': 'application/json' } });
        if (!res.ok) throw new Error('Ürün listesi alınamadı');
        const data = await res.json();

        // API camelCase (name) veya PascalCase (Name) dönebilir → normalize et
        return Array.isArray(data) ? data.map(d => ({
            barcode: d.barcode ?? d.Barcode ?? '',
            name: d.name ?? d.Name ?? '',
            productType: d.productType ?? d.ProductType ?? '',
            brand: d.brand ?? d.Brand ?? '',
            model: d.model ?? d.Model ?? '',
            location: d.location ?? d.Location ?? '',
            currentHolder: d.currentHolder ?? d.CurrentHolder ?? '',
            isInStock: (d.isInStock ?? d.IsInStock) ?? undefined
        })) : [];
    }

    // Tabloyu bas
    function renderRows(rows) {
        const tb = $('#pp-table tbody');
        if (!rows.length) {
            tb.innerHTML = `<tr><td colspan="7" class="text-muted">Sonuç bulunamadı.</td></tr>`;
            return;
        }
        tb.innerHTML = rows.map(p => `
      <tr class="pp-row" data-barcode="${p.barcode}">
        <td class="fw-semibold">${p.barcode}</td>
        <td>${p.name ?? ''}</td>
        <td>${p.productType ?? ''}</td>
        <td>${p.brand ?? ''}</td>
        <td>${p.model ?? ''}</td>
        <td>${p.location ?? ''}</td>
        <td>${p.currentHolder ?? ''}</td>
      </tr>
    `).join('');
    }

    // Tip dropdown’u doldur
    function hydrateTypeFilter(list) {
        const types = Array.from(new Set(list.map(p => p.productType).filter(Boolean))).sort();
        const sel = $('#pp-type');
        sel.innerHTML = `<option value="">(Tümü) Tip</option>` +
            types.map(t => `<option value="${t}">${t}</option>`).join('');
    }

    // Filtre uygula
    function applyFilters() {
        const q = ($('#pp-search').value || '').trim().toLowerCase();
        const type = $('#pp-type').value;
        const onlyStock = $('#pp-instock').checked;

        filtered = allProducts.filter(p => {
            const matchesQ =
                !q ||
                (p.barcode && p.barcode.toLowerCase().includes(q)) ||
                (p.name && p.name.toLowerCase().includes(q));
            const matchesType = !type || p.productType === type;
            const matchesStock = !onlyStock || (p.location === 'Depo' || p.isInStock === true);
            return matchesQ && matchesType && matchesStock;
        });

        renderRows(filtered);
    }

    // Etkileşimler
    $('#pp-search')?.addEventListener('input', debounce(applyFilters, 200));
    $('#pp-type')?.addEventListener('change', applyFilters);
    $('#pp-instock')?.addEventListener('change', applyFilters);

    // Satır tıklama → Barkod doldur & modal kapat
    document.body.addEventListener('click', (e) => {
        const row = e.target.closest('#pp-table tbody tr.pp-row');
        if (!row) return;
        const bc = row.getAttribute('data-barcode');
        if (bc && barcodeInput) {
            barcodeInput.value = bc;
            // Otomatik validasyon tetikle (min 6 / max 7 kontrolü varsa)
            barcodeInput.dispatchEvent(new Event('input', { bubbles: true }));
            barcodeInput.dispatchEvent(new Event('change', { bubbles: true }));
            bsModal().hide();
            barcodeInput.focus();
        }
    });

    // Açma butonu
    openBtn.addEventListener('click', async (ev) => {
        ev.preventDefault();
        const modalEl = $('#productPickerModal');
        // İlk kez açılıyorsa veriyi yükle
        if (!allProducts.length) {
            const tb = $('#pp-table tbody');
            tb.innerHTML = `<tr><td colspan="7" class="text-muted">Yükleniyor...</td></tr>`;
            try {
                allProducts = await fetchProducts();
                hydrateTypeFilter(allProducts);
                applyFilters();
            } catch (err) {
                tb.innerHTML = `<tr><td colspan="7" class="text-danger">Liste alınamadı.</td></tr>`;
                console.error(err);
            }
        } else {
            applyFilters();
        }
        bsModal().show(modalEl);
    });
})();