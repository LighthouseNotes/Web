using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Microsoft.AspNetCore.Mvc.Route("auth")]
public class AuthController : Controller
{
    // GET: /auth/login
    [HttpGet("login")]
    public IActionResult ChallengeLogin(string? returnUrl = "/")
    {
        // Redirect to OIDC challenge URL and use the return url to get the user back to the page they were on
        AuthenticationProperties authenticationProperties = new()
        {
            RedirectUri = returnUrl
        };
        return Challenge(authenticationProperties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    // GET: /auth/logout
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        // Remove the cookie
        SignOut(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect to OIDC to complete logout and redirect to /account/logout to show logout screen
        AuthenticationProperties authenticationProperties = new()
        {
            RedirectUri = "/account/logout"
        };

        return SignOut(authenticationProperties, OpenIdConnectDefaults.AuthenticationScheme);
    }
}