namespace Web.Components.Pages.Case;

public class CaseBase : ComponentBase
{
    // Class variables
    private List<API.User> _users = null!;

    // Page variables
    protected API.Case SCase = null!;
    protected Settings Settings = null!;
    protected MudAutocomplete<API.User> CaseUsersAutoComplete = null!;
    protected AddCaseUserForm Model = new();
    protected PageLoad? PageLoad;

    // Component parameters and dependency injection
    [Parameter] public required string CaseId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = null!;
    [Inject] private LighthouseNotesAPIDelete LighthouseNotesAPIDelete { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ISettingsService SettingsService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - get case details adn all users
    protected override async Task OnParametersSetAsync()
    {
        // Get case details
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Get users from api
        (API.Pagination, List<API.User>) usersWithPagination = await LighthouseNotesAPIGet.Users(1, 0);
        _users = usersWithPagination.Item2;

        // Remove any users already added to the case from the drop-down
        SCase.Users.ForEach(user => _users.RemoveAll(u => u.EmailAddress == user.EmailAddress));
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

    // Delete user button clicked - remove user from case
    protected async Task DeleteClick(string emailAddress)
    {
        // Show an error and stop execution if trying to remove the lead investigator
        if (SCase.LeadInvestigator.EmailAddress == emailAddress)
        {
            Snackbar.Add("Can not remove the lead investigator!", Severity.Error);
            return;
        }

        // Call the API
        await LighthouseNotesAPIDelete.CaseUser(CaseId, emailAddress);

        // Show a success message
        Snackbar.Add("Successfully removed the user!", Severity.Success);

        // Update client side case user list to reflect the remove user
        SCase.Users = SCase.Users.Where(u => u.EmailAddress != emailAddress).ToList();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // On valid submit - call API to add user to the case
    protected async Task OnValidSubmit(EditContext context)
    {
        // Update the model to only contain valid users
        Model.Users = Model.Users.OfType<API.User>().ToList();

        // For each user call the API
        foreach (API.User user in Model.Users) await LighthouseNotesAPIPut.CaseUser(CaseId, user.EmailAddress);

        // Show a success message
        Snackbar.Add("Successfully added user(s) to the case!", Severity.Success);

        // Empty selection
        Model.Users = [];

        // Get updated case details
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Clear the form fields
        Model = new AddCaseUserForm();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // Export button clicked - open export page in new tab
    protected async Task ExportClick()
    {
        // Open export page in new tab
        await JS.InvokeVoidAsync("open", $"/case/{CaseId}/export", "_blank");
    }

    // User search function - search users by name, last name and display name
    protected async Task<IEnumerable<API.User>> UserSearchFunc(string search, CancellationToken token)
    {
        if (string.IsNullOrEmpty(search)) return _users;
        return await Task.FromResult(_users.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }

    // User selected - add to case users and remove from dropdown
    protected async Task UserSelected(API.User user)
    {
        // Add the user to case users
        Model.Users.Add(user);

        // Remove user from drop down
        _users.Remove(user);

        // Clear and close dropdown
        await CaseUsersAutoComplete.ClearAsync();
    }

    // User removed - chip x clicked so remove user
    protected void UserRemoved(MudChip<API.User> chip)
    {
        // Remove the user from case users
        Model.Users.Remove(chip.Value!);

        // Add the user back to drop down
        _users.Add(chip.Value!);
    }

    // Form
    protected class AddCaseUserForm
    {
        [Required] public List<API.User> Users { get; set; } = [];
    }
}