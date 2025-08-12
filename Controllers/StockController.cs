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
    // Cookie-Auth + Role claim kurulu: sadece Admin erişir
    [Authorize(Roles = "Admin")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockController> _logger;

        // Entity tarafında DeliveredTo [Required]; IN’de placeholder kullanıyoruz
        private const string DeliveredToWarehousePlaceholder = "Depo";

        public StockController(ApplicationDbContext context, ILogger<StockController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ---- STOCK IN -------------------------------------------------------

        [HttpGet]
        public IActionResult In() => View(new StockInVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> In(StockInVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // 1) Barkodu normalize et (server-side güvenlik)
            var barcode = (vm.Barcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Barkod zorunludur.");
                return View(vm);
            }

            // 2) Ürün var mı? Yoksa işlem kesinlikle yapılmaz
            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu barkod ile kayıtlı ürün yok.");
                return View(vm);
            }

            try
            {
                // 3) Stok artır + depoya al
                p.Quantity += vm.Quantity;
                p.Location = "Depo";
                p.CurrentHolder = null;

                // 4) Hareket logu (enum ile)
                await _context.StockTransaction.AddAsync(new StockTransaction
                {
                    Barcode = barcode,                      // normalize edilmiş değer
                    Type = TransactionType.Entry,
                    Quantity = vm.Quantity,
                    DeliveredTo = DeliveredToWarehousePlaceholder, // entity [Required] uyumu
                    DeliveredBy = vm.DeliveredBy,
                    Note = vm.Note
                    // TransactionDate: DB default GETDATE() / model default
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Stok girişi kaydedildi.";
                return RedirectToAction("InStockOnly", "Product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock IN failed for {Barcode}", barcode);
                TempData["Error"] = "Stok girişi sırasında hata oluştu.";
                return View(vm);
            }
        }

        // ---- STOCK OUT ------------------------------------------------------

        [HttpGet]
        public IActionResult Out() => View(new StockOutVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Out(StockOutVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var barcode = (vm.Barcode ?? string.Empty).Trim();

            // Ürün var mı?
            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), "Bu barkod ile kayıtlı ürün yok.");
                return View(vm);
            }

            // Miktar kuralları
            if (vm.Quantity <= 0)
            {
                ModelState.AddModelError(nameof(vm.Quantity), "Miktar en az 1 olmalıdır.");
                return View(vm);
            }
            if (p.Quantity < vm.Quantity)
            {
                ModelState.AddModelError(nameof(vm.Quantity), $"Yetersiz stok. Mevcut: {p.Quantity}");
                return View(vm);
            }

            try
            {
                // Stok düş
                p.Quantity -= vm.Quantity;

                // Kural: Kalan > 0 ise ürünün bir kısmı hâlâ depoda.
                if (p.Quantity == 0)
                {
                    p.Location = "Dışarıda";
                    p.CurrentHolder = vm.DeliveredTo;
                }
                else
                {
                    p.Location = "Depo";
                    p.CurrentHolder = null;
                }

                // Log
                await _context.StockTransaction.AddAsync(new StockTransaction
                {
                    Barcode = barcode,
                    Type = TransactionType.Exit,
                    Quantity = vm.Quantity,
                    DeliveredTo = vm.DeliveredTo, // VM'de Required
                    DeliveredBy = vm.DeliveredBy,
                    Note = vm.Note
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Stok çıkışı kaydedildi.";
                return RedirectToAction("All", "Product"); // All Products'ta her zaman görünür
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock OUT failed for {Barcode}", barcode);
                TempData["Error"] = "Stok çıkışı sırasında hata oluştu.";
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
