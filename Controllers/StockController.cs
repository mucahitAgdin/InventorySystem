using System;
using System.Linq;
using System.Threading.Tasks;
using InventorySystem.Data;
using InventorySystem.Models;
using InventorySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        public IActionResult In() => View(new StockInVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> In(StockInVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var barcode = (vm.Barcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Barkod zorunludur.");
                return View(vm);
            }

            // Ürün var mı? → Yoksa işlem YAPILMAZ (tekil modelin kuralı)
            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu barkod ile kayıtlı ürün yok.");
                return View(vm);
            }

            // Zaten depodaysa ikinci kez IN yapılmasın (UX kararı)
            if (string.Equals(p.Location, "Depo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu ürün zaten depoda görünüyor.");
                return View(vm);
            }

            try
            {
                // Tekil model: adet yok — sadece konumu/görevlendirmeyi güncelle
                p.Location = "Depo";
                p.CurrentHolder = null;

                // Hareket kaydı (Quantity = 1; tekil hareket)
                await _context.StockTransaction.AddAsync(new StockTransaction
                {
                    Barcode = barcode,
                    Type = TransactionType.Entry,
                    Quantity = 1,
                    DeliveredTo = DeliveredToWarehousePlaceholder, // entity [Required] uyumu için
                    DeliveredBy = vm.DeliveredBy,
                    Note = vm.Note
                    // TransactionDate: DB default GETDATE() / model default set ediyor
                });

                await _context.SaveChangesAsync();
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

        // ---- STOCK OUT (Çıkış) ----------------------------------------------

        [HttpGet]
        public IActionResult Out() => View(new StockOutVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Out(StockOutVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var barcode = (vm.Barcode ?? string.Empty).Trim();

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu barkod ile kayıtlı ürün yok.");
                return View(vm);
            }

            // Depoda değilse ikinci kez OUT yapılmasın
            if (!string.Equals(p.Location, "Depo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu ürün depoda değil (zaten dışarıda).");
                return View(vm);
            }

            try
            {
                // Dışarı ver (tekil model; adet yok)
                p.Location = "Dışarıda";
                p.CurrentHolder = vm.DeliveredTo;

                // Hareket kaydı (Quantity=1)
                await _context.StockTransaction.AddAsync(new StockTransaction
                {
                    Barcode = barcode,
                    Type = TransactionType.Exit,
                    Quantity = 1,
                    DeliveredTo = vm.DeliveredTo,  // VM tarafında Required
                    DeliveredBy = vm.DeliveredBy,
                    Note = vm.Note
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Stok çıkışı kaydedildi.";
                return RedirectToAction("All", "Product"); // All Products her zaman listeler
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock OUT failed for {Barcode}", barcode);
                TempData["Error"] = "Stok çıkışı sırasında beklenmeyen bir hata oluştu.";
                return View(vm);
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
