using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;

namespace Web.Components.Pages;

public class ErrorBase : ComponentBase
{
    // Class variables
    private string _currentUrl = "/";
    private string _loginUrl = "/auth/login?returnUrl=/";

    // Page variables
    protected string? Title;
    protected string? Description;
    protected bool LoginExpired;

    // Component parameters and dependency injection
    [Parameter] public Exception? Exception { get; set; }
    [Parameter] public HttpStatusCode? StatusCode { get; set; }
    [Parameter] public string? UrlStatusCode { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private TokenService TokenService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - handle error messages
    protected override async Task OnParametersSetAsync()
    {
        if (UrlStatusCode == null && StatusCode == null && Exception == null)
        {
            Snackbar.Add("No error is provided!", Severity.Error);
            return;
        }

        // If a status code is provided in the url then parse it as an HTTP Status code
        if (UrlStatusCode != null) StatusCode = (HttpStatusCode)int.Parse(UrlStatusCode);

        // If status code is 404 Not Found or 403 Forbidden then return
        if (StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden) return;

        // Check exception type
        switch (Exception)
        {
            case LighthouseNotesErrors.LighthouseNotesApiException exceptionDetails:
            {
                Title = exceptionDetails.Response.ReasonPhrase;
                string responseContent = await exceptionDetails.Response.Content.ReadAsStringAsync();
                try
                {
                    Models.Error errorMessage = JsonSerializer.Deserialize<Models.Error>(responseContent)!;
                    Description = errorMessage.Detail ??
                                  System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
                }
                catch
                {
                    Description = System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
                }

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
            JwtSecurityToken? jsonToken = handler.ReadToken(TokenService.GetAccessToken()) as JwtSecurityToken;
            JwtPayload? payload = jsonToken?.Payload;

            // Get expiration date time
            long? expiration = payload?.Expiration;

            // Check if the token is expired
            if (expiration.HasValue)
            {
                DateTimeOffset expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(expiration.Value);

                // If token is expired set login expired to True
                if (expirationDateTime <= DateTime.UtcNow) LoginExpired = true;
            }
        }

        // Get current url as an escaped string
        _currentUrl = Uri.EscapeDataString(new Uri(NavigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

        // Create login url with redirect ulr to the current page
        _loginUrl = $"/auth/login?returnUrl={_currentUrl}";
    }

    // Login link clicked
    protected void LoginClick()
    {
        NavigationManager.NavigateTo(_loginUrl, true);
    }
}