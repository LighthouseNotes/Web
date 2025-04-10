namespace Web.Components.Pages.Case;

public class ExhibitBase : ComponentBase
{
    // Page variables
    protected PageLoad? PageLoad;
    protected Settings Settings = null!;
    protected API.Exhibit Exhibit = null!;

    // Component parameters and dependency injection
    [Parameter] public required string CaseId { get; set; }
    [Parameter] public required string ExhibitId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;
    [Inject] private ISettingsService SettingsService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - get exhibit details
    protected override async Task OnParametersSetAsync()
    {
        Exhibit = await LighthouseNotesAPIGet.Exhibit(CaseId, ExhibitId);

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    //  Lifecycle method called after the component has rendered - get settings
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
            Settings = settingsCheckOrSetResult.Item2!;

            // Mark page load as complete
            PageLoad?.LoadComplete();

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }
}