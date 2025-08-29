// File: Controllers/Api/BarcodeController.cs
// Purpose: Lookup endpoint for barcodes.
// i18n: Error/feedback messages are localized via IStringLocalizer<BarcodeController>.
//       Resource files live at Resources/Controllers.Api.BarcodeController.{culture}.resx

using InventorySystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace InventorySystem.Controllers.Api
{
    [Route("api/barcodes")]
    [ApiController]
    [Authorize(Roles = "Admin")] // keep cookie auth/role behavior
    public class BarcodeController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IStringLocalizer<BarcodeController> _localizer;

        public BarcodeController(ApplicationDbContext db, IStringLocalizer<BarcodeController> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        // GET /api/barcodes/lookup?code=XXXXXX
        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string code)
        {
            // 1) Normalize & validate (aligned with rule: length 6–7)
            var barcode = (code ?? string.Empty).Trim();
            if (barcode.Length < 6 || barcode.Length > 7)
            {
                // Localized message from Resources/Controllers.Api.BarcodeController.{lang}.resx
                return BadRequest(new
                {
                    message = _localizer["InvalidBarcodeLen"], // e.g., "Barkod 6–7 karakter olmalıdır."
                    code = "INVALID_BARCODE_LENGTH"
                });
            }

            // 2) Product lookup
            var p = await _db.Products.AsNoTracking()
                                      .FirstOrDefaultAsync(x => x.Barcode == barcode);

            if (p is null)
            {
                return NotFound(new
                {
                    message = _localizer["ProductNotFound"], // e.g., "Bu barkod ile kayıtlı ürün yok."
                    code = "PRODUCT_NOT_FOUND"
                });
            }

            // 3) Minimal payload for front-end consumption (unchanged)
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
