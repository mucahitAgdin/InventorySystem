using InventorySystem.Data;
using InventorySystem.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ProductsController(ApplicationDbContext db) => _db = db;

        // GET api/products?term=&productType=&inStockOnly=true
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? term, [FromQuery] string? productType, [FromQuery] bool inStockOnly = false)
        {
            var q = _db.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(productType))
                q = q.Where(p => p.ProductType == productType);

            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim();
                q = q.Where(p =>
                    p.Barcode.Contains(term) ||
                    p.Name.Contains(term) ||
                    (p.Brand != null && p.Brand.Contains(term)) ||
                    (p.Model != null && p.Model.Contains(term)) ||
                    (p.SerialNumber != null && p.SerialNumber.Contains(term)));
            }

            if (inStockOnly)
                q = q.Where(p => p.Location == "Depo");

            var list = await q
                .OrderByDescending(p => p.Id)
                .Select(p => p.ToListItemDto())
                .Take(500)
                .ToListAsync();

            return Ok(list);
        }

        // GET api/products/{barcode}
        [HttpGet("{barcode}")]
        public async Task<IActionResult> GetByBarcode(string barcode)
        {
            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null) return NotFound();
            return Ok(p.ToDetailDto());
        }

        // GET api/products/{barcode}/history
        [HttpGet("{barcode}/history")]
        public async Task<IActionResult> History(string barcode)
        {
            var hist = await _db.StockTransaction.AsNoTracking()
                .Where(t => t.Barcode == barcode)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Select(t => t.ToDto())
                .Take(200)
                .ToListAsync();

            return Ok(hist);
        }
    }
}
