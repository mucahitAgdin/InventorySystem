using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// 🔽 ekle
using Microsoft.AspNetCore.Authorization;

namespace InventorySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StockController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public IActionResult Exit(string? barcode)
        {
            var model = new StockTransaction();
            if (!string.IsNullOrEmpty(barcode)) model.Barcode = barcode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Exit(StockTransaction t)
        {
            if (!ModelState.IsValid) return View(t);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == t.Barcode);
            if (product == null) { ViewBag.Error = "Ürün bulunamadı."; return View(t); }
            if (product.Quantity < t.Quantity) { ModelState.AddModelError("Quantity", "Stok yetersiz."); return View(t); }

            product.Quantity -= t.Quantity;
            product.IsInStock = false;
            product.CurrentHolder = t.DeliveredTo;
            product.Location = "Dışarıda";

            t.Type = TransactionType.Exit;
            t.TransactionDate = DateTime.Now;

            _context.StockTransaction.Add(t);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün çıkışı kaydedildi.";
            return RedirectToAction("InStockOnly", "Product");
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var list = await _context.StockTransaction.OrderByDescending(x => x.Id).ToListAsync();
            return View(list);
        }

        // ❌ Artık gerek yok
        // public override void OnActionExecuting(...) { ... }
    }
}