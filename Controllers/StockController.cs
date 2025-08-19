using InventorySystem.Data;
using InventorySystem.Models;
using InventorySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;


namespace InventorySystem.Controllers
{
    // Yalnızca Admin erişsin (Cookie Auth + Role Claim gerekli)
    [Authorize(Roles = "Admin")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockController> _logger;


        // Entity tarafında StockTransaction.DeliveredTo [Required] olduğundan
        // stok girişi (Entry) için placeholder kullanıyoruz.
        private const string DeliveredToWarehousePlaceholder = "Depo";

        public StockController(ApplicationDbContext context, ILogger<StockController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ---- STOCK IN (Giriş) ------------------------------------------------

        [HttpGet]
        public IActionResult Move()
        {
            var tuple = Tuple.Create(new StockInVm(), new StockOutVm());
            return View("Move", tuple);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> In(StockInVm vm)
        {
            if (!ModelState.IsValid) return View("Move", Tuple.Create(vm, new StockOutVm()));

            var barcode = (vm.Barcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Barkod zorunludur.");
                return View(vm);
            }
            if (barcode.Length < 6 || barcode.Length > 7)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Barkod 6 ile 7 karakter arasında olmalıdır.");
                return View(vm);
            }

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu barkod ile kayıtlı ürün yok.");
                return View(vm);
            }
            if (string.Equals(p.Location, "Depo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu ürün zaten depoda görünüyor.");
                return View(vm);
            }

            // >>> Serilog LogContext scope
            using (LogContext.PushProperty("Op", "Stock-IN"))
            using (LogContext.PushProperty("Barcode", barcode))
            using (LogContext.PushProperty("User", User?.Identity?.Name ?? "admin"))
            {
                _logger.LogInformation("Stock IN started for {Barcode}", barcode);

                try
                {
                    p.Location = "Depo";
                    p.CurrentHolder = null;

                    await _context.StockTransaction.AddAsync(new StockTransaction
                    {
                        Barcode = barcode,
                        Type = TransactionType.Entry,
                        Quantity = 1,
                        DeliveredTo = DeliveredToWarehousePlaceholder,
                        DeliveredBy = vm.DeliveredBy,
                        Note = vm.Note
                    });

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Stock IN success for {Barcode}", barcode);
                    TempData["Success"] = "Stok girişi kaydedildi.";
                    return RedirectToAction("InStockOnly", "Product");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stock IN failed for {Barcode}", barcode);
                    TempData["Error"] = "Stok girişi sırasında beklenmeyen bir hata oluştu.";
                    return View(vm);
                }
            }
        }


        // ---- STOCK OUT (Çıkış) ----------------------------------------------

        [HttpGet]
        public IActionResult Out() => View(new StockOutVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Out(StockOutVm vm)
        {
            if (!ModelState.IsValid) return View("Move", Tuple.Create(new StockInVm(), vm));

            var barcode = (vm.Barcode ?? string.Empty).Trim();
            if (barcode.Length < 6 || barcode.Length > 7)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Barkod 6 ile 7 karakter arasında olmalıdır.");
                return View(vm);
            }

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu barkod ile kayıtlı ürün yok.");
                return View(vm);
            }
            if (!string.Equals(p.Location, "Depo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu ürün depoda değil (zaten dışarıda).");
                return View(vm);
            }

            // >>> Serilog LogContext scope
            using (LogContext.PushProperty("Op", "Stock-OUT"))
            using (LogContext.PushProperty("Barcode", barcode))
            using (LogContext.PushProperty("DeliveredTo", vm.DeliveredTo))
            using (LogContext.PushProperty("User", User?.Identity?.Name ?? "admin"))
            {
                _logger.LogInformation("Stock OUT started for {Barcode}", barcode);

                try
                {
                    p.Location = "Dışarıda";
                    p.CurrentHolder = vm.DeliveredTo;

                    await _context.StockTransaction.AddAsync(new StockTransaction
                    {
                        Barcode = barcode,
                        Type = TransactionType.Exit,
                        Quantity = 1,
                        DeliveredTo = vm.DeliveredTo,
                        DeliveredBy = vm.DeliveredBy,
                        Note = vm.Note
                    });

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Stock OUT success for {Barcode}", barcode);
                    TempData["Success"] = "Stok çıkışı kaydedildi.";
                    return RedirectToAction("All", "Product");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stock OUT failed for {Barcode}", barcode);
                    TempData["Error"] = "Stok çıkışı sırasında beklenmeyen bir hata oluştu.";
                    return View(vm);
                }
            }
        }



        // ---- HISTORY --------------------------------------------------------

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
