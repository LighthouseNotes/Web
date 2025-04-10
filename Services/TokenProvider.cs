// ReSharper disable UnusedAutoPropertyAccessor.Global

using Microsoft.AspNetCore.Authentication;

namespace Web.Services;

public class TokenService(IHttpContextAccessor httpContextAccessor)
{
    // Get the access token
    public string? GetAccessToken()
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        return context?.GetTokenAsync("access_token").Result;
    }
}