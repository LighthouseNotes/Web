using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Web.Models;

namespace Web.Components.Pages;

public class HomeBase : ComponentBase
{
    [Inject] private TokenProvider TokenProvider { get; set; } = default!;

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;

    [Inject] private IJSRuntime Js { get; set; } = default!;

    [Inject] private IHostEnvironment HostEnvironment { get; set; } = default!;

    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;

    // Page variables
    protected PageLoad? PageLoad;
    protected API.User User = null!;
    protected List<API.Case>? Cases;
    protected Settings Settings = new();
    protected bool DevelopmentMode;
    protected string SearchString = null!;

    // Class variables
    private List<API.User> _users = null!;
    private List<API.User> _sioUsers = null!;

    // Page initialized
    protected override async Task OnInitializedAsync()
    {
        // Fetch all cases the user has access to 
        Cases = await LighthouseNotesAPIGet.Cases();

        // Get users from api
        _users = await LighthouseNotesAPIGet.Users();

        // Create a list of users who has the role SIO
        _sioUsers = _users.Where(u => u.Roles.Contains("sio")).ToList();

        // Check if environment is development
        if (HostEnvironment.IsDevelopment()) DevelopmentMode = true;

        // Re-render component
        await InvokeAsync(StateHasChanged);
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

            // Fetch user details 
            User = await LighthouseNotesAPIGet.User();

            // Mark page load as complete 
            PageLoad?.LoadComplete();

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // SIO user search function - searches by given name or last name
    protected async Task<IEnumerable<API.User>> SIOUserSearchFunc(string search)
    {
        if (string.IsNullOrEmpty(search)) return _sioUsers;
        return await Task.FromResult(_sioUsers.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }

    // Case filter 
    protected Func<API.Case, bool> QuickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(SearchString))
            return true;

        if (x.DisplayName.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            return true;

        if (x.DisplayId.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            return true;

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (x.SIO.DisplayName.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    };

    // Case item edit committed 
    protected async Task CommittedItemChanges(API.Case item)
    {
        // Create updated case
        API.UpdateCase updateCase = new()
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