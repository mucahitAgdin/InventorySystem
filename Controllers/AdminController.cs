using Microsoft.AspNetCore.Mvc;
using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

// 🔽 eklendi
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace InventorySystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous] // 🔽 login sayfası anonim erişilebilir
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous] // 🔽 post da anonim olmalı
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Admin model)
        {
            if (!ModelState.IsValid) return View(model);

            // ⚠️ Şimdilik düz şifre – sonraki patch’te hash’e geçeceğiz
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);

            if (admin == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
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
