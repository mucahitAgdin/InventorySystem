using InventorySystem.Data;
using InventorySystem.Models;
using InventorySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockController> _logger;

        private const string DeliveredToWarehousePlaceholder = "Depo"; // IN'de entity Required uyumu

        public StockController(ApplicationDbContext context, ILogger<StockController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        // ... ctor aynen

        [HttpGet] public IActionResult In() => View(new StockInVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> In(StockInVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == vm.Barcode);
            if (p is null) { ModelState.AddModelError(nameof(vm.Barcode), "Ürün bulunamadı."); return View(vm); }

            p.Quantity += vm.Quantity;
            p.Location = "Depo";
            p.CurrentHolder = null;

            await _context.StockTransaction.AddAsync(new StockTransaction
            {
                Barcode = vm.Barcode,
                Type = vm.Type,                                  // TransactionType.Entry
                Quantity = vm.Quantity,
                DeliveredTo = DeliveredToWarehousePlaceholder,   // Entity [Required] uyumu
                DeliveredBy = vm.DeliveredBy,
                Note = vm.Note
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Stok girişi kaydedildi.";
            return RedirectToAction("InStockOnly", "Product");
        }

        [HttpGet] public IActionResult Out() => View(new StockOutVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Out(StockOutVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == vm.Barcode);
            if (p is null) { ModelState.AddModelError(nameof(vm.Barcode), "Ürün bulunamadı."); return View(vm); }
            if (vm.Quantity <= 0) { ModelState.AddModelError(nameof(vm.Quantity), "Miktar en az 1 olmalıdır."); return View(vm); }
            if (p.Quantity < vm.Quantity) { ModelState.AddModelError(nameof(vm.Quantity), $"Yetersiz stok. Mevcut: {p.Quantity}"); return View(vm); }

            // miktarı düş
            p.Quantity -= vm.Quantity;

            // ✅ kalan miktara göre konum ve holder kuralı
            if (p.Quantity == 0)
            {
                p.Location = "Dışarıda";
                p.CurrentHolder = vm.DeliveredTo;
            }
            else
            {
                p.Location = "Depo";
                p.CurrentHolder = null; // depoda kaldıysa zimmet olmaz
            }

            await _context.StockTransaction.AddAsync(new StockTransaction
            {
                Barcode = vm.Barcode,
                Type = vm.Type,                // TransactionType.Exit
                Quantity = vm.Quantity,
                DeliveredTo = vm.DeliveredTo,  // Required
                DeliveredBy = vm.DeliveredBy,
                Note = vm.Note
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Stok çıkışı kaydedildi.";
            return RedirectToAction("All", "Product");
        }

        [HttpGet]
        public async Task<IActionResult> History(string? barcode = null)
        {
            var q = _context.StockTransaction.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(barcode)) q = q.Where(t => t.Barcode == barcode);

            var list = await q.OrderByDescending(t => t.TransactionDate)
                              .ThenByDescending(t => t.Id)
                              .ToListAsync();

            ViewBag.Barcode = barcode ?? "";
            return View(list);
        }
    }
}
