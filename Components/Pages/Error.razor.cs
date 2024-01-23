using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace Web.Components.Pages;

public class ErrorBase : ComponentBase
{
    [Parameter] public Exception? Exception { get; set; }
    [Parameter] public required HttpStatusCode StatusCode { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private TokenProvider TokenProvider { get; set; } = default!;

    private string _loginUrl = "/account/login?returnUrl=/";
    private string _currentUrl = "/";

    protected string? Title;
    protected string? Description;
    protected bool LoginExpired;

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // If status code is 404 Not Found or 403 Forbidden then return
        if (StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden) return;
        
        // Check exception type 
        switch (Exception)
        {
            case LighthouseNotesErrors.LighthouseNotesApiException exceptionDetails:
            {
                Title = exceptionDetails.Response.ReasonPhrase;
                string responseContent = await exceptionDetails.Response.Content.ReadAsStringAsync();
                Description = System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
                break;
            }
            case HttpRequestException httpRequestException:
                Title = "HTTP Error";
                Description = httpRequestException.Message;
                break;
            default:
                Title = "Unknown Error:";
                Description = Exception?.Message;
                break;
        }
        
        // If title is unauthorized check token expiry date time
        if (Title == "Unauthorized")
        {
            // Create JWT handler and read token
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken? jsonToken = handler.ReadToken(TokenProvider.AccessToken) as JwtSecurityToken;

            // Get expiration date time
            int? expiration = jsonToken?.Payload.Exp;

            // Check if the token is expired
            if (expiration.HasValue)
            {
                DateTimeOffset expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(expiration.Value);

                // If token is expired set login expired to True
                if (expirationDateTime <= DateTime.UtcNow) LoginExpired = true;
            }
        }
    }

    // On initialized 
    protected override void OnInitialized()
    {
        // Get current url as an escaped string
        _currentUrl = Uri.EscapeDataString(new Uri(NavigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

        // Create login url with redirect ulr to the current page
        _loginUrl = $"/account/login?returnUrl={_currentUrl}";
    }

    // Login link clicked
    protected void LoginClick()
    {
        NavigationManager.NavigateTo(_loginUrl, true);
    }
}