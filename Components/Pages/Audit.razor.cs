namespace Web.Components.Pages;

public class AuditBase : ComponentBase
{
    // Page variables
    protected PageLoad? PageLoad;
    protected Settings Settings = null!;

    // Component parameters and dependency injection
    [Inject] private LighthouseNotesAPIGet LighthouseNotesApiGet { get; set; } = null!;

    [Inject] private ISettingsService SettingsService { get; set; } = null!;

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    //  Lifecycle method called after the component has rendered - get settings
    protected override async Task OnAfterRenderAsync(bool firstRender)
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
        Settings = settingsCheckOrSetResult.Item2!;

        // Mark page load as complete
        PageLoad?.LoadComplete();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // Load data grid
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