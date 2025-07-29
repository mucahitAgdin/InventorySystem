using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Models
{
    public class StockTransaction : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
