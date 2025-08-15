using System;
using System.Linq;
using System.Threading.Tasks;
using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace InventorySystem.Controllers
{
    // Yalnızca Admin erişsin
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

        // ProductController.cs
        [HttpGet]
        public async Task<IActionResult> TypesJson()
        {
            var types = await _context.Products
                .AsNoTracking()
                .Where(p => p.ProductType != null && p.ProductType != "")
                .Select(p => p.ProductType!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return Json(types);
        }

        [HttpGet]
        public async Task<IActionResult> ListJson()
        {
            var data = await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .Select(p => new {
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


        // GET: /Product/All   (?productType=.. opsiyonel filtre)
        // Tekil model: lokasyona bakmadan TÜM ürünleri getirir.
        public async Task<IActionResult> All(string? productType = null)
        {
            var q = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(productType))
                q = q.Where(p => p.ProductType == productType);

            var products = await q.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.SelectedProductType = productType;
            return View("AllProducts", products);
        }

        // Eski linkler 404 vermesin diye geçici yönlendirme
        [HttpGet]
        public IActionResult AllProducts(string? productType) =>
            RedirectToAction(nameof(All), new { productType });

        // GET: /Product/InStockOnly
        // Depoda olan tekil ürünler (Location == "Depo")
        public async Task<IActionResult> InStockOnly()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Location == "Depo")
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
        // Tekil model: Quantity bind etmiyoruz (yok); IsInStock DB/computed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Barcode,CurrentHolder,Location,ProductType,Brand,Model,Description,SerialNumber,DateTime")]
            Product input)
        {
            if (input is null) return BadRequest();

            // Barkod boş olamaz
            if (string.IsNullOrWhiteSpace(input.Barcode))
                ModelState.AddModelError(nameof(input.Barcode), "Barcode is required.");
           
            if (!string.IsNullOrEmpty(input.Barcode) &&
                (input.Barcode.Length < 6 || input.Barcode.Length > 7))
            {
                ModelState.AddModelError(nameof(input.Barcode), "Barcode must be between 6 and 7 characters.");
            }


            // ✅ Unique kontrolleri
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

            // Varsayılan konum
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
        // Tekil model: Quantity yok; IsInStock DB/computed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Barcode,CurrentHolder,Location,ProductType,Brand,Model,Description,SerialNumber,DateTime")]
            Product input)
        {
            if (input is null || id != input.Id) return BadRequest();

            // Unique kontrolleri (kendi kaydını hariç tut)
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
                // Minimal update (attach + modified)
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
        [HttpGet] public IActionResult Scan() => View();

        // (opsiyonel) /Product/Index → All’a yönlendirme
        public IActionResult Index() => RedirectToAction(nameof(All));
    }
}
