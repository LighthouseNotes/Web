using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Web.Models;

namespace Web.Components.Pages.Case;

public class CaseBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;
    [Inject] private LighthouseNotesAPIDelete LighthouseNotesAPIDelete { get; set; } = default!;
    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // Page variables
    protected API.Case SCase = null!;
    protected Settings Settings = new();
    protected PageLoad? PageLoad;
    protected readonly AddCaseUserForm Model = new();

    // Class variables
    private List<API.User> _users = null!;

    protected class AddCaseUserForm
    {
        [Required] public IEnumerable<API.User> Users { get; set; } = null!;
    }

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // Get case details
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Get users from api
        _users = await LighthouseNotesAPIGet.Users();

        // Mark page load as complete 
        PageLoad?.LoadComplete();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // After page render
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Get user settings from browser storage
            ProtectedBrowserStorageResult<Settings> result =
                await ProtectedLocalStore.GetAsync<Settings>("settings");

            // If result is success and not null assign value from browser storage, if result is success and null assign default values, if result is unsuccessful assign default values
            Settings = result.Success ? result.Value ?? new Settings() : new Settings();

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // Delete user button clicked
    protected async Task DeleteClick(string userId)
    {
        // Call the API
        await LighthouseNotesAPIDelete.CaseUser(CaseId, userId);

        // Show a success message
        Snackbar.Add("Successfully removed the user!", Severity.Success);

        // Update client side case user list to reflect the remove user 
        SCase.Users = SCase.Users.Where(u => u.Id != userId).ToList();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // On valid submit
    protected async Task OnValidSubmit(EditContext context)
    {
        // For each user call the API
        foreach (API.User user in Model.Users) await LighthouseNotesAPIPut.CaseUser(CaseId, user.Id);

        // Show a success message
        Snackbar.Add("Successfully added user(s) to the case!", Severity.Success);

        // Empty selection
        Model.Users = new List<API.User>();

        // Get updated case details
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // Export button clicked
    protected async Task ExportClick()
    {
        // Call api export and get S3 presigned Url 
        string url = await LighthouseNotesAPIGet.Export(CaseId);

        // Open url in new tab
        await JS.InvokeVoidAsync("open", url, "_blank");
    }

    // User search function
    protected async Task<IEnumerable<API.User>> UserSearchFunc(string search)
    {
        if (string.IsNullOrEmpty(search)) return _users;
        return await Task.FromResult(_users.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }
}