using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InventorySystem.Controllers
{
    // 🔒 Yalnızca Admin rolüne izin ver
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        // 🌍 Çoklu dil desteği için localizer
        private readonly IStringLocalizer<ProductController> _localizer;

        // Küçük yardımcı: LocalizedString → string
        private string T(string key, params object[] args) => _localizer[key, args].Value; // FIX: TempData için hep string

        // Constructor → Dependency Injection
        public ProductController(ApplicationDbContext context,
                                 ILogger<ProductController> logger,
                                 IStringLocalizer<ProductController> localizer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        // ---- Routing uyumluluğu --------------------------------------------
        // Eski linkleri koru: /Product/AllProducts → /Product/All
        [HttpGet]
        public IActionResult AllProducts(string? productType, string? location, string? brand, string? serial) =>
            RedirectToAction(nameof(All), new { productType, location, brand, serial });

        // ---- JSON kaynakları (autocomplete / dropdown) ----------------------

        [HttpGet]
        public async Task<IActionResult> TypesJson()
        {
            var types = await _context.Products
                .AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.ProductType))
                .Select(p => p.ProductType!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return Json(types);
        }

        [HttpGet]
        public async Task<IActionResult> BrandsJson()
        {
            var brands = await _context.Products
                .AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return Json(brands);
        }

        [HttpGet]
        public async Task<IActionResult> LocationsJson()
        {
            var locations = await _context.Products
                .AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return Json(locations);
        }

        [HttpGet]
        public async Task<IActionResult> ListJson()
        {
            var data = await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    barcode = p.Barcode,
                    productType = p.ProductType,
                    brand = p.Brand,
                    model = p.Model,
                    location = p.Location,
                    currentHolder = p.CurrentHolder,
                    isInStock = p.IsInStock
                })
                .ToListAsync();

            return Json(data);
        }

        // ---- Listeleme ------------------------------------------------------

        public async Task<IActionResult> All(
            string? location = null,
            string? serial = null,
            string? productType = null,
            string? brand = null)
        {
            var q = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(location))
                q = q.Where(p => p.Location == location);

            if (!string.IsNullOrWhiteSpace(productType))
                q = q.Where(p => p.ProductType == productType);

            if (!string.IsNullOrWhiteSpace(brand))
                q = q.Where(p => p.Brand == brand);

            if (!string.IsNullOrWhiteSpace(serial))
            {
                var s = serial.Trim();
                q = q.Where(p => p.SerialNumber != null && p.SerialNumber.Contains(s));
            }

            var products = await q.OrderByDescending(p => p.Id).ToListAsync();

            ViewBag.SelectedLocation = location ?? "";
            ViewBag.SelectedProductType = productType ?? "";
            ViewBag.SelectedBrand = brand ?? "";
            ViewBag.SelectedSerial = serial ?? "";

            return View("AllProducts", products);
        }

        // ---- Detay / CRUD ---------------------------------------------------

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return BadRequest();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (product is null) return NotFound();
            return View(product);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Barcode,CurrentHolder,Location,ProductType,Brand,Model,Description,SerialNumber,DateTime")]
            Product input)
        {
            if (input is null) return BadRequest();

            // Basit validasyon örnekleri
            if (string.IsNullOrWhiteSpace(input.Barcode))
                ModelState.AddModelError(nameof(input.Barcode), "Barcode is required.");

            if (!string.IsNullOrEmpty(input.Barcode) &&
                (input.Barcode.Length < 6 || input.Barcode.Length > 7))
            {
                ModelState.AddModelError(nameof(input.Barcode), "Barcode must be between 6 and 7 characters.");
            }

            if (!string.IsNullOrWhiteSpace(input.Barcode) &&
                await _context.Products.AsNoTracking().AnyAsync(p => p.Barcode == input.Barcode))
            {
                ModelState.AddModelError(nameof(input.Barcode), "This barcode already exists.");
            }

            if (!string.IsNullOrWhiteSpace(input.SerialNumber) &&
                await _context.Products.AsNoTracking().AnyAsync(p => p.SerialNumber == input.SerialNumber))
            {
                ModelState.AddModelError(nameof(input.SerialNumber), "This serial number already exists.");
            }

            if (string.IsNullOrWhiteSpace(input.Location))
                input.Location = "Depo";

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Product validation failed: {@ModelState}", ModelState);
                return View(input);
            }

            try
            {
                await _context.Products.AddAsync(input);
                await _context.SaveChangesAsync();

                TempData["Success"] = T("CreateSuccess"); // FIX: LocalizedString yerine string
                return RedirectToAction(nameof(All));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create Product failed for {@Input}", input);
                TempData["Error"] = T("CreateError"); // FIX
                return View(input);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return BadRequest();

            var product = await _context.Products.FindAsync(id.Value);
            if (product is null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Barcode, ProductType,Brand,Model,Description,SerialNumber")]
            Product input)
        {
            if (input is null || id != input.Id) return BadRequest();

            // Benzersizlik ve uzunluk validasyonları
            if (!string.IsNullOrWhiteSpace(input.Barcode) &&
                await _context.Products.AsNoTracking().AnyAsync(p => p.Barcode == input.Barcode && p.Id != id))
            {
                ModelState.AddModelError(nameof(input.Barcode), "This barcode is used by another product.");
            }

            if (!string.IsNullOrEmpty(input.Barcode) &&
                (input.Barcode.Length < 6 || input.Barcode.Length > 7))
            {
                ModelState.AddModelError(nameof(input.Barcode), "Barcode must be between 6 and 7 characters.");
            }

            if (!string.IsNullOrWhiteSpace(input.SerialNumber) &&
                await _context.Products.AsNoTracking().AnyAsync(p => p.SerialNumber == input.SerialNumber && p.Id != id))
            {
                ModelState.AddModelError(nameof(input.SerialNumber), "This serial number is used by another product.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Product validation failed: {@ModelState}", ModelState);
                return View(input);
            }

            try
            {
                _context.Attach(input);
                _context.Entry(input).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["Success"] = T("UpdateSuccess"); // FIX
                return RedirectToAction(nameof(All));
            }
            catch (DbUpdateConcurrencyException cex)
            {
                if (!await _context.Products.AnyAsync(p => p.Id == id))
                    return NotFound();

                _logger.LogError(cex, "Concurrency error on Edit for Id={Id}", id);
                TempData["Error"] = T("UpdateConcurrencyError"); // FIX
                return View(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit Product failed for Id={Id}", id);
                TempData["Error"] = T("UpdateError"); // FIX
                return View(input);
            }
        }

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

                TempData["Success"] = T("DeleteSuccess"); // FIX
                return RedirectToAction(nameof(All));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete Product failed for Id={Id}", id);
                TempData["Error"] = T("DeleteError"); // FIX
                return RedirectToAction(nameof(All));
            }
        }

        [HttpGet]
        public IActionResult Scan() => View();

        public IActionResult Index() => RedirectToAction(nameof(All));
    }
}
