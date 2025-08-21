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

        public StockController(ApplicationDbContext context, ILogger<StockController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: tek sayfa
        [HttpGet]
        public IActionResult Move()
        {
            return View(new StockMoveVm());
        }

        // Legacy linkler kırılmasın
        [HttpGet] public IActionResult In() => RedirectToAction(nameof(Move));
        [HttpGet] public IActionResult Out() => RedirectToAction(nameof(Move));

        // POST: tek onay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(StockMoveVm vm)
        {
            if (!ModelState.IsValid) return View("Move", vm);

            var barcode = (vm.Barcode ?? "").Trim();
            if (barcode.Length < 6 || barcode.Length > 7)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Barkod 6–7 karakter olmalıdır.");
                return View("Move", vm);
            }

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu barkod ile kayıtlı ürün yok.");
                return View("Move", vm);
            }

            // hedef konumu metne çevir
            string targetLoc = vm.Location switch
            {
                MoveLocation.Depo => "Depo",
                MoveLocation.Ofis => "Ofis",
                MoveLocation.StokDisi => "Stok dışı",
                _ => "Depo"
            };

            // Entry/Exit kuralı:
            // - Depo = Entry (stokta)
            // - Ofis veya Stok dışı = Exit (stok dışında)
            var type = targetLoc == "Depo" ? TransactionType.Entry : TransactionType.Exit;

            using (LogContext.PushProperty("Op", "Stock-MOVE"))
            using (LogContext.PushProperty("Barcode", barcode))
            using (LogContext.PushProperty("User", User?.Identity?.Name ?? "admin"))
            {
                try
                {
                    // Ürünün güncel durumunu yaz
                    p.Location = targetLoc;

                    // Ofis/Depo için CurrentHolder'ı temizlemek mantıklı;
                    // Stok dışı senaryosunda kişi takibi istenirse ayrıyeten alan ekleriz.
                    p.CurrentHolder = null;

                    await _context.StockTransaction.AddAsync(new StockTransaction
                    {
                        Barcode = barcode,
                        Type = type,
                        Quantity = 1,
                        Location = targetLoc,
                        DeliveredBy = vm.DeliveredBy,
                        Note = vm.Note
                    });

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Hareket kaydedildi.";
                    return RedirectToAction("All", "Product");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Confirm move failed for {Barcode}", barcode);
                    TempData["Error"] = "Kayıt sırasında beklenmeyen bir hata oluştu.";
                    return View("Move", vm);
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
