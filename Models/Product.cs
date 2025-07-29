using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Models
{
    public class Product : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
