using InventorySystem.Data;
using InventorySystem.Models;
using InventorySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace InventorySystem.Controllers
{
    // Yalnızca Admin
    [Authorize(Roles = "Admin")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockController> _logger;

        // StockTransaction.DeliveredTo [Required] — IN için placeholder
        private const string DeliveredToWarehousePlaceholder = "Depo";

        public StockController(ApplicationDbContext context, ILogger<StockController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // -------------------- MOVE (tek sayfa) --------------------

        [HttpGet]
        public IActionResult Move()
        {
            // Move.cshtml: @model Tuple<StockInVm, StockOutVm>
            return View(Tuple.Create(new StockInVm(), new StockOutVm()));
        }

        // Eski linkler bozulmasın:
        [HttpGet] public IActionResult In() => RedirectToAction(nameof(Move));
        [HttpGet] public IActionResult Out() => RedirectToAction(nameof(Move));

        // -------------------- STOCK IN (Giriş) --------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Move.cshtml'de alan adları Item1.* olduğu için prefix ile bind ediyoruz.
        public async Task<IActionResult> In([Bind(Prefix = "Item1")] StockInVm vm)
        {
            // ModelState yanlışsa yine Move'a dön; OUT tarafını boş gönder.
            if (!ModelState.IsValid)
                return View("Move", Tuple.Create(vm, new StockOutVm()));

            var barcode = (vm.Barcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                ModelState.AddModelError("Item1.Barcode", "Barkod zorunludur.");
                return View("Move", Tuple.Create(vm, new StockOutVm()));
            }
            // İş kuralı: 6–7 uzunluk (VM 200 char izin verir ama biz iş kuralı koyuyoruz)
            if (barcode.Length < 6 || barcode.Length > 7)
            {
                ModelState.AddModelError("Item1.Barcode", "Barkod 6–7 karakter olmalıdır.");
                return View("Move", Tuple.Create(vm, new StockOutVm()));
            }

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError("Item1.Barcode", "Bu barkod ile kayıtlı ürün yok.");
                return View("Move", Tuple.Create(vm, new StockOutVm()));
            }
            if (string.Equals(p.Location, "Depo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Item1.Barcode", "Bu ürün zaten depoda.");
                return View("Move", Tuple.Create(vm, new StockOutVm()));
            }

            using (LogContext.PushProperty("Op", "Stock-IN"))
            using (LogContext.PushProperty("Barcode", barcode))
            using (LogContext.PushProperty("User", User?.Identity?.Name ?? "admin"))
            {
                try
                {
                    // Ürünü depoda işaretle
                    p.Location = "Depo";
                    p.CurrentHolder = null;

                    // StockTransaction.DeliveredTo zorunlu → "Depo" yazıyoruz
                    await _context.StockTransaction.AddAsync(new StockTransaction
                    {
                        Barcode = barcode,
                        Type = TransactionType.Entry,
                        Quantity = 1,
                        DeliveredTo = DeliveredToWarehousePlaceholder, // <-- required alan
                        DeliveredBy = vm.DeliveredBy,
                        Note = vm.Note
                    });

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Stok girişi kaydedildi.";
                    return RedirectToAction("InStockOnly", "Product");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stock IN failed for {Barcode}", barcode);
                    TempData["Error"] = "Stok girişi sırasında beklenmeyen bir hata oluştu.";
                    return View("Move", Tuple.Create(vm, new StockOutVm()));
                }
            }
        }

        // -------------------- STOCK OUT (Çıkış) --------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Move.cshtml'de OUT alanları Item2.* — prefix ile bind
        public async Task<IActionResult> Out([Bind(Prefix = "Item2")] StockOutVm vm)
        {
            if (!ModelState.IsValid)
                return View("Move", Tuple.Create(new StockInVm(), vm));

            var barcode = (vm.Barcode ?? string.Empty).Trim();
            if (barcode.Length < 6 || barcode.Length > 7)
            {
                ModelState.AddModelError("Item2.Barcode", "Barkod 6–7 karakter olmalıdır.");
                return View("Move", Tuple.Create(new StockInVm(), vm));
            }

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError("Item2.Barcode", "Bu barkod ile kayıtlı ürün yok.");
                return View("Move", Tuple.Create(new StockInVm(), vm));
            }
            if (!string.Equals(p.Location, "Depo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Item2.Barcode", "Bu ürün depoda değil (zaten dışarıda).");
                return View("Move", Tuple.Create(new StockInVm(), vm));
            }

            using (LogContext.PushProperty("Op", "Stock-OUT"))
            using (LogContext.PushProperty("Barcode", barcode))
            using (LogContext.PushProperty("DeliveredTo", vm.DeliveredTo))
            using (LogContext.PushProperty("User", User?.Identity?.Name ?? "admin"))
            {
                try
                {
                    // Ürünü dışarıda işaretle
                    p.Location = "Dışarıda";
                    p.CurrentHolder = vm.DeliveredTo;

                    await _context.StockTransaction.AddAsync(new StockTransaction
                    {
                        Barcode = barcode,
                        Type = TransactionType.Exit,
                        Quantity = 1,
                        DeliveredTo = vm.DeliveredTo,  // OUT'ta zorunlu
                        DeliveredBy = vm.DeliveredBy,
                        Note = vm.Note
                    });

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Stok çıkışı kaydedildi.";
                    return RedirectToAction("All", "Product");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stock OUT failed for {Barcode}", barcode);
                    TempData["Error"] = "Stok çıkışı sırasında beklenmeyen bir hata oluştu.";
                    return View("Move", Tuple.Create(new StockInVm(), vm));
                }
            }
        }

        // -------------------- HISTORY --------------------

        [HttpGet]
        public async Task<IActionResult> History(string? barcode = null)
        {
            var q = _context.StockTransaction.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(barcode))
                q = q.Where(t => t.Barcode == barcode.Trim());

            var list = await q
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            ViewBag.Barcode = barcode ?? "";
            return View(list);
        }
    }
}
