// /wwwroot/js/scanner.js
// Amaç:
// - USB‑HID (Zebra DS3678 HID) veya manuel giriş ile lookup.
// - Ürün bilgi panelini doldurur.
// - Barkodu tek karttaki input'a (#moveBarcode) otomatik yazar.
// - Konum/metinleri Türkçe sabit (Depo/Ofis/Stok dışı) gösterir.
// - Güvenlik: Elemanlar yoksa no‑op; 2 sn debounce; 6–7 uzunluk; basit beep.

(() => {
    // ---------- DOM helpers ----------
    const $ = (s) => document.querySelector(s);

    // Giriş alanı ve form
    const barcodeInput = $("#barcodeInput");
    const scanForm = $("#scanForm");
    const isScanPage = !!(barcodeInput && scanForm);

    // Lookup panel elemanları
    const panel = $("#resultPanel");
    const pName = $("#pName");
    const pMeta = $("#pMeta");
    const pSerial = $("#pSerial");
    const pStockBadge = $("#pStockBadge");

    // Chip/text alanları
    const pBarcodeText = $("#pBarcodeText");
    const pLocationText = $("#pLocationText");
    const pBarcodeChip = $("#pBarcodeChip");
    const pLocationChip = $("#pLocationChip");

    // ✅ Tek kart alanları
    const moveBarcode = $("#moveBarcode");
    const moveLocation = $("#moveLocation"); // <select> (Depo/Ofis/Stok dışı)

    // Uyarı alanı
    let alerts = $("#alerts");

    // ---------- State ----------
    let busy = false;
    let last = { code: "", at: 0 };

    // ---------- Utils ----------
    function beep(ok = true) {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = "sine";
            osc.frequency.value = ok ? 880 : 220;
            osc.connect(gain); gain.connect(ctx.destination);
            gain.gain.setValueAtTime(0.03, ctx.currentTime);
            osc.start(); osc.stop(ctx.currentTime + 0.08);
        } catch { /* sessiz */ }
    }

    function setBorder(ok) {
        if (!barcodeInput) return;
        barcodeInput.classList.remove("is-valid", "is-invalid");
        barcodeInput.classList.add(ok ? "is-valid" : "is-invalid");
        if (!ok) setTimeout(() => barcodeInput.classList.remove("is-invalid"), 1500);
    }

    function showAlert(type, msg) {
        if (!alerts) {
            alerts = document.createElement("div");
            alerts.id = "alerts";
            scanForm?.insertAdjacentElement("afterend", alerts);
        }
        alerts.innerHTML = `<div class="alert alert-${type} py-2 my-2">${msg}</div>`;
    }

    function clearAndFocus() {
        if (!barcodeInput) return;
        barcodeInput.value = "";
        barcodeInput.focus();
    }

    // ---------- Türkçe enum/konum yardımcıları ----------
    // normalize: her tür giriş değerini ('depo','warehouse','Ofis','office', 'stok dışı', 'out' vb.) 3 standarda indirger
    function normalizeLocation(loc, isInStock) {
        const raw = (loc ?? "").toString().trim().toLowerCase();
        if (raw === "depo" || raw === "warehouse" || (isInStock && raw === "")) return "depo";
        if (raw === "ofis" || raw === "office") return "ofis";
        if (raw === "stok dışı" || raw === "stokdisi" || raw === "stok-dışı" || raw === "out" || raw === "disarida" || raw === "dışarıda") return "stok dışı";
        // bilinmiyorsa stokta bilgisine göre varsay
        return isInStock ? "depo" : "stok dışı";
    }
    function trLocation(locStd) {
        // ekran metni — sabit Türkçe
        switch ((locStd || "").toLowerCase()) {
            case "depo": return "Depo";
            case "ofis": return "Ofis";
            case "stok dışı": return "Stok dışı";
            default: return "Stok dışı";
        }
    }

    // ---------- Lookup UI doldurma ----------
    function applyLookupToUI(p, rawCode) {
        const name = p.name ?? p.Name ?? "-";
        const bc = p.barcode ?? p.Barcode ?? rawCode;
        const type = p.productType ?? p.ProductType;
        const brand = p.brand ?? p.Brand;
        const model = p.model ?? p.Model;
        const serial = p.serialNumber ?? p.SerialNumber;
        const isInStock = (p.isInStock ?? p.IsInStock) === true;
        const locStd = normalizeLocation(p.location ?? p.Location, isInStock); // depo|ofis|stok dışı
        const locText = trLocation(locStd);

        // Üst panel
        if (pName) pName.textContent = `${name} (${bc})`;
        if (pMeta) pMeta.textContent = [type, brand, model].filter(Boolean).join(" • ");
        if (pSerial) pSerial.textContent = serial ? `SN: ${serial}` : "";

        // Açıklayıcı alanlar
        if (pBarcodeText) pBarcodeText.textContent = bc;
        if (pLocationText) pLocationText.textContent = locText;

        // Chip'ler
        if (pBarcodeChip) {
            pBarcodeChip.textContent = `Barkod: ${bc}`;
            pBarcodeChip.setAttribute("data-help", "Sistemde ürünün benzersiz kimliği");
        }
        if (pLocationChip) {
            pLocationChip.textContent = `Konum: ${locText}`;
            pLocationChip.setAttribute("data-help", "Depo / Ofis / Stok dışı");
        }

        // Rozet
        if (pStockBadge) {
            const inDepot = locStd === "depo";
            pStockBadge.textContent = inDepot ? "DEPO" : locText.toUpperCase();
            pStockBadge.className = "badge rounded-pill";
            // FORVIA renk değişkeni varsa kullan
            if (getComputedStyle(document.documentElement).getPropertyValue('--forvia-green')) {
                pStockBadge.style.backgroundColor = inDepot
                    ? getComputedStyle(document.documentElement).getPropertyValue('--forvia-green').trim() || "#00B48F"
                    : getComputedStyle(document.documentElement).getPropertyValue('--forvia-coral').trim() || "#F06473";
                pStockBadge.style.color = "#fff";
            } else {
                pStockBadge.classList.add(inDepot ? "text-bg-success" : "text-bg-danger");
            }
        }

        // Paneli aç
        panel?.classList.remove("d-none");

        // ✅ Barkodu tek karta yaz
        if (moveBarcode) moveBarcode.value = bc;

        // (İsteğe bağlı UX) Konumu öner: depodaysa "Ofis", değilse "Depo" öner
        if (moveLocation) {
            const suggested = (locStd === "depo") ? "Ofis" : "Depo";
            // sadece boş/placeholder seçiliyken yaz
            if (!moveLocation.value) {
                // Select içinde değerler Türkçe sabit olarak tanımlı olmalı
                const opt = Array.from(moveLocation.options).find(o => o.text.trim().toLowerCase() === suggested.toLowerCase());
                if (opt) moveLocation.value = opt.value;
            }
        }
    }

    // ---------- CORE: Lookup ----------
    async function lookup(code) {
        if (!isScanPage || busy) return;

        const raw = (code || "").trim();

        // uzunluk kontrolü
        if (raw.length < 6 || raw.length > 7) {
            setBorder(false); beep(false);
            showAlert("warning", "Barkod 6–7 karakter olmalıdır.");
            clearAndFocus();
            return;
        }

        // 2 sn debounce
        const now = Date.now();
        if (last.code === raw && (now - last.at) < 2000) return;
        last = { code: raw, at: now };

        // busy + buton
        busy = true;
        const submitBtn = scanForm?.querySelector('button[type="submit"]');
        if (submitBtn) { submitBtn.disabled = true; submitBtn.textContent = "Aranıyor…"; }

        try {
            const res = await fetch(`/api/barcodes/lookup?code=${encodeURIComponent(raw)}`, {
                headers: { "Accept": "application/json" }
            });

            if (res.status === 404) {
                setBorder(false); beep(false);
                panel?.classList.add("d-none");
                showAlert("danger", "Bu barkodla ürün bulunamadı.");
                return;
            }
            if (!res.ok) throw new Error("lookup failed");

            const p = await res.json();
            applyLookupToUI(p, raw);

            setBorder(true); beep(true);
            alerts && (alerts.innerHTML = "");
        } catch (err) {
            setBorder(false); beep(false);
            panel?.classList.add("d-none");
            showAlert("danger", "Lookup başarısız. Ağ bağlantısını kontrol edin.");
        } finally {
            busy = false;
            if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = "Lookup"; }
            clearAndFocus();
        }
    }

    // ---------- Events ----------
    // Form submit -> lookup
    scanForm?.addEventListener("submit", (e) => {
        e.preventDefault();
        e.stopPropagation();
        lookup(barcodeInput?.value);
    });

    // Barkod okuyucu Enter gönderdiğinde
    barcodeInput?.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            e.stopPropagation();
            lookup(barcodeInput.value);
        }
    });

    // Sticky focus
    let stickyFocus = true;
    document.addEventListener("click", (e) => {
        if (!stickyFocus || !barcodeInput) return;
        const t = e.target;
        if (!t) return;
        const interactive = ["input", "textarea", "select", "button", "a", "label"];
        const tag = (t.tagName || "").toLowerCase();
        if (interactive.includes(tag) || t.closest("form") || t.closest(".modal")) return;
        barcodeInput.focus();
    });
})();
