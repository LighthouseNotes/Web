using System.Text.RegularExpressions;

namespace Web.Components.Layout.Main;

public partial class NavMenuBase : ComponentBase
{
    // Page variables
    protected readonly List<string> MenuUrls =
    [
        "",
        "case/create",
        "audit"
    ];

    protected string? CaseId;
    protected (API.Pagination, List<API.Case>?) Cases;
    protected string? CurrentUrl;
    protected List<API.SharedTab>? SharedTabs;
    protected List<API.Tab>? Tabs;

    // Component parameters and dependency injection
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;

    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter] public bool ContainsItems { get; set; }

    [Parameter] public EventCallback<bool> ContainsItemsChanged { get; set; }

    [CascadingParameter] public Error.ErrorLayout? Error { get; set; }

    // Component parameters and dependency injection - carry out URL checks to decide if we need to show the menu
    protected override async Task OnParametersSetAsync()
    {
        // Get current url
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");

        // If current url is not null
        if (CurrentUrl != null)
        {
            // User is at a case page that should display case menu items
            if (IsCaseUrl())
            {
                // Menu contains items so set to true
                await SetValue(true);

                // Get a list of cases for drop down
                Cases = await LighthouseNotesAPIGet.Cases();

                // Use regex to extract case id from url
                Match match = CaseRegex().Match(CurrentUrl);

                // If case id is found fetch data specific to case
                if (match.Success)
                {
                    // Store case id in variable
                    CaseId = match.Value;

                    // Fetch tabs and shared tabs
                    Tabs = await LighthouseNotesAPIGet.Tabs(CaseId);
                    SharedTabs = await LighthouseNotesAPIGet.SharedTabs(CaseId);

                    // Re-render component
                    await InvokeAsync(StateHasChanged);

                    return;
                }
            }

            // If the user is at page which should display the menu
            if (MenuUrls.Contains(CurrentUrl))
            {
                // Menu contains items so set to true
                await SetValue(true);

                return;
            }

            // Menu does not contain items so set to false
            await SetValue(false);
        }
    }

    // Set value - Updates the contains items parameter
    private async Task SetValue(bool value)
    {
        if (ContainsItems != value)
        {
            ContainsItems = value;
            await ContainsItemsChanged.InvokeAsync(value);

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // Create tab - create tab dialog and call API if user submits tab
    protected async Task CreateTab()
    {
        // Create the create tab dialog
        DialogParameters<AddTabDialog> parameters = new() { { p => p.CaseId, CaseId } };
        DialogOptions options = new() { CloseOnEscapeKey = true };
        IDialogReference dialog = await DialogService.ShowAsync<AddTabDialog>("Add tab", parameters, options);

        // Get result of dialog
        DialogResult? result = await dialog.Result;

        // If dialog was not canceled then call the API to create the tab
        if (!result!.Canceled)
        {
            Tabs = await LighthouseNotesAPIGet.Tabs(CaseId);
            await InvokeAsync(StateHasChanged);
        }
    }

    // Create shared tab - create shared tab dialog and call API if user submits tab
    protected async Task CreateSharedTab()
    {
        // Create the create shared tab dialog
        DialogParameters<AddSharedTabDialog> parameters = new() { { p => p.CaseId, CaseId } };
        DialogOptions options = new() { CloseOnEscapeKey = true };
        IDialogReference dialog =
            await DialogService.ShowAsync<AddSharedTabDialog>("Add shared tab", parameters, options);
        // Get result of dialog
        DialogResult? result = await dialog.Result;

        // If dialog was not canceled then call the API to create the tab
        if (!result!.Canceled)
        {
            SharedTabs = await LighthouseNotesAPIGet.SharedTabs(CaseId);
            await InvokeAsync(StateHasChanged);
        }
    }

    // Is Case URL - uses regex to check if the URL is a case URL
    protected bool IsCaseUrl()
    {
        // Create regex
        Regex re = CaseUrlRegex();

        // Check if there is a match
        return re.IsMatch(CurrentUrl!);
    }

    // Case changed - if the user changes case on the drop-down then navigate to it
    protected void CaseChanged(IEnumerable<string?>? caseIdToNavigateTo)
    {
        // Create regex pattern
        Regex pattern = CaseRegex();

        // Check if case id to navigate to is not null
        if (caseIdToNavigateTo != null)

            // Replace the case ID in the current url with the one the user wishes to change to
            NavigationManager.NavigateTo(
                pattern.Replace(new Uri(NavigationManager.Uri).PathAndQuery,
                    caseIdToNavigateTo.First()!, 1));
    }

    // Case ID Regex
    [GeneratedRegex("[a-zA-Z0-9]{10}")]
    private static partial Regex CaseRegex();

    // Case URL regex
    [GeneratedRegex(@"(case\/[a-zA-Z0-9]{10}\/shared)|(case\/[a-zA-Z0-9]{10})")]
    private static partial Regex CaseUrlRegex();
}