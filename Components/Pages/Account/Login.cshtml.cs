using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

// ReSharper disable UnusedMember.Global

namespace Web.Components.Pages.Account;

public class LoginModel : PageModel
{
    public async Task OnGet([FromQuery] QueryParameters parameters)
    {
        // If model state is not valid 
        if (!ModelState.IsValid) return;

        // If return url is not set then set it to /
        if (string.IsNullOrWhiteSpace(parameters.ReturnUrl)) parameters.ReturnUrl = "/";

        // Create variable for authentication properties
        AuthenticationProperties authenticationProperties;

        // If invitation is set then redirect to /account/new-user with invitation 
        if (parameters.Invitation != null)
            authenticationProperties = new LoginAuthenticationPropertiesBuilder()
                .WithRedirectUri("/account/new-user")
                .WithInvitation(parameters.Invitation)
                .Build();

        // Else login and redirect to the specified redirect URL
        else
            authenticationProperties = new LoginAuthenticationPropertiesBuilder()
                .WithRedirectUri(parameters.ReturnUrl)
                .Build();

        // Do login 
        await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class QueryParameters
{
    [BindProperty(Name = "returnUrl", SupportsGet = true)]
    [BindRequired]
    public string? ReturnUrl { get; set; }

    [BindProperty(Name = "organization", SupportsGet = true)]
    public string? Organization { get; set; }

    [BindProperty(Name = "organization_name", SupportsGet = true)]
    public string? OrganizationName { get; set; }

    [BindProperty(Name = "invitation", SupportsGet = true)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? Invitation { get; set; }
}