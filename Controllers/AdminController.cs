using InventorySystem.Data;
using InventorySystem.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;   // 🔹 i18n for controller messages

using System.Security.Claims;

namespace InventorySystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<AdminController> _localizer; // 🔹 injected

        public AdminController(
            ApplicationDbContext context,
            IStringLocalizer<AdminController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [AllowAnonymous] // login page should be accessible anonymously
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Admin model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // NOTE: Plain-text password check for now.
            // Next patch: switch to hashed passwords + salt.
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);

            if (admin == null)
            {
                // i18n: use resx key "InvalidCredentials"
                ViewBag.Error = _localizer["InvalidCredentials"].Value; // FIX: ViewBag'a da string koymak tutarlılık sağlar
                return View(model);
            }

            // ✅ Cookie-based auth
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("All", "Product");
        }

        [HttpGet]
        [Authorize] // only authenticated users can sign out
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
