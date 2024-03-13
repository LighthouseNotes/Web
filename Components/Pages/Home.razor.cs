using MudBlazor;
using Web.Models.API;

namespace Web.Components.Pages;

public class HomeBase : ComponentBase
{
    [Inject] private TokenProvider TokenProvider { get; set; } = default!;

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;

    [Inject] private IJSRuntime Js { get; set; } = default!;

    [Inject] private IHostEnvironment HostEnvironment { get; set; } = default!;

    [Inject] private ISettingsService SettingsService { get; set; } = default!;

    // Page variables
    protected PageLoad? PageLoad;
    protected API.User User = null!;
    protected Web.Models.Settings Settings = new();
    protected bool DevelopmentMode;
    protected string SearchString = null!;
    protected MudDataGrid<API.Case> CasesTable = null!;

    // Class variables
    private List<API.User> _sioUsers = null!;

    // Page initialized
    protected override async Task OnInitializedAsync()
    {
        // Get SIO users from api
        (Pagination, List<API.User>) usersWithPagination = await LighthouseNotesAPIGet.Users(sio: true);
        _sioUsers = usersWithPagination.Item2;

        // Check if environment is development
        if (HostEnvironment.IsDevelopment()) DevelopmentMode = true;

        // Fetch user details 
        User = await LighthouseNotesAPIGet.User();

        // Mark page load as complete 
        PageLoad?.LoadComplete();
    }

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

    protected async Task<GridData<API.Case>> LoadGridData(GridState<API.Case> state)
    {
        // Fetch cases from API
        (Pagination, List<API.Case>?) cases;
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

    protected async Task Search()
    {
        if (CasesTable.SortDefinitions.Count == 1)
            await CasesTable.RemoveSortAsync(CasesTable.SortDefinitions.First().Value.SortBy);
        await CasesTable.ReloadServerData();
    }

    // SIO user search function - searches by given name or last name
    protected async Task<IEnumerable<API.User>> SIOUserSearchFunc(string search)
    {
        if (string.IsNullOrEmpty(search)) return _sioUsers;
        return await Task.FromResult(_sioUsers.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }

    // Case item edit committed 
    protected async Task CommittedItemChanges(API.Case item)
    {
        // Create updated case
        UpdateCase updateCase = new()
        {
            DisplayId = item.DisplayId,
            Name = item.Name,
            SIOUserId = item.SIO.Id,
            Status = item.Status
        };

        // Call api to update user
        await LighthouseNotesAPIPut.Case(item.Id, updateCase);
    }

    // Copy access token to clipboard button clicked
    protected async Task CopyAccessTokenToClipboard()
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", TokenProvider.AccessToken);
    }
}