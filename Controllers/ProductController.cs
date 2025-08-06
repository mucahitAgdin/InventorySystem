using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics.Contracts;

namespace InventorySystem.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Sayfa yüklenince boş form döner
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Sistem tarafından atanacak varsayılan değerler:
            model.IsInStock = true;                // Yeni ürün stokta başlasın
            model.CurrentHolder = null;            // Başlangıçta kimseye zimmetli değil
            model.Location = "Depo";               // Depoda tutuluyor

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            Log.Information("Yeni ürün eklendi: {@Name}, {@Barcode}", model.Name, model.Barcode);

            return RedirectToAction("InStockOnly");
        }


        public async Task<IActionResult> Edit (int id)
        {
            var product = await _context.Products.FindAsync(id);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Products.Update(model);
            await _context.SaveChangesAsync();

            Log.Information("Ürün güncellendi: {@Name}", model.Name);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if(product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                Log.Warning("Ürün silindi: {@Barcode}", product.Barcode);
            }

            return RedirectToAction("Index");
        }

        ///<summary>
        /// Barkod numarası ile ürün aramak için kullanılır
        /// Barkod doğruysa JSON formatında ürün bilgisi döner
        /// </summary>
        /// <param name="barcode">Kullanıcının veya barkod okuyucunun gönderdiği barkod</param>
        /// <returns>JSON veri objesi (başarılı/başarısız)</returns>
        public async Task<IActionResult> SearchByBarcode(string barcode)
        {
            //Barkod boş gönderildiyse uyarı ver
            if (string.IsNullOrEmpty(barcode))
                return Json(new { success = false, message = "Barkod boş." });

            //Veritabanında barkoda sahip ürünü ara
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            //Ürün bulunamazsa hata döndür
            if (product == null)
                return Json(new { success = false, message = "Ürün bulunamadı" });

            //Ürün bulunduysa JSON ile ürün bilgilerini döndür
            return Json(new
            {
                success = true,
                data = new
                {
                    name = product.Name,
                    barcode = product.Barcode,
                    quantity = product.Quantity
                }
            });
        }


        public async Task<IActionResult> InStockOnly()
        {
            var inStock = await _context.Products
                .Where(p => p.IsInStock)
                .ToListAsync();

            return View(inStock);
        }

        public async Task<IActionResult> AllProducts(string? productType = null)
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(productType))
                query = query.Where(p => p.ProductType == productType);

            var grouped = await query
                .OrderBy(p => p.ProductType)
                .ThenBy(p => p.Name)
                .ToListAsync();

            // Seçili tipi ViewBag ile View'a gönder
            ViewBag.SelectedProductType = productType;

            return View(grouped);
        }


        // Tüm farklı product type’ları dropdown için getir
        public async Task<IActionResult> GetProductTypes()
        {
            var types = await _context.Products
                .Select(p => p.ProductType)
                .Distinct()
                .ToListAsync();
            return Json(types);
        }

        public IActionResult Index()
        {
            return RedirectToAction("InStockOnly");
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                context.Result = RedirectToAction("Login", "Admin");
            }
            base.OnActionExecuting(context);
        }

    }
}
