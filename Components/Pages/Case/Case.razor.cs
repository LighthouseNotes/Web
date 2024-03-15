using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Web.Components.Pages.Case;

public class CaseBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;
    [Inject] private LighthouseNotesAPIDelete LighthouseNotesAPIDelete { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    // Page variables
    protected API.Case SCase = null!;
    protected Models.Settings Settings = new();
    protected PageLoad? PageLoad;
    protected AddCaseUserForm Model = new();

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
        (API.Pagination, List<API.User>) usersWithPagination = await LighthouseNotesAPIGet.Users(1, 0);
        _users = usersWithPagination.Item2;

        // Mark page load as complete 
        PageLoad?.LoadComplete();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // After page render
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // If settings is null the get the settings
        if (Settings.Auth0UserId == null || Settings.OrganizationId == null || Settings.UserId == null ||
            Settings.S3Endpoint == null)
        {
            // Get the settings redirect url
            string? settingsRedirect = await SettingsService.CheckOrSet();
            
            // If the settings redirect url is not null then redirect 
            if (settingsRedirect != null)
            {
                NavigationManager.NavigateTo(settingsRedirect, true);
            }
            
            // Use the setting service to retrieve the settings
            Settings = await SettingsService.Get();

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

        // Clear the form fields
        Model = new AddCaseUserForm();

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