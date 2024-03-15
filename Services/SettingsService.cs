using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Web.Models.API;

namespace Web.Services;

public interface ISettingsService
{
    Task<string?> CheckOrSet();
    Task<Models.Settings> Get();
    Task Remove();
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

    public async Task<string?> CheckOrSet()
    {
        // Get user settings from browser storage
        ProtectedBrowserStorageResult<Models.Settings> result =
            await _protectedLocalStore.GetAsync<Models.Settings>("settings");

        // Get current url as an escaped string
        string currentUrl = Uri.EscapeDataString(new Uri(_navigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

        // If result is not success or value is null then set the settings in the browser storage 
        if (result.Success == false || result.Value == null)
        {
            // Call the api to get user's settings
            Settings settings = await _lighthouseNotesAPIGet.UserSettings();
            
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

            // Use the culture controller to set the culture cookie and redirect back to the current page
            return $"Culture/Set?culture={cultureEscaped}&redirectUrl={currentUrl}";
            
        }

        // Get authentication state 
        AuthenticationState authenticationState = await _authenticationState.GetAuthenticationStateAsync();

        // Get organization id from claim
        string? organizationId = authenticationState.User.Claims.FirstOrDefault(c => c.Type == "org_id")?.Value;
        string? auth0UserId = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        
        // If organizationId or auth0UserId are null from token
        if (organizationId == null || auth0UserId == null)
        {
            // Create login url with redirect ulr to the current page
            return $"/account/login?returnUrl={currentUrl}";
        }

        // If result auth0UserId and OrganizationId is not equal to the auth0UserId and OrganizationId in the token fetch settings again as we probably have an different users settings in browser storage.
        if (result.Value.Auth0UserId != auth0UserId && result.Value.OrganizationId != organizationId)
        {
                // Call the api to get user's settings
                Settings settings = await _lighthouseNotesAPIGet.UserSettings();
                
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

                // Use the culture controller to set the culture cookie and redirect back to the current page
                return $"Culture/Set?culture={cultureEscaped}&redirectUrl={currentUrl}";
        }
        
        // Return null as all checks completed without needing to redirect 
        return null;
    }

    public async Task<Models.Settings> Get()
    {
        // Get user settings from browser storage
        ProtectedBrowserStorageResult<Models.Settings> result =
            await _protectedLocalStore.GetAsync<Models.Settings>("settings");
        
        // Return result value which should never be null as CheckOrSet should always be used first
        return result.Value!;
    }
    
    public async Task Remove()
    {
        await _protectedLocalStore.DeleteAsync("settings");
    }
}