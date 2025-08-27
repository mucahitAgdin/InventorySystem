using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Controllers
{
    [AllowAnonymous] // 🔑 global Authorize varsa bile dil değişimi serbest
    public class LanguageController : Controller
    {
        [HttpGet, HttpPost] // her iki yöntem de kabul
        public IActionResult Set(string? culture, string? returnUrl)
        {
            var lang = string.IsNullOrWhiteSpace(culture) ? "tr" : culture.Trim();

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            // Önce parametre ile gelen URL'yi deneriz
            var target = CoerceLocalUrl(returnUrl)
                         // sonra Referer header (varsa)
                         ?? CoerceLocalUrl(Request.Headers["Referer"].ToString())
                         // en son güvenli bir varsayılan
                         ?? Url.Action("AllProducts", "Product") ?? "/";

            return LocalRedirect(target);
        }

        /// Yalnızca yerel (same-site) path'leri kabul eder.
        private string? CoerceLocalUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // Zaten local path ise
            if (Url.IsLocalUrl(input)) return input;

            // Mutlak URL ise ve aynı host ise path+query'yi çıkar
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                if (string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
                {
                    var path = uri.PathAndQuery;
                    if (Url.IsLocalUrl(path)) return path;
                }
            }
            return null;
        }
    }
}
