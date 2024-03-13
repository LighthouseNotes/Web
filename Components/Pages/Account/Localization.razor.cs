using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages.Account;

public class LocalizationBase : ComponentBase
{
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;
    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;


    // Page variables 
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    protected LocalizationForm Model = new();
    protected PageLoad? PageLoad;

    // Class variables
    private readonly ReadOnlyCollection<TimeZoneInfo> _timeZones = TimeZoneInfo.GetSystemTimeZones();
    private readonly List<CultureInfo> _cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();
    private Settings _settings = new();

    protected class LocalizationForm
    {
        [Required] public TimeZoneInfo TimeZone { get; set; } = null!;

        [Required] public string DateFormat { get; set; } = null!;

        [Required] public string TimeFormat { get; set; } = null!;

        [Required] public CultureInfo Culture { get; set; } = null!;
    }


    // Page rendered
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // If settings is null the get the settings
        if (_settings.Auth0UserId == null || _settings.OrganizationId == null || _settings.UserId == null ||
            _settings.S3Endpoint == null)
        {
            // Use the setting service to retrieve the settings
            _settings = await SettingsService.Get();

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


    protected async Task OnValidSubmit(EditContext context)
    {
        API.UserSettings settings = new()
        {
            TimeZone = Model.TimeZone.Id,
            TimeFormat = Model.TimeFormat,
            DateFormat = Model.DateFormat,
            Locale = Model.Culture.Name
        };

        await LighthouseNotesAPIPut.UserSettings(settings);

        // Notify the user of case creation
        Snackbar.Add("Your localization settings have been updated!", Severity.Success);

        // Update user settings in browser storage
        await ProtectedLocalStore.SetAsync("settings", new Settings
        {
            TimeZone = settings.TimeZone,
            DateFormat = settings.DateFormat,
            TimeFormat = settings.TimeFormat,
            DateTimeFormat = settings.DateFormat + " " + settings.TimeFormat
        });


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
    }


    protected Task<IEnumerable<TimeZoneInfo>> TimeZoneSearch(string value)
    {
        // If text is null or empty, show complete list
        return Task.FromResult(string.IsNullOrEmpty(value)
            ? _timeZones
            : _timeZones.Where(x => x.DisplayName.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }

    protected Task<IEnumerable<CultureInfo>> CultureSearch(string value)
    {
        // If text is null or empty, show complete list
        return Task.FromResult(string.IsNullOrEmpty(value)
            ? _cultures
            : _cultures.Where(x => x.EnglishName.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }
}