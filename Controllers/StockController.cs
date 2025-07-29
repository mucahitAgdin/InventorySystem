using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Controllers
{
    public class StockController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
