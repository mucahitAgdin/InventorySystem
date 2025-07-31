using Microsoft.AspNetCore.Mvc;
using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Controllers
{
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 📤 GET: Ürün çıkış formunu göster
        /// </summary>
        public IActionResult Exit(string? barcode)
        {
            // Barkod gönderilmişse, otomatik doldurmak için model oluştur
            var model = new StockTransaction();
            if (!string.IsNullOrEmpty(barcode))
                model.Barcode = barcode;

            return View(model);
        }

        /// <summary>
        /// 📤 POST: Ürün çıkışı yapılır (zimmet verilir)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Exit(StockTransaction transaction)
        {
            if (!ModelState.IsValid)
                return View(transaction);

            // Ürünü barkodla bul
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == transaction.Barcode);

            if (product == null)
            {
                ViewBag.Error = "Ürün bulunamadı.";
                return View(transaction);
            }

            // Miktar yeterli mi kontrol et
            if (product.Quantity < transaction.Quantity)
            {
                ModelState.AddModelError("Quantity", "Stok yetersiz.");
                return View(transaction);
            }

            // Stoktan düş
            product.Quantity -= transaction.Quantity;

            // Ürün artık dışarıda
            product.IsInStock = false;
            product.CurrentHolder = transaction.DeliveredTo;
            product.Location = "Dışarıda";

            // Hareket kaydı oluştur
            transaction.Type = TransactionType.Exit;
            transaction.TransactionDate = DateTime.Now;

            _context.StockTransaction.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün çıkışı başarıyla kaydedildi.";
            return RedirectToAction("InStockOnly", "Product");
        }
    }
}
