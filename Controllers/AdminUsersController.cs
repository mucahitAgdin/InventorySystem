// Controllers/AdminUsersController.cs
using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InventorySystem.Controllers
{
    // Sadece Admin rolü erişebilsin (login şart)
    [Authorize(Roles = "Admin")]

    // Tüm aksiyonlara "sys-admin" öneki veriyoruz -> /sys-admin/...
    [Route("sys-admin")]
    public class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AdminUsersController(ApplicationDbContext db) => _db = db;

        // GET /sys-admin/users
        [HttpGet("users")]
        public async Task<IActionResult> Index()
        {
            var admins = await _db.Admins.AsNoTracking()
                                         .OrderBy(a => a.Username)
                                         .ToListAsync();
            ViewBag.Message = TempData["Message"] as string;
            ViewBag.Error = TempData["Error"] as string;
            return View(admins);
        }

        // POST /sys-admin/users/{id}/delete
        [HttpPost("users/{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var admin = await _db.Admins.FindAsync(id);
            if (admin == null) { TempData["Error"] = "Admin not found."; return RedirectToAction(nameof(Index)); }

            var totalAdmins = await _db.Admins.CountAsync();
            if (totalAdmins <= 1) { TempData["Error"] = "You cannot delete the last remaining admin."; return RedirectToAction(nameof(Index)); }

            var currentUsername = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(currentUsername) &&
                string.Equals(admin.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
            { TempData["Error"] = "You cannot delete your own admin account."; return RedirectToAction(nameof(Index)); }

            try
            {
                _db.Admins.Remove(admin);
                await _db.SaveChangesAsync();
                Log.Information("Admin deleted: {Username} by {PerformedBy}", admin.Username, currentUsername);
                TempData["Message"] = $"Admin '{admin.Username}' deleted successfully.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting admin Id={Id}", id);
                TempData["Error"] = "An unexpected error occurred while deleting the admin.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
