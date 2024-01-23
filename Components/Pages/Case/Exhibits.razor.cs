using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages.Case;

public class ExhibitsBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = default!;
    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // Page variables
    protected PageLoad? PageLoad;
    protected API.Case SCase = null!;
    protected List<API.Exhibit>? Exhibits;
    protected readonly AddExhibit Model = new();
    protected Settings Settings = new();

    // Class variables
    private readonly ReadOnlyCollection<TimeZoneInfo> _timeZones = TimeZoneInfo.GetSystemTimeZones();

    // Add exhibit form
    protected class AddExhibit
    {
        [Required] public string Reference { get; set; } = null!;

        [Required] public string Description { get; set; } = null!;

        [Required] public DateTime? DateSeizedProduced { get; set; }

        [Required] public TimeSpan? TimeSeizedProduced { get; set; }

        [Required] public TimeZoneInfo TimeZone { get; set; } = null!;

        [Required] public string WhereSeizedProduced { get; set; } = null!;

        [Required] public string SeizedBy { get; set; } = null!;
    }

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // Get case, exhibits and users from API
        SCase = await LighthouseNotesAPIGet.Case(CaseId);
        Exhibits = await LighthouseNotesAPIGet.Exhibits(CaseId);

        // Mark page load as complete 
        PageLoad?.LoadComplete();
    }

    // After page render
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Get user settings from browser storage
            ProtectedBrowserStorageResult<Settings> result =
                await ProtectedLocalStore.GetAsync<Settings>("settings");

            // If result is success and not null assign value from browser storage, if result is success and null assign default values, if result is unsuccessful assign default values
            Settings = result.Success ? result.Value ?? new Settings() : new Settings();

            Model.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(Settings.TimeZone);

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // On valid form submission
    protected async Task OnValidSubmit(EditContext context)
    {
        // Get user timezone 
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(Model.TimeZone.Id);

        // Parse date time from form
        DateTime localDateTimeSeizedProduced = DateTime.Parse(
            $"{DateOnly.FromDateTime(Model.DateSeizedProduced!.Value)} {Model.TimeSeizedProduced!.Value.ToString()}");

        // Convert date time from local date time to UTC
        DateTime dateTimeSeizedProduced =
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(localDateTimeSeizedProduced, timeZone.Id, TimeZoneInfo.Utc.Id);

        // Create exhibit
        API.AddExhibit addExhibit = new()
        {
            Reference = Model.Reference,
            Description = Model.Description,
            DateTimeSeizedProduced = dateTimeSeizedProduced,
            WhereSeizedProduced = Model.WhereSeizedProduced,
            SeizedBy = Model.SeizedBy
        };

        // Call api to create exhibit and get result
        API.Exhibit newExhibit = await LighthouseNotesAPIPost.Exhibit(CaseId, addExhibit);

        // If exhibits list is null create a new list with the exhibit just created
        if (Exhibits == null)
            Exhibits = new List<API.Exhibit> { newExhibit };
        // Else add the exhibit to the list
        else
            Exhibits.Add(newExhibit);

        // Re-render component
        await InvokeAsync(StateHasChanged);

        Snackbar.Add("Successfully created the exhibit!", Severity.Success);
    }

    // Time zone search 
    protected Task<IEnumerable<TimeZoneInfo>> TimeZoneSearch(string value)
    {
        // If text is null or empty, show complete list else return match for timezone 
        return Task.FromResult(string.IsNullOrEmpty(value) ? _timeZones : _timeZones.Where(x => x.DisplayName.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }
}