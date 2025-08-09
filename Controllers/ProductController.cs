using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
// 🔽 ekle
using Microsoft.AspNetCore.Authorization;

namespace InventorySystem.Controllers
{
    [Authorize(Roles = "Admin")] // ✅ Artık erişim buradan kontrol
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product productData)
        {
            if (!ModelState.IsValid)
                return View(productData);

            // Varsayılan değerler
            productData.IsInStock = true;     // Yeni ürün stokta başlasın
            productData.CurrentHolder = null; // Kimseye zimmetli değil
            productData.Location = "Depo";    // Depoda

            _context.Products.Add(productData);
            await _context.SaveChangesAsync();

            Log.Information("Yeni ürün eklendi: {@Name}, {@Barcode}", productData.Name, productData.Barcode);

            return RedirectToAction("InStockOnly");
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product productData)
        {
            if (!ModelState.IsValid)
                return View(productData);

            _context.Products.Update(productData);
            await _context.SaveChangesAsync();

            Log.Information("Ürün güncellendi: {@Name}", productData.Name);

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p != null)
            {
                _context.Products.Remove(p);
                await _context.SaveChangesAsync();
                Log.Warning("Ürün silindi: {@Barcode}", p.Barcode);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> SearchByBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return Json(new { success = false, message = "Barkod boş." });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
            if (product == null)
                return Json(new { success = false, message = "Ürün bulunamadı" });

            return Json(new { success = true, data = new { name = product.Name, barcode = product.Barcode, quantity = product.Quantity } });
        }

        public async Task<IActionResult> InStockOnly()
        {
            var inStock = await _context.Products.Where(p => p.IsInStock).ToListAsync();
            return View(inStock);
        }

        public async Task<IActionResult> AllProducts(string? productType = null)
        {
            var q = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(productType)) q = q.Where(p => p.ProductType == productType);

            var list = await q.OrderBy(p => p.ProductType).ThenBy(p => p.Name).ToListAsync();
            ViewBag.SelectedProductType = productType;
            return View(list);
        }

        public async Task<IActionResult> GetProductTypes()
        {
            var types = await _context.Products.Select(p => p.ProductType).Distinct().ToListAsync();
            return Json(types);
        }

        public IActionResult Index() => RedirectToAction("InStockOnly");

        // ❌ Artık gerek yok
        // public override void OnActionExecuting(...) { ... }
    }
}
