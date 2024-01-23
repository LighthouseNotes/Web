using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Web.Models;

namespace Web.Components.Pages.Case;

public class ExhibitBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }
    [Parameter] public required string ExhibitId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;

    // Page variables
    protected PageLoad? PageLoad;
    protected API.Exhibit Exhibit = null!;
    protected Settings Settings = new();

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        Exhibit = await LighthouseNotesAPIGet.Exhibit(CaseId, ExhibitId);

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

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }
}