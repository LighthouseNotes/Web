using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages;

public class AuditBase : ComponentBase
{
    [Inject] private LighthouseNotesAPIGet LighthouseNotesApiGet { get; set; } = default!;

    [Inject] private ISettingsService SettingsService { get; set; } = default!;

    // Page variables
    protected Settings Settings = new();

    // Page rendered
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // If settings is null the get the settings
        if (Settings.Auth0UserId == null || Settings.OrganizationId == null || Settings.UserId == null ||
            Settings.S3Endpoint == null)
        {
            // Use the setting service to retrieve the settings
            Settings = await SettingsService.Get();

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task<GridData<API.UserAudit>> LoadGridData(GridState<API.UserAudit> state)
    {
        // Fetch cases from API
        (API.Pagination, List<API.UserAudit>) cases =
            await LighthouseNotesApiGet.UserAudit(state.Page + 1, state.PageSize);

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