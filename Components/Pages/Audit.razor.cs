using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages;

public class AuditBase : ComponentBase
{
    [Inject] private LighthouseNotesAPIGet LighthouseNotesApiGet { get; set; } = default!;

    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;

    // Page variables
    protected PageLoad? PageLoad;
    protected Settings Settings = new();
    
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
    
    protected async Task<GridData<API.UserAudit>> LoadGridData(GridState<API.UserAudit> state)
    {
        
        // Fetch cases from API
        (API.Pagination, List<API.UserAudit>) cases = await LighthouseNotesApiGet.UserAudit(state.Page + 1, state.PageSize);
        
        // Create grid data
        GridData<API.UserAudit> data = new()
        {
            Items = cases.Item2,
            TotalItems = cases.Item1.Total
        };
        
        // Return grid data
        return data;
    }
}