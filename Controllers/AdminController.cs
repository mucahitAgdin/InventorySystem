using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
