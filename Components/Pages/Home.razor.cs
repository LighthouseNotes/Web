namespace Web.Components.Pages;

public class HomeBase : ComponentBase
{
    // Class variables
    private List<API.User> _users = null!;

    // Page variables
    protected PageLoad? PageLoad;
    protected string SearchString = null!;
    protected Settings Settings = null!;
    protected API.User User = null!;
    protected MudDataGrid<API.Case> CasesTable = null!;
    protected bool DevelopmentMode;

    // Component parameters and dependency injection
    [Inject] private TokenService TokenService { get; set; } = null!;

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;

    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = null!;

    [Inject] private IJSRuntime Js { get; set; } = null!;

    [Inject] private IHostEnvironment HostEnvironment { get; set; } = null!;

    [Inject] private ISettingsService SettingsService { get; set; } = null!;

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    // Page initialized - get user details
    protected override async Task OnParametersSetAsync()
    {
        // Check if environment is development
        if (HostEnvironment.IsDevelopment()) DevelopmentMode = true;

        // Fetch user details
        User = await LighthouseNotesAPIGet.User();

        // Get users from api
        (API.Pagination, List<API.User>) usersWithPagination = await LighthouseNotesAPIGet.Users(1, 0);
        _users = usersWithPagination.Item2;

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

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

    // Load cases data grid and handle sorting
    protected async Task<GridData<API.Case>> LoadGridData(GridState<API.Case> state)
    {
        // Fetch cases from API
        (API.Pagination, List<API.Case>?) cases;
        if (!string.IsNullOrWhiteSpace(SearchString))
        {
            // Fetch cases from API
            cases = await LighthouseNotesAPIGet.Cases(state.Page + 1, state.PageSize, search: SearchString);
        }
        // If sort definition is set then set sort string
        else if (state.SortDefinitions.Count == 1)
        {
            // if descending is true then column-name desc else column-name asc
            string sortString = state.SortDefinitions.First().Descending
                ? $"{state.SortDefinitions.First().SortBy} desc"
                : $"{state.SortDefinitions.First().SortBy} asc";

            // Fetch cases from API
            cases = await LighthouseNotesAPIGet.Cases(state.Page + 1, state.PageSize, sortString);
        }
        else
        {
            cases = await LighthouseNotesAPIGet.Cases(state.Page + 1, state.PageSize);
        }

        // Create grid data
        GridData<API.Case> data = new()
        {
            Items = cases.Item2!,
            TotalItems = cases.Item1.Total
        };

        // Return grid data
        return data;
    }

    // Search cases
    protected async Task Search()
    {
        // If sort is set remove it because we can't sort and seach
        if (CasesTable.SortDefinitions.Count == 1)
            await CasesTable.RemoveSortAsync(CasesTable.SortDefinitions.First().Value.SortBy);
        await CasesTable.ReloadServerData();
    }

    // LeadInvestigator user search function - searches by given name or last name
    protected async Task<IEnumerable<API.User>> UserSearchFunc(string search, CancellationToken token)
    {
        if (string.IsNullOrEmpty(search)) return _users;
        return await Task.FromResult(_users.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }

    // Case item edit committed - call API to update case
    protected async Task CommittedItemChanges(API.Case item)
    {
        // Create updated case
        API.UpdateCase updateCase = new()
        {
            DisplayId = item.DisplayId,
            Name = item.Name,
            LeadInvestigatorEmailAddress = item.LeadInvestigator.EmailAddress,
            Status = item.Status
        };

        // Call api to update case
        await LighthouseNotesAPIPut.Case(item.Id, updateCase);
    }

    // Copy access token to clipboard button clicked
    protected async Task CopyAccessTokenToClipboard()
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", TokenService.GetAccessToken());
    }
}