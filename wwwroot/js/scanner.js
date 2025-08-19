// /wwwroot/js/scanner.js
// Purpose:
// - USB‑HID barkod okuyucular ile (Zebra DS3678 SR | FIPS) Enter’da canlı arama.
// - Ürün kartını doldurur, mevcut StockController POST’larına gizli formlar ile submit eder.
// Güvenli kullanım: Elemanlar yoksa no‑op; 2 sn debounce; 6–7 uzunluk kontrolü; basit beep.

// /wwwroot/js/scanner.js
(() => {
    const $ = (s) => document.querySelector(s);

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

    // ✅ yeni: tekrar çağrıyı engelle
    let busy = false;
    let last = { code: "", at: 0 }; // debounce

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
        } catch { }
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
    function clearAndFocus() {
        if (barcodeInput) {
            barcodeInput.value = "";
            barcodeInput.focus();
        }
    }

    async function lookup(code) {
        if (!isScanPage || busy) return;
        const raw = (code || "").trim();

        // uzunluk validasyonu
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

        // ✅ yeni: busy & buton durumu
        busy = true;
        const submitBtn = scanForm?.querySelector('button[type="submit"]');
        if (submitBtn) { submitBtn.disabled = true; submitBtn.innerText = "Looking…"; }

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

            // kart doldur
            if (pName) {
                const name = p.name ?? p.Name ?? "-";
                const bc = p.barcode ?? p.Barcode ?? "-";
                pName.textContent = `${name} (${bc})`;
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

            // butonlar
            const isInStock = (p.isInStock ?? p.IsInStock) === true;
            if (btnIn) btnIn.disabled = isInStock;   // depoda ise IN kapalı
            if (btnOut) btnOut.disabled = !isInStock;  // depoda değilse OUT kapalı
            outForm?.classList.add("d-none");
            if (btnIn) btnIn.onclick = () => doIn(raw);
            if (btnOut) btnOut.onclick = () => { outForm?.classList.remove("d-none"); deliveredTo?.focus(); };

            panel?.classList.remove("d-none");
            setBorder(true); beep(true);
            alerts && (alerts.innerHTML = "");
        } catch (err) {
            setBorder(false); beep(false);
            panel?.classList.add("d-none");
            showAlert("danger", "Lookup failed. Check network.");
        } finally {
            // ✅ yeni: state temizliği
            busy = false;
            if (submitBtn) { submitBtn.disabled = false; submitBtn.innerText = "Lookup"; }
            clearAndFocus();
        }
    }

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

    // ✅ önemli: event bubbling’i kes
    scanForm?.addEventListener("submit", (e) => {
        e.preventDefault();
        e.stopPropagation();
        lookup(barcodeInput?.value);
    });
    barcodeInput?.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            e.stopPropagation();
            lookup(barcodeInput.value);
        }
    });

    let stickyFocus = true;

    // Boş/arka plana tıklandıysa focus geri al; form kontrollerinde asla zorlama
    document.addEventListener("click", (e) => {
        if (!stickyFocus || !barcodeInput) return;

        const t = e.target;
        if (!t) return;

        // Etkileşimli elemanlar: input, textarea, select, button, link vs.
        const interactive = ["input", "textarea", "select", "button", "a", "label"];
        const tag = (t.tagName || "").toLowerCase();

        // Bir form kontrolüne ya da formun içine tıklandıysa odak GERİ ALMA
        if (interactive.includes(tag) || t.closest("form") || t.closest(".modal")) return;

        // Aksi halde barkod alanına odak
        barcodeInput.focus();
    });
    function setBorder(ok) {
        if (!barcodeInput) return;
        barcodeInput.classList.remove("is-valid", "is-invalid");
        barcodeInput.classList.add(ok ? "is-valid" : "is-invalid");
        if (!ok) setTimeout(() => barcodeInput.classList.remove("is-invalid"), 1500);
    }


})();
