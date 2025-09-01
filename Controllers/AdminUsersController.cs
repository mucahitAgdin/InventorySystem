// Controllers/AdminUsersController.cs
using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InventorySystem.Controllers
{
    [Authorize(Roles = "Admin")]      // only after admin login
    [Route("sys-admin")]              // hidden prefix -> /sys-admin/...
    public class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext _db;

        // ⚠️ Change this to your real default admin username in DB
        private const string PROTECTED_ADMIN_USERNAME = "admin";

        public AdminUsersController(ApplicationDbContext db) => _db = db;

        // GET /sys-admin/users  -> returns a View (list)
        [HttpGet("users")]
        public async Task<IActionResult> Index()
        {
            var admins = await _db.Admins
                .AsNoTracking()
                .OrderBy(a => a.Username)
                .ToListAsync();

            ViewBag.Message = TempData["Message"] as string; // strings only
            ViewBag.Error = TempData["Error"] as string;

            return View(admins);
        }

        // POST /sys-admin/user/add  (modal form buraya post eder)
        [HttpPost("user/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFromForm([FromForm] string? username, [FromForm] string? password, [FromForm] string? confirmPassword)
        {
            username = (username ?? string.Empty).Trim();
            password = (password ?? string.Empty).Trim();
            confirmPassword = (confirmPassword ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Username and password are required.";
                return RedirectToAction(nameof(Index));
            }
            if (username.Length > 50)
            {
                TempData["Error"] = "Username can be at most 50 characters.";
                return RedirectToAction(nameof(Index));
            }
            if (password.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return RedirectToAction(nameof(Index));
            }
            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction(nameof(Index));
            }

            var exists = await _db.Admins.AnyAsync(a => a.Username == username);
            if (exists)
            {
                TempData["Error"] = $"Username '{username}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _db.Admins.AddAsync(new Admin { Username = username, Password = password });
                await _db.SaveChangesAsync();

                Serilog.Log.Information("Admin created: {Username} by {PerformedBy}", username, User?.Identity?.Name);
                TempData["Message"] = $"Admin '{username}' created successfully.";
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error creating admin {Username}", username);
                TempData["Error"] = "Unexpected error occurred while creating the admin.";
            }

            return RedirectToAction(nameof(Index));
        }


        // GET /sys-admin/user/add?u=alice&p=Secret123
        // Quick add via URL (no view). You asked to add via address bar.
        [HttpGet("user/add")]
        public async Task<IActionResult> Add([FromQuery] string? u, [FromQuery] string? p)
        {
            var username = (u ?? "").Trim();
            var password = (p ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return BadRequest("Parameters 'u' (username) and 'p' (password) are required.");

            if (username.Length > 50)
                return BadRequest("Username can be at most 50 characters.");

            var exists = await _db.Admins.AnyAsync(a => a.Username == username);
            if (exists)
                return Conflict($"Username '{username}' already exists.");

            try
            {
                await _db.Admins.AddAsync(new Admin { Username = username, Password = password });
                await _db.SaveChangesAsync();

                Log.Information("Admin created: {Username} by {PerformedBy}", username, User?.Identity?.Name);
                return Ok($"Created: admin '{username}'.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Create admin failed for {Username}", username);
                return StatusCode(500, "Unexpected error occurred while creating the admin.");
            }
        }

        // POST /sys-admin/users/{id}/delete  -> used by the View form
        [HttpPost("users/{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFromView([FromRoute] int id)
        {
            var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin is null)
            {
                TempData["Error"] = "Admin not found.";
                return RedirectToAction(nameof(Index));
            }

            // cannot delete protected default admin
            if (string.Equals(admin.Username, PROTECTED_ADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = $"The protected admin '{PROTECTED_ADMIN_USERNAME}' cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            // cannot delete the last remaining admin
            var total = await _db.Admins.CountAsync();
            if (total <= 1)
            {
                TempData["Error"] = "You cannot delete the last remaining admin.";
                return RedirectToAction(nameof(Index));
            }

            // cannot delete your own account
            var current = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(current) &&
                string.Equals(current, admin.Username, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You cannot delete your own admin account.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _db.Admins.Remove(admin);
                await _db.SaveChangesAsync();

                Log.Information("Admin deleted: {Username} by {PerformedBy}", admin.Username, current);
                TempData["Message"] = $"Admin '{admin.Username}' deleted successfully.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Delete admin failed for Id={Id}", admin.Id);
                TempData["Error"] = "Unexpected error occurred while deleting the admin.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Optional: allow delete by URL too -> /sys-admin/user/delete?u=alice or ?id=5
        [HttpGet("user/delete")]
        public async Task<IActionResult> DeleteByUrl([FromQuery] int? id, [FromQuery] string? u)
        {
            Admin? admin = null;
            if (id is int i)
                admin = await _db.Admins.FirstOrDefaultAsync(a => a.Id == i);
            else if (!string.IsNullOrWhiteSpace(u))
                admin = await _db.Admins.FirstOrDefaultAsync(a => a.Username == u!.Trim());

            if (admin is null) return NotFound("Admin not found.");

            if (string.Equals(admin.Username, PROTECTED_ADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
                return Forbid($"The protected admin '{PROTECTED_ADMIN_USERNAME}' cannot be deleted.");

            var total = await _db.Admins.CountAsync();
            if (total <= 1) return BadRequest("You cannot delete the last remaining admin.");

            var current = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(current) &&
                string.Equals(current, admin.Username, StringComparison.OrdinalIgnoreCase))
                return BadRequest("You cannot delete your own admin account.");

            try
            {
                _db.Admins.Remove(admin);
                await _db.SaveChangesAsync();

                Log.Information("Admin deleted: {Username} by {PerformedBy}", admin.Username, current);
                return Ok($"Deleted: admin '{admin.Username}'.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Delete admin by URL failed for Id={Id}", admin.Id);
                return StatusCode(500, "Unexpected error occurred while deleting the admin.");
            }
        }
    }
}
