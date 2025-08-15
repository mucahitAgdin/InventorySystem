// /wwwroot/js/scanner.js
// Purpose:
// - USB‑HID barkod okuyucular ile (Zebra DS3678 SR | FIPS) Enter’da canlı arama.
// - Ürün kartını doldurur, mevcut StockController POST’larına gizli formlar ile submit eder.
// Güvenli kullanım: Elemanlar yoksa no‑op; 2 sn debounce; 6–7 uzunluk kontrolü; basit beep.

(() => {
    const $ = (s) => document.querySelector(s);

    // ---- Elements (opsiyonel) ----
    const barcodeInput = $("#barcodeInput");
    const scanForm = $("#scanForm");
    const panel = $("#resultPanel");
    const pName = $("#pName");
    const pMeta = $("#pMeta");
    const pSerial = $("#pSerial");
    const pStockBadge = $("#pStockBadge");
    const btnIn = $("#btnIn");
    const btnOut = $("#btnOut");
    const outForm = $("#outForm");
    const deliveredTo = $("#deliveredTo");
    const deliveredBy = $("#deliveredBy");
    const note = $("#note");
    const btnOutConfirm = $("#btnOutConfirm");
    const alerts = $("#alerts");
    const formIn = $("#formIn");
    const formOut = $("#formOut");

    const isScanPage = !!(barcodeInput && scanForm);

    // ---- Helpers ----
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
        } catch { /* ignore */ }
    }

    function setBorder(ok) {
        if (!barcodeInput) return;
        barcodeInput.classList.remove("is-valid", "is-invalid");
        barcodeInput.classList.add(ok ? "is-valid" : "is-invalid");
    }

    function showAlert(type, msg) {
        if (!alerts) return;
        alerts.innerHTML = `<div class="alert alert-${type} py-2 my-1">${msg}</div>`;
    }

    let last = { code: "", at: 0 }; // debounce

    // ---- Lookup ----
    async function lookup(code) {
        if (!isScanPage) return;

        const raw = (code || "").trim();
        if (raw.length < 6 || raw.length > 7) {
            setBorder(false); beep(false);
            return showAlert("warning", "Barcode must be 6–7 chars.");
        }
        const now = Date.now();
        if (last.code === raw && (now - last.at) < 2000) return;
        last = { code: raw, at: now };

        try {
            const res = await fetch(`/api/barcodes/lookup?code=${encodeURIComponent(raw)}`, {
                headers: { "Accept": "application/json" }
            });

            if (res.status === 404) {
                setBorder(false); beep(false);
                panel?.classList.add("d-none");
                return showAlert("danger", "No product found for this barcode.");
            }
            if (!res.ok) throw new Error("lookup failed");

            const p = await res.json();

            // Kartı doldur
            if (pName) {
                const name = p.name ?? p.Name ?? "-";
                const barcode = p.barcode ?? p.Barcode ?? "-";
                pName.textContent = `${name} (${barcode})`;
            }
            if (pMeta) {
                const type = p.productType ?? p.ProductType;
                const brand = p.brand ?? p.Brand;
                const model = p.model ?? p.Model;
                pMeta.textContent = [type, brand, model].filter(Boolean).join(" • ");
            }
            if (pSerial) {
                const serial = p.serialNumber ?? p.SerialNumber;
                pSerial.textContent = serial ? `SN: ${serial}` : "";
            }
            if (pStockBadge) {
                const isInStock = (p.isInStock ?? p.IsInStock) === true;
                const loc = p.location ?? p.Location;
                pStockBadge.textContent = isInStock ? "IN STOCK (Depot)" : (loc || "Out");
                pStockBadge.className = `badge rounded-pill ${isInStock ? "text-bg-success" : "text-bg-warning"}`;
            }

            // Aksiyonların durumu
            const isInStock = (p.isInStock ?? p.IsInStock) === true;
            if (btnIn) btnIn.disabled = isInStock;   // depoda ise IN kapalı
            if (btnOut) btnOut.disabled = !isInStock; // depoda değilse OUT kapalı
            outForm?.classList.add("d-none");

            // Click davranışı
            if (btnIn) btnIn.onclick = () => doIn(raw);
            if (btnOut) btnOut.onclick = () => { outForm?.classList.remove("d-none"); deliveredTo?.focus(); };

            panel?.classList.remove("d-none");
            setBorder(true); beep(true);
            if (alerts) alerts.innerHTML = "";
        } catch {
            setBorder(false); beep(false);
            panel?.classList.add("d-none");
            showAlert("danger", "Lookup failed. Check network.");
        }
    }

    // ---- Hidden form submit ----
    function doIn(barcode) {
        if (!formIn) return showAlert("danger", "IN form not found.");
        formIn.querySelector('input[name="Barcode"]')?.setAttribute("value", barcode);
        formIn.querySelector('input[name="DeliveredBy"]')?.setAttribute("value", "Scanner");
        formIn.querySelector('input[name="Note"]')?.setAttribute("value", "Scanned IN");
        formIn.submit();
    }

    function doOut(barcode) {
        if (!formOut) return showAlert("danger", "OUT form not found.");
        const to = deliveredTo?.value.trim();
        const by = (deliveredBy?.value.trim() || "Scanner");
        const nt = (note?.value.trim() || "");
        if (!to) {
            deliveredTo?.focus();
            return showAlert("warning", "Delivered To is required.");
        }
        formOut.querySelector('input[name="Barcode"]')?.setAttribute("value", barcode);
        formOut.querySelector('input[name="DeliveredTo"]')?.setAttribute("value", to);
        formOut.querySelector('input[name="DeliveredBy"]')?.setAttribute("value", by);
        formOut.querySelector('input[name="Note"]')?.setAttribute("value", nt);
        formOut.submit();
    }

    // OUT onay
    btnOutConfirm?.addEventListener("click", (e) => {
        e.preventDefault();
        const m = pName?.textContent?.match(/\((.+)\)$/);
        const code = m ? m[1] : null;
        if (code) doOut(code);
    });

    // Enter/submit wiring
    scanForm?.addEventListener("submit", (e) => {
        e.preventDefault();
        lookup(barcodeInput?.value);
        if (barcodeInput) barcodeInput.value = "";
    });
    barcodeInput?.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            lookup(barcodeInput.value);
            barcodeInput.value = "";
        }
    });

    // Odak koruma
    window.addEventListener("load", () => { barcodeInput?.focus(); });
    document.addEventListener("click", () => { barcodeInput?.focus(); });
})();
