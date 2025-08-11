using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InventorySystem.Data;
using InventorySystem.Models;

// ✅ (opsiyonel ama önerilir) Sayfayı sadece Admin rolüne aç
using Microsoft.AspNetCore.Authorization;

namespace InventorySystem.Controllers
{
    // ⚠️ Cookie-Auth + Role claim kuruluysa aktif et
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Product/All   (?productType=.. destekler)
        // - Tüm ürünleri listeler. Lokasyona BAKMAZ. (Depo/Dışarıda fark etmez.)
        // - Sadece kullanıcı filtre girerse ProductType'a göre daraltır.
        public async Task<IActionResult> All(string? productType = null)
        {
            var q = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(productType))
                q = q.Where(p => p.ProductType == productType);

            var products = await q
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.SelectedProductType = productType;
            return View("AllProducts", products);
        }

        // Eski linkler 404 vermesin diye alias (geçiş dönemi için bırakıldı)
        [HttpGet]
        public IActionResult AllProducts(string? productType) =>
            RedirectToAction(nameof(All), new { productType });

        // GET: /Product/InStockOnly
        // - Sadece depodaki ürünleri göster. (Dışarıda olanları sakla)
        // - IsInStock computed olsa da, iş ihtiyacın net: Depo şartı
        public async Task<IActionResult> InStockOnly()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Quantity > 0 && p.Location == "Depo") // ← iş kuralı
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View("InStockOnly", products);
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return BadRequest();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (product is null) return NotFound();
            return View(product);
        }

        // GET: /Product/Create
        public IActionResult Create() => View();

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            // ✅ Overposting koruması
            [Bind("Name,Barcode,Quantity,CurrentHolder,Location,ProductType,Brand,Model,Description,SerialNumber,DateTime")]
            Product input)
        {
            if (input is null) return BadRequest();

            // ✅ Benzersiz barkod kontrolü (UI/UX için server-side doğrulama)
            if (!string.IsNullOrWhiteSpace(input.Barcode) &&
                await _context.Products.AsNoTracking().AnyAsync(p => p.Barcode == input.Barcode))
            {
                ModelState.AddModelError(nameof(input.Barcode), "This barcode already exists.");
            }

            // ❌ IsInStock SET ETME — computed column (DB hesaplıyor)
            // input.IsInStock = (input.Quantity >= 1);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Product validation failed: {@ModelState}", ModelState);
                return View(input);
            }

            try
            {
                await _context.Products.AddAsync(input);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product created successfully.";
                return RedirectToAction(nameof(All));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create Product failed for {@Input}", input);
                TempData["Error"] = "An error occurred while creating the product.";
                return View(input);
            }
        }

        // GET: /Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return BadRequest();

            var product = await _context.Products.FindAsync(id.Value);
            if (product is null) return NotFound();

            return View(product);
        }

        // POST: /Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            // ✅ Overposting koruması (IsInStock dahil edilmedi; computed)
            [Bind("Id,Name,Barcode,Quantity,CurrentHolder,Location,ProductType,Brand,Model,Description,SerialNumber,DateTime")]
            Product input)
        {
            if (input is null || id != input.Id) return BadRequest();

            // ❌ IsInStock SET ETME — computed column
            // input.IsInStock = (input.Quantity >= 1);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Product validation failed: {@ModelState}", ModelState);
                return View(input);
            }

            try
            {
                // Daha güvenli güncelleme: Attach + Modified
                _context.Attach(input);
                _context.Entry(input).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction(nameof(All));
            }
            catch (DbUpdateConcurrencyException cex)
            {
                if (!await _context.Products.AnyAsync(p => p.Id == id))
                    return NotFound();

                _logger.LogError(cex, "Concurrency error on Edit for Id={Id}", id);
                TempData["Error"] = "Concurrency error while updating the product.";
                return View(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit Product failed for Id={Id}", id);
                TempData["Error"] = "An error occurred while updating the product.";
                return View(input);
            }
        }

        // POST: /Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return BadRequest();

            try
            {
                var product = await _context.Products.FindAsync(id.Value);
                if (product is null) return NotFound();

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product deleted.";
                return RedirectToAction(nameof(All));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete Product failed for Id={Id}", id);
                TempData["Error"] = "An error occurred while deleting the product.";
                return RedirectToAction(nameof(All));
            }
        }
    }
}
