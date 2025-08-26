// File: Controllers/StockController.cs
// Purpose: Stock move + history endpoints.
// i18n: Use IStringLocalizer<StockController> for controller messages (TempData, ModelState).
// NOTE: View texts are handled by their own view .resx files.

using InventorySystem.Data;
using InventorySystem.Models;
using InventorySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog.Context;

namespace InventorySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockController> _logger;
        private readonly IStringLocalizer<StockController> _localizer;

        public StockController(
            ApplicationDbContext context,
            ILogger<StockController> logger,
            IStringLocalizer<StockController> localizer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        // GET: single page move UI
        [HttpGet]
        public IActionResult Move() => View(new StockMoveVm());

        // Keep legacy routes alive
        [HttpGet] public IActionResult In() => RedirectToAction(nameof(Move));
        [HttpGet] public IActionResult Out() => RedirectToAction(nameof(Move));

        // POST: single confirm endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(StockMoveVm vm)
        {
            if (!ModelState.IsValid) return View("Move", vm);

            var barcode = (vm.Barcode ?? "").Trim();
            if (barcode.Length < 6 || barcode.Length > 7)
            {
                ModelState.AddModelError(nameof(vm.Barcode), _localizer["InvalidBarcodeLen"]);
                return View("Move", vm);
            }

            var p = await _context.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p is null)
            {
                ModelState.AddModelError(nameof(vm.Barcode), _localizer["ProductNotFound"]);
                return View("Move", vm);
            }

            // Map enum to persisted TR location text (domain rule stays unchanged)
            string targetLoc = vm.Location switch
            {
                MoveLocation.Depo => "Depo",
                MoveLocation.Ofis => "Ofis",
                MoveLocation.StokDisi => "Stok dışı",
                _ => "Depo"
            };

            // Business rule:
            // - "Depo" => Entry (in stock)
            // - "Ofis" or "Stok dışı" => Exit (out of stock)
            var type = targetLoc == "Depo" ? TransactionType.Entry : TransactionType.Exit;

            using (LogContext.PushProperty("Op", "Stock-MOVE"))
            using (LogContext.PushProperty("Barcode", barcode))
            using (LogContext.PushProperty("User", User?.Identity?.Name ?? "admin"))
            {
                try
                {
                    // Update product current state
                    p.Location = targetLoc;
                    p.CurrentHolder = null; // clear holder for depot/office

                    await _context.StockTransaction.AddAsync(new StockTransaction
                    {
                        Barcode = barcode,
                        Type = type,
                        Quantity = 1,
                        Location = targetLoc,
                        DeliveredBy = vm.DeliveredBy,
                        Note = vm.Note
                    });

                    await _context.SaveChangesAsync();
                    TempData["Success"] = _localizer["MoveSuccess"];
                    return RedirectToAction("All", "Product");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Confirm move failed for {Barcode}", barcode);
                    TempData["Error"] = _localizer["MoveError"];
                    return View("Move", vm);
                }
            }
        }

        // -------------------- HISTORY --------------------

        [HttpGet]
        public async Task<IActionResult> History(string? barcode = null)
        {
            var q = _context.StockTransaction.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(barcode))
                q = q.Where(t => t.Barcode == barcode.Trim());

            var list = await q
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            ViewBag.Barcode = barcode ?? "";
            return View(list);
        }
    }
}
