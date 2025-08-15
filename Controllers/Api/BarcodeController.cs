using InventorySystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Controllers.Api
{
    [Route("api/barcodes")]
    [ApiController]
    [Authorize(Roles ="Admin")]
    public class BarcodeController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public BarcodeController(ApplicationDbContext db) => _db = db;

        // GET /api/barcodes/lookup?code=XXXXXX
        public async Task<IActionResult> Lookup([FromQuery] string code)
        {
            // 1) Basit doğrulama (6-7 kuralı mevcut sisteme uyum için)
            var barcode = (code ?? string.Empty).Trim();
            if (barcode.Length < 6 || barcode.Length > 7)
                return BadRequest(new { message = "Invalid barcode length." });

            // 2) Ürün çek
            var p = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p == null) return NotFound(new {message = "Product not found."});

            // 3) Front-end'e yeterli bilgiler
            return Ok(new
            {
                p.Id,
                p.Name,
                p.Barcode,
                p.ProductType,
                p.Brand,
                p.Model,
                p.SerialNumber,
                p.Location,
                p.CurrentHolder,
                p.IsInStock
            });
        }
    }
}