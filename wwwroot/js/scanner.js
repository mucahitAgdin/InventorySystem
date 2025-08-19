// /wwwroot/js/scanner.js
// Purpose:
// - USB‑HID barkod okuyucu (Zebra DS3678 HID) veya manuel giriş ile lookup.
// - Ürün bilgi panelini doldurur.
// - Depo durumuna göre barkodu otomatik olarak doğru karta (IN/OUT) yazar.
// - İsteğe bağlı olarak gizli formlar ile mevcut StockController POST’larına submit eder.
//
// Güvenli kullanım: Elemanlar yoksa no‑op; 2 sn debounce; 6–7 uzunluk kontrolü; basit beep.

(() => {
    // ---------- Helpers / DOM ----------
    const $ = (s) => document.querySelector(s);

    // Giriş alanı ve form
    const barcodeInput = $("#barcodeInput");
    const scanForm = $("#scanForm");
    const isScanPage = !!(barcodeInput && scanForm);

    // Lookup sonucu paneli
    const panel = $("#resultPanel");
    const pName = $("#pName");
    const pMeta = $("#pMeta");
    const pSerial = $("#pSerial");
    const pStockBadge = $("#pStockBadge");

    // Kalıcı IN/OUT kartlarındaki barkod alanları
    const inBarcode = $("#inBarcode");
    const outBarcode = $("#outBarcode");

    // Gizli POST formları (opsiyonel kullanım için)
    const formIn = $("#formIn");
    const formOut = $("#formOut");

    // Uyarı alanı (scan formunun altına koyacağız)
    let alerts = $("#alerts");

    // ---------- State ----------
    let busy = false;                 // eşzamanlı lookup engelle
    let last = { code: "", at: 0 };   // debounce için

    // ---------- UI Utils ----------
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
        // sayfada #alerts yoksa hızlıca oluştur
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

    // ---------- Ürün bilgisini UI'a uygula ----------
    function applyLookupToUI(p, rawCode) {
        // Normalize alanlar
        const name = p.name ?? p.Name ?? "-";
        const bc = p.barcode ?? p.Barcode ?? rawCode;
        const type = p.productType ?? p.ProductType;
        const brand = p.brand ?? p.Brand;
        const model = p.model ?? p.Model;
        const serial = p.serialNumber ?? p.SerialNumber;
        const isInStock = (p.isInStock ?? p.IsInStock) === true;
        const location = (p.location ?? p.Location ?? "").toString().toLowerCase();

        // Üst panel metinleri
        if (pName) pName.textContent = `${name} (${bc})`;
        if (pMeta) pMeta.textContent = [type, brand, model].filter(Boolean).join(" • ");
        if (pSerial) pSerial.textContent = serial ? `SN: ${serial}` : "";

        // Badge (FORVIA renkleri varsa onları kullan, yoksa Bootstrap fallback)
        if (pStockBadge) {
            const inDepot = isInStock || location === "depo" || location === "warehouse";
            pStockBadge.textContent = inDepot ? "IN STOCK (Depot)" : (location || "Out");
            pStockBadge.className = "badge rounded-pill"; // reset class
            // CSS değişkenleriyle renklendir; yoksa text-bg-* kullan
            if (getComputedStyle(document.documentElement).getPropertyValue('--forvia-green')) {
                pStockBadge.style.backgroundColor = inDepot
                    ? getComputedStyle(document.documentElement).getPropertyValue('--forvia-green').trim() || "#00B48F"
                    : getComputedStyle(document.documentElement).getPropertyValue('--forvia-coral').trim() || "#F06473";
                pStockBadge.style.color = "#fff";
            } else {
                pStockBadge.classList.add(inDepot ? "text-bg-success" : "text-bg-danger");
            }
        }

        // Paneli göster
        panel?.classList.remove("d-none");

        // Barkodu doğru karta yaz (diğerini temizle)
        const inDepot = isInStock || location === "depo" || location === "warehouse";
        if (inDepot) {
            if (outBarcode) outBarcode.value = bc;
            if (inBarcode) inBarcode.value = "";
        } else {
            if (inBarcode) inBarcode.value = bc;
            if (outBarcode) outBarcode.value = "";
        }
    }

    // ---------- CORE: Lookup ----------
    async function lookup(code) {
        if (!isScanPage || busy) return;

        const raw = (code || "").trim();

        // uzunluk validasyonu (6–7)
        if (raw.length < 6 || raw.length > 7) {
            setBorder(false); beep(false);
            showAlert("warning", "Barcode must be 6–7 chars.");
            clearAndFocus();
            return;
        }

        // 2 sn debounce
        const now = Date.now();
        if (last.code === raw && (now - last.at) < 2000) return;
        last = { code: raw, at: now };

        // busy + buton durumu
        busy = true;
        const submitBtn = scanForm?.querySelector('button[type="submit"]');
        if (submitBtn) { submitBtn.disabled = true; submitBtn.textContent = "Looking…"; }

        try {
            const res = await fetch(`/api/barcodes/lookup?code=${encodeURIComponent(raw)}`, {
                headers: { "Accept": "application/json" }
            });

            if (res.status === 404) {
                setBorder(false); beep(false);
                panel?.classList.add("d-none");
                showAlert("danger", "No product found for this barcode.");
                return;
            }
            if (!res.ok) throw new Error("lookup failed");

            const p = await res.json();

            // UI'ı doldur ve doğru karta yaz
            applyLookupToUI(p, raw);

            setBorder(true); beep(true);
            alerts && (alerts.innerHTML = "");
        } catch (err) {
            setBorder(false); beep(false);
            panel?.classList.add("d-none");
            showAlert("danger", "Lookup failed. Check network.");
        } finally {
            // state temizliği
            busy = false;
            if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = "Lookup"; }
            clearAndFocus();
        }
    }

    // ---------- Opsiyonel: gizli formlar ile gönderim ----------
    function doIn(barcode) {
        if (!formIn) return showAlert("danger", "IN form not found.");
        formIn.querySelector('input[name="Barcode"]')?.setAttribute("value", barcode);
        formIn.querySelector('input[name="DeliveredBy"]')?.setAttribute("value", "Scanner");
        formIn.querySelector('input[name="Note"]')?.setAttribute("value", "Scanned IN");
        formIn.submit();
    }
    function doOut(barcode, to, by = "Scanner", nt = "") {
        if (!formOut) return showAlert("danger", "OUT form not found.");
        if (!to) return showAlert("warning", "Delivered To is required.");
        formOut.querySelector('input[name="Barcode"]')?.setAttribute("value", barcode);
        formOut.querySelector('input[name="DeliveredTo"]')?.setAttribute("value", to);
        formOut.querySelector('input[name="DeliveredBy"]')?.setAttribute("value", by);
        formOut.querySelector('input[name="Note"]')?.setAttribute("value", nt);
        formOut.submit();
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

    // Sticky focus: boş alana tıklanınca odak barkod alanına dönsün
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
