using InventorySystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Controllers.Api
{
    [Route("api/barcodes")]
    [ApiController]
    [Authorize(Roles = "Admin")] // mevcut Cookie Auth/Role ile uyumlu
    public class BarcodeController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public BarcodeController(ApplicationDbContext db) => _db = db;

        // GET /api/barcodes/lookup?code=XXXXXX
        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string code)
        {
            // 1) Mevcut kurala hizalı basit kontrol (6–7 karakter)
            var barcode = (code ?? string.Empty).Trim();
            if (barcode.Length < 6 || barcode.Length > 7)
                return BadRequest(new { message = "Invalid barcode length." });

            // 2) Ürün
            var p = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p == null) return NotFound(new { message = "Product not found." });

            // 3) Front-end’in ihtiyacı olan alanlar
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
