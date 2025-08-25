using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Controllers
{
    // NOTE: Sadece dil cookie’si set edip geri döner; admin akışı için kullanılacak.
    public class LanguageController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Set(string culture, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(culture))
                culture = "tr";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            // Güvenlik: local URL değilse ana sayfaya dön
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.Action("Login", "Admin") ?? "/";

            return LocalRedirect(returnUrl);
        }
    }
}
