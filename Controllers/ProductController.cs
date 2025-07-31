using Microsoft.AspNetCore.Mvc;
using InventorySystem.Data;
using InventorySystem.Models;
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

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            Log.Information("Yeni ürün eklendi: {@Name}, {@Barcode}", model.Name, model.Barcode);

            return RedirectToAction("Index");
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
    }
}
