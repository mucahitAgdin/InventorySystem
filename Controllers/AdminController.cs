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
            {
                Log.Warning("Geçersiz model ile giriş denemesi.");
                return View(model);
            }

            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);

            if (admin == null)
            {
                Log.Warning("Başarısız giriş denemesi: {@Username}", model.Username);
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View(model);
            }

            Log.Information("Başarılı giriş : {@Username}", model.Username);
            return RedirectToAction("Dashboard");
        }
        public IActionResult Dashboard()
        {
            return View(); // şimdilik boş view olacak
        }
    }
}