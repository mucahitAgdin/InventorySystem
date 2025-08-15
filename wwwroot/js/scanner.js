// scanner.js
// Amaç: HID (keyboard wedge) barkod okuyucu ile Enter'da lookup yapmak,
// sonucu ekranda göstermek, IN/OUT için mevcut StockController POST'larına
// gizli formlar üzerinden submit etmek.


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

    // Basit sesli geri bildirim (opsiyonel: /sounds/success.wav & error.wav ekleyebilirsin)
    function beep(ok = true) {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = "sine";
            osc.frequency.value = ok ? 880 : 220; // success: yüksek, error: düşük ton
            osc.connect(gain); gain.connect(ctx.destination);
            gain.gain.setValueAtTime(0.03, ctx.currentTime);
            osc.start(); osc.stop(ctx.currentTime + 0.08);
        } catch { }
    }

    function setBorder(ok) {
        barcodeInput.classList.remove("is-valid", "is-invalid");
        barcodeInput.classList.add(ok ? "is-valid" : "is-invalid");
    }

    function showAlert(type, msg) {
        alerts.innerHTML = `<div class="alert alert-${type} py-2 my-1">${msg}</div>`;
    }

    // Aynı barkodu 2 sn içinde tekrar işlememek için basit koruma
    let last = { code: "", at: 0 };

    async function lookup(code) {
        const raw = (code || "").trim();
        if (raw.length < 6 || raw.length > 7) {
            setBorder(false); beep(false);
            showAlert("warning", "Barcode must be 6–7 chars.");
            return;
        }

        const now = Date.now();
        if (last.code === raw && (now - last.at) < 2000) return; // debounce
        last = { code: raw, at: now };

        try {
            const res = await fetch(`/api/barcodes/lookup?code=${encodeURIComponent(raw)}`, {
                headers: { "Accept": "application/json" }
            });

            if (res.status === 404) {
                setBorder(false); beep(false);
                panel.classList.add("d-none");
                showAlert("danger", "No product found for this barcode.");
                return;
            }
            if (!res.ok) throw new Error("lookup failed");

            const p = await res.json();

            // Kartı doldur
            pName.textContent = `${p.name ?? p.Name} (${p.barcode ?? p.Barcode})`;
            const type = p.productType ?? p.ProductType;
            const brand = p.brand ?? p.Brand;
            const model = p.model ?? p.Model;
            pMeta.textContent = [type, brand, model].filter(Boolean).join(" • ");
            const serial = p.serialNumber ?? p.SerialNumber;
            pSerial.textContent = serial ? `SN: ${serial}` : "";

            const isInStock = (p.isInStock ?? p.IsInStock) === true;
            const loc = p.location ?? p.Location;
            pStockBadge.textContent = isInStock ? "IN STOCK (Depot)" : (loc || "Out");
            pStockBadge.className = `badge rounded-pill ${isInStock ? "text-bg-success" : "text-bg-warning"}`;

            // IN: sadece depoda DEĞİLSE; OUT: sadece depodaysa
            btnIn.disabled = isInStock;     // depoda ise IN kapalı
            btnOut.disabled = !isInStock;   // depoda değilse OUT kapalı
            outForm.classList.add("d-none"); // her lookup'ta OUT formu gizle

            // Buton davranışları
            btnIn.onclick = () => doIn(raw);
            btnOut.onclick = () => {
                outForm.classList.remove("d-none");
                deliveredTo.focus();
            };

            panel.classList.remove("d-none");
            setBorder(true); beep(true);
            alerts.innerHTML = "";
        } catch (e) {
            setBorder(false); beep(false);
            panel.classList.add("d-none");
            showAlert("danger", "Lookup failed. Check network.");
        }
    }

    // Mevcut /Stock/In POST'una gizli form ile submit
    function doIn(barcode) {
        // DeliveredTo = "Depo" kuralı controller tarafında set ediliyor (placeholder),
        // burada sadece isteğe bağlı DeliveredBy/Note dolduruyoruz.
        formIn.querySelector('input[name="Barcode"]').value = barcode;
        formIn.querySelector('input[name="DeliveredBy"]').value = "Scanner";
        formIn.querySelector('input[name="Note"]').value = "Scanned IN";
        formIn.submit(); // normal POST -> mevcut controller çalışır
    }

    // Mevcut /Stock/Out POST'una gizli form ile submit
    function doOut(barcode) {
        const to = deliveredTo.value.trim();
        const by = deliveredBy.value.trim() || "Scanner";
        const nt = note.value.trim();

        if (!to) {
            deliveredTo.focus();
            return showAlert("warning", "Delivered To is required.");
        }
        formOut.querySelector('input[name="Barcode"]').value = barcode;
        formOut.querySelector('input[name="DeliveredTo"]').value = to;
        formOut.querySelector('input[name="DeliveredBy"]').value = by;
        formOut.querySelector('input[name="Note"]').value = nt;
        formOut.submit();
    }

    // OUT onay butonu
    btnOutConfirm?.addEventListener("click", (e) => {
        e.preventDefault();
        const codeMatch = (pName.textContent.match(/\((.+)\)$/) || [])[1];
        if (codeMatch) doOut(codeMatch);
    });

    // Enter ile arama
    scanForm.addEventListener("submit", (e) => {
        e.preventDefault();
        lookup(barcodeInput.value);
        barcodeInput.value = "";
    });

    barcodeInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            lookup(barcodeInput.value);
            barcodeInput.value = "";
        }
    });

    // Odak kaçarsa geri al
    window.addEventListener("load", () => barcodeInput?.focus());
    document.addEventListener("click", () => barcodeInput?.focus());
})();
