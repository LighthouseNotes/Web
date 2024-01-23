using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class CultureController : Controller
{
    // GET: /Culture/Set
    public IActionResult Set(string culture, string redirectUrl)
    {
        // Set culture cookie
        HttpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture, culture)));

        // Redirect to the specified redirect URL
        return LocalRedirect(redirectUrl);
    }
}