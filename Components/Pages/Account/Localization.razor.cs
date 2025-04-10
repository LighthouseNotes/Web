using System.Globalization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Web.Components.Pages.Account;

public class LocalizationBase : ComponentBase
{
    // Class variables
    private readonly List<CultureInfo> _cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();
    private readonly List<TimeZoneInfo> _timeZones = TimeZoneInfo.GetSystemTimeZones().ToList();

    // Page variables
    protected readonly LocalizationForm Model = new();
    private Settings _settings = null!;
    protected PageLoad? PageLoad;

    // Component parameters and dependency injection
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = null!;
    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;
    [Inject] private ISettingsService SettingsService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    // Lifecycle method called after the component has rendered - get settings and prefill model
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Call Check, Get or Set - to get the settings or a redirect url
            (string?, Settings?) settingsCheckOrSetResult = await SettingsService.CheckGetOrSet();

            // If a redirect url is provided then use it
            if (settingsCheckOrSetResult.Item1 != null)
            {
                NavigationManager.NavigateTo(settingsCheckOrSetResult.Item1, true);
                return;
            }

            // Set settings to the result
            _settings = settingsCheckOrSetResult.Item2!;

            // Set model from settings
            Model.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZone);
            Model.Culture = CultureInfo.CurrentCulture;
            Model.DateFormat = _settings.DateFormat;
            Model.TimeFormat = _settings.TimeFormat;

            // Mark page load as complete
            PageLoad?.LoadComplete();

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // Valid form submit - call API to update and update browser storage
    protected async Task OnValidSubmit(EditContext context)
    {
        // Create settings model
        API.UserSettings settings = new()
        {
            TimeZone = Model.TimeZone.Id,
            TimeFormat = Model.TimeFormat,
            DateFormat = Model.DateFormat,
            Locale = Model.Culture.Name
        };

        // Cal the API
        await LighthouseNotesAPIPut.UserSettings(settings);

        // Notify the user of case creation
        Snackbar.Add("Your localization settings have been updated!", Severity.Success);

        // Get authentication sate
        AuthenticationState authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        // Update user settings in browser storage
        await ProtectedLocalStore.SetAsync("settings", new Settings
        {
            EmailAddress = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!,
            TimeZone = settings.TimeZone,
            DateFormat = settings.DateFormat,
            TimeFormat = settings.TimeFormat,
            DateTimeFormat = settings.DateFormat + " " + settings.TimeFormat
        });

        // If culture has changed then use the culture controller to update the culture
        if (Model.Culture.Name != CultureInfo.CurrentCulture.Name)
        {
            // Get current url as an escaped string
            string currentUrl = Uri.EscapeDataString(new Uri(NavigationManager.Uri)
                .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

            // Crated an escaped string for the culture
            string cultureEscaped = Uri.EscapeDataString(settings.Locale);

            // Use the culture controller to set the culture cookie and redirect back to the current page
            NavigationManager.NavigateTo(
                $"Culture/Set?culture={cultureEscaped}&redirectUrl={currentUrl}",
                true);
        }

        // Update model from settings
        Model.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone);
        Model.Culture = CultureInfo.CurrentCulture;
        Model.DateFormat = settings.DateFormat;
        Model.TimeFormat = settings.TimeFormat;

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // Timezone search - searches all .NET timezones by display name
    protected Task<IEnumerable<TimeZoneInfo>> TimeZoneSearch(string value, CancellationToken token)
    {
        if (string.IsNullOrEmpty(value)) return Task.FromResult<IEnumerable<TimeZoneInfo>>(_timeZones);

        return Task.FromResult(_timeZones.Where(x =>
            x.DisplayName.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }

    // Culture search - searches all .NET cultures by english name
    protected Task<IEnumerable<CultureInfo>> CultureSearch(string value, CancellationToken token)
    {
        // If text is null or empty, show complete list
        return Task.FromResult(string.IsNullOrEmpty(value)
            ? _cultures
            : _cultures.Where(x => x.EnglishName.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }

    // Form
    protected class LocalizationForm
    {
        [Required] public TimeZoneInfo TimeZone { get; set; } = null!;

        [Required] public string DateFormat { get; set; } = null!;

        [Required] public string TimeFormat { get; set; } = null!;

        [Required] public CultureInfo Culture { get; set; } = null!;
    }
}