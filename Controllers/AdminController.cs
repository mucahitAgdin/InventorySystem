using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
// 🔽 eklendi
using System.Security.Claims;

namespace InventorySystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<AdminController> _localizer;

        public AdminController(ApplicationDbContext context, IStringLocalizer<AdminController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpGet]
        [AllowAnonymous] // 🔽 login sayfası anonim erişilebilir
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Admin model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);

            if (admin == null)
            {
                // 🔽 resx anahtarı: InvalidCredentials
                ViewBag.Error = _localizer["InvalidCredentials"];
                return View(model);
            }

            // ✅ Cookie tabanlı oturum
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
        [Authorize] // 🔽 sadece oturum sahipleri çıkış yapabilsin
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
