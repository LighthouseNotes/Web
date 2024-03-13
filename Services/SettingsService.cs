using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Web.Models.API;

namespace Web.Services;

public interface ISettingsService
{
    Task<Models.Settings> Get();
    Task Set();
}

public class SettingsService : ISettingsService
{
    private ProtectedLocalStorage _protectedLocalStore;
    private LighthouseNotesAPIGet _lighthouseNotesAPIGet;
    private NavigationManager _navigationManager;
    private AuthenticationStateProvider _authenticationState;

    public SettingsService(ProtectedLocalStorage protectedLocalStorage, LighthouseNotesAPIGet lighthouseNotesAPIGet,
        NavigationManager navigationManager, AuthenticationStateProvider authenticationState)
    {
        _protectedLocalStore = protectedLocalStorage;
        _lighthouseNotesAPIGet = lighthouseNotesAPIGet;
        _navigationManager = navigationManager;
        _authenticationState = authenticationState;
    }

    public async Task<Models.Settings> Get()
    {
        // Get user settings from browser storage
        ProtectedBrowserStorageResult<Models.Settings> result =
            await _protectedLocalStore.GetAsync<Models.Settings>("settings");

        Console.WriteLine(result.Success);
        while (result.Success == false || result.Value == null)
        {
            // Set the user settings
            await Set();

            // Get user settings from browser storage
            result = await _protectedLocalStore.GetAsync<Models.Settings>("settings");
        }

        // Get authentication state 
        AuthenticationState authenticationState = await _authenticationState.GetAuthenticationStateAsync();

        // Get organization id from claim
        string? organizationId = authenticationState.User.Claims.FirstOrDefault(c => c.Type == "org_id")?.Value;
        string? auth0UserId = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (organizationId == null || auth0UserId == null)
        {
            // Get current url as an escaped string
            string currentUrl = Uri.EscapeDataString(new Uri(_navigationManager.Uri)
                .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

            // Create login url with redirect ulr to the current page
            string loginUrl = $"/account/login?returnUrl={currentUrl}";

            // Navigate to the login URL
            _navigationManager.NavigateTo(loginUrl, true);
        }

        while (result.Value == null || result.Value.Auth0UserId != auth0UserId ||
               result.Value.OrganizationId != organizationId)
        {
            // Set the user settings
            await Set();

            // Get user settings from browser storage
            result = await _protectedLocalStore.GetAsync<Models.Settings>("settings");
        }

        Console.WriteLine(result.Value);
        return result.Value;
    }

    public async Task Set()
    {
        // Call the api to get user's settings
        Settings? settings = await _lighthouseNotesAPIGet.UserSettings();

        Console.WriteLine(settings);
        // If settings is not null then set user's settings
        if (settings != null)
        {
            // Set user settings in browser storage
            await _protectedLocalStore.SetAsync("settings", new Models.Settings
            {
                Auth0UserId = settings.Auth0UserId,
                OrganizationId = settings.OrganizationId,
                UserId = settings.UserId,
                TimeZone = settings.TimeZone,
                DateFormat = settings.DateFormat,
                TimeFormat = settings.TimeFormat,
                DateTimeFormat = settings.DateFormat + " " + settings.TimeFormat,
                S3Endpoint = settings.S3Endpoint
            });

            // Create an escaped string for the culture
            string cultureEscaped = Uri.EscapeDataString(settings.Locale);

            // Get current url as an escaped string
            string currentUrl = Uri.EscapeDataString(new Uri(_navigationManager.Uri)
                .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

            // Use the culture controller to set the culture cookie and redirect back to the current page
            _navigationManager.NavigateTo(
                $"Culture/Set?culture={cultureEscaped}&redirectUrl={currentUrl}",
                true);
        }
    }

    public async Task Remove()
    {
        await _protectedLocalStore.DeleteAsync("settings");
    }
}