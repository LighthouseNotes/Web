using System.Text.RegularExpressions;
using MudBlazor;
using Web.Models.API;

namespace Web.Components.Layout.Main;

public partial class NavMenuBase : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    [Inject] private IDialogService DialogService { get; set; } = default!;

    [Parameter] public bool ContainsItems { get; set; }

    [Parameter] public EventCallback<bool> ContainsItemsChanged { get; set; }

    [CascadingParameter] public Error.ErrorLayout? Error { get; set; }


    protected string? CurrentUrl;
    protected List<Case>? Cases;
    protected List<Tab>? Tabs;
    protected List<SharedTab>? SharedTabs;
    protected string? CaseId;

    protected readonly List<string> MenuUrls = new()
    {
        "",
        "sio/create-case",
        "organization-administrator/users",
        "organization-administrator/config"
    };


    private async Task SetValue(bool value)
    {
        if (ContainsItems != value)
        {
            ContainsItems = value;
            await ContainsItemsChanged.InvokeAsync(value);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // Get current url
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "");

        // If current url is not null
        if (CurrentUrl != null)
        {
            AuthenticationState authenticationState = await AuthState.GetAuthenticationStateAsync();

            // User is at a case page that should display case menu items
            if (authenticationState.User.IsInRole("user") && IsCaseUrl())
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

            // SIO is at a page that should display sio menu items
            if (authenticationState.User.IsInRole("sio") && MenuUrls.Contains(CurrentUrl))
            {
                // Menu contains items so set to true
                await SetValue(true);

                return;
            }

            // Organization administrator is at a page that should display organization administrator menu items
            if (authenticationState.User.IsInRole("organization-administrator") && MenuUrls.Contains(CurrentUrl))
            {
                // Menu contains items so set to true
                await SetValue(true);

                return;
            }

            // Menu does not contain items so set to false
            await SetValue(false);
        }
    }


    protected async Task CreateTab()
    {
        DialogParameters<AddTabDialog> parameters = new() { { p => p.CaseId, CaseId } };
        DialogOptions options = new() { CloseOnEscapeKey = true };
        IDialogReference? dialog = await DialogService.ShowAsync<AddTabDialog>("Add tab", parameters, options);
        DialogResult? result = await dialog.Result;

        if (!result.Canceled)
        {
            Tabs = await LighthouseNotesAPIGet.Tabs(CaseId);
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task CreateSharedTab()
    {
        DialogParameters<AddSharedTabDialog> parameters = new() { { p => p.CaseId, CaseId } };
        DialogOptions options = new() { CloseOnEscapeKey = true };
        IDialogReference? dialog =
            await DialogService.ShowAsync<AddSharedTabDialog>("Add shared tab", parameters, options);
        DialogResult? result = await dialog.Result;

        if (!result.Canceled)
        {
            SharedTabs = await LighthouseNotesAPIGet.SharedTabs(CaseId);
            await InvokeAsync(StateHasChanged);
        }
    }

    protected bool IsCaseUrl()
    {
        Regex re = CaseUrlRegex();

        return re.IsMatch(CurrentUrl!);
    }


    protected void CaseChanged(IEnumerable<string> caseIdToNavigateTo)
    {
        Regex pattern = CaseRegex();
        NavigationManager.NavigateTo(
            pattern.Replace(new Uri(NavigationManager.Uri).PathAndQuery,
                caseIdToNavigateTo.First(), 1));
    }

    [GeneratedRegex("[a-zA-Z0-9]{10}")]
    private static partial Regex CaseRegex();

    [GeneratedRegex(@"(case\/[a-zA-Z0-9]{10}\/shared)|(case\/[a-zA-Z0-9]{10})")]
    private static partial Regex CaseUrlRegex();
}