using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Web.Models.API;

namespace Web.Services;

public interface ISettingsService
{
    Task<(string?, Settings?)> CheckGetOrSet();
    Task Remove();
}

public class SettingsService(
    ProtectedLocalStorage protectedLocalStorage,
    LighthouseNotesAPIGet lighthouseNotesAPIGet,
    NavigationManager navigationManager,
    AuthenticationStateProvider authenticationStateProvider)
    : ISettingsService
{
    // Check then get or set the settings from browser storage
    public async Task<(string?, Settings?)> CheckGetOrSet()
    {
        // Get user settings from browser storage
        ProtectedBrowserStorageResult<Settings> result =
            await protectedLocalStorage.GetAsync<Settings>("settings");

        // Get authentication sate
        AuthenticationState authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();

        // If the result is successfully return null indicting that we don't need to redirect
        if (result is { Success: true, Value: not null })
        {
            // If the settings stored in the browser contain the current user's email address then we can return
            if (result.Value.EmailAddress ==
                authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value)
                return (null, result.Value);
        }

        // Get current url as an escaped string
        string currentUrl = Uri.EscapeDataString(new Uri(navigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

        // Call the api to get user's settings
        UserSettings userSettings = await lighthouseNotesAPIGet.UserSettings();

        // Set user settings in browser storage
        await protectedLocalStorage.SetAsync("settings", new Settings
        {
            EmailAddress = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!,
            TimeZone = userSettings.TimeZone,
            DateFormat = userSettings.DateFormat,
            TimeFormat = userSettings.TimeFormat,
            DateTimeFormat = userSettings.DateFormat + " " + userSettings.TimeFormat,
        });

        // Create an escaped string for the culture
        string cultureEscaped = Uri.EscapeDataString(userSettings.Locale);

        // Use the culture controller to set the culture cookie and redirect back to the current page
        return ($"Culture/Set?culture={cultureEscaped}&redirectUrl={currentUrl}", null);
    }

    // Remove setting from local storage
    public async Task Remove()
    {
        await protectedLocalStorage.DeleteAsync("settings");
    }
}