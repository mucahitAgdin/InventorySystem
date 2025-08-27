using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.Controllers
{
    // Sets the culture cookie, then safely redirects back.
    public class LanguageController : Controller
    {
        // Accept GET (for anchor links) and POST (for forms)
        [HttpGet]
        public IActionResult Set(string? culture, string? returnUrl)
            => SetInternal(culture, returnUrl, isPost: false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetPost(string? culture, string? returnUrl)
            => SetInternal(culture, returnUrl, isPost: true);

        // --- helper ---
        private IActionResult SetInternal(string? culture, string? returnUrl, bool isPost)
        {
            // 1) Culture fallback
            var lang = string.IsNullOrWhiteSpace(culture) ? "tr" : culture.Trim();

            // 2) Write cookie (1 year)
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            // 3) Determine a safe local return url
            var target = CoerceLocalUrl(returnUrl)
                         ?? CoerceLocalUrl(Request.Headers["Referer"])
                         ?? "/";

            return LocalRedirect(target);
        }

        /// <summary>
        /// If the input is a local path already, returns it.
        /// If it's an absolute URL on the same host, returns PathAndQuery.
        /// Otherwise returns null (caller should fallback).
        /// </summary>
        private string? CoerceLocalUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Already a local path like "/Stock/History?barcode=..."
            if (Url.IsLocalUrl(input))
                return input;

            // Absolute URL? Try to convert to local path if same host
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                // Compare only host; scheme/port differences are fine in most intranet setups
                if (string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
                {
                    var path = uri.PathAndQuery;
                    if (Url.IsLocalUrl(path))
                        return path;
                }
            }
            return null;
        }
    }
}
