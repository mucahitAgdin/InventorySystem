using Microsoft.AspNetCore.Mvc;
using InventorySystem.Data;
using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InventorySystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Giriş sayfası
        public IActionResult Login()
        {
            return View();
        }

        //giriş POST işlemi
        [HttpPost]
        public async Task<IActionResult> Login(Admin model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);

            if (admin == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View(model);
            }

            HttpContext.Session.SetString("IsAdmin", "true"); // ✅ Oturum başlat
            return RedirectToAction("InStockOnly", "Product");
        }

        //çıkışşşş yapıyorum.
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Admin");
        }


    }
}