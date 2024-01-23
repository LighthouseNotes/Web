using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Web.Models;

namespace Web.Components.Pages;

public class AuditBase : ComponentBase
{
    [Inject] private LighthouseNotesAPIGet LighthouseNotesApiGet { get; set; } = default!;

    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;

    // Page variables
    protected PageLoad? PageLoad;
    protected List<API.UserAudit>? UserEvents;
    protected Settings Settings = new();

    // On page initialized 
    protected override async Task OnInitializedAsync()
    {
        // Get user events from API
        UserEvents = await LighthouseNotesApiGet.UserAudit();
    }

    // Page rendered
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Get user settings from browser storage
            ProtectedBrowserStorageResult<Settings> result =
                await ProtectedLocalStore.GetAsync<Settings>("settings");

            // If result is success and not null assign value from browser storage, if result is success and null assign default values, if result is unsuccessful assign default values
            Settings = result.Success ? result.Value ?? new Settings() : new Settings();

            // Mark page load as complete 
            PageLoad?.LoadComplete();

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }
}