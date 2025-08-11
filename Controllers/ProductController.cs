
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InventorySystem.Data;
using InventorySystem.Models; // Product namespace’in neyse ona göre düzelt

namespace InventorySystem.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Product/All
        // ProductController

        // GET: /Product/All   (?productType=.. destekler)
        public async Task<IActionResult> All(string? productType = null)
        {
            var q = _context.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(productType))
                q = q.Where(p => p.ProductType == productType);

            var products = await q.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.SelectedProductType = productType;
            return View("AllProducts", products);
        }

        // Eski linkler 404 vermesin diye alias (geçici)
        [HttpGet]
        public IActionResult AllProducts(string? productType) =>
            RedirectToAction(nameof(All), new { productType });

        // GET: /Product/InStockOnly
        public async Task<IActionResult> InStockOnly()
        {
            var products = await _context.Products
                                         .AsNoTracking()
                                         .Where(p => p.Quantity > 0)  //geçici
                                         .OrderByDescending(p => p.Id)
                                         .ToListAsync();
            return View("InStockOnly", products);
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return BadRequest();

            var product = await _context.Products.AsNoTracking()
                                 .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (product is null) return NotFound();
            return View(product);
        }

        // GET: /Product/Create
        public IActionResult Create() => View();

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Barcode,Quantity,CurrentHolder,Location,ProductType,Brand,Model,Description,SerialNumber,DateTime")] Product input)
        {
            // Null guard (view’den hiç gelmezse)
            if (input is null) return BadRequest();


            if (!string.IsNullOrWhiteSpace(input.Barcode) &&
                await _context.Products.AsNoTracking().AnyAsync(p => p.Barcode == input.Barcode))
            {
                ModelState.AddModelError(nameof(input.Barcode), "This barcode already exists.");
                return View(input);
            }


            // Derived field: IsInStock mantığını merkezileştir
            input.IsInStock = (input.Quantity >= 1);

            if (!ModelState.IsValid)
            {
                // Hataları logla – validation mesajları UI’da gösterilecek
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Barcode,Quantity,CurrentHolder,Location,ProductType,Brand,Model,Description,SerialNumber,DateTime,IsInStock")] Product input)
        {
            if (id != input.Id) return BadRequest();
            if (input is null) return BadRequest();

            // türetilen alanı tekrar hesapla
            input.IsInStock = (input.Quantity >= 1);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Product validation failed: {@ModelState}", ModelState);
                return View(input);
            }

            try
            {
                // Tracking’i basit tut: Attach + Modified
                _context.Attach(input);
                _context.Entry(input).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction(nameof(All));
            }
            catch (DbUpdateConcurrencyException cex)
            {
                // Kayıt silinmiş olabilir
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
