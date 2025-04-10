namespace Web.Components.Pages.Case;

public class CreateBase : ComponentBase
{
    // Class variables
    private API.User _loggedInUser = null!;
    private List<API.User> _users = [];

    // Page variables
    protected PageLoad? PageLoad;
    protected MudAutocomplete<API.User> CaseUsersAutoComplete = null!;
    protected CreateCaseForm Model = null!;

    // Component parameters and dependency injection
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;
    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - get users from api and prefill details
    protected override async Task OnParametersSetAsync()
    {
        // Get users from api
        (API.Pagination, List<API.User>) usersWithPagination = await LighthouseNotesAPIGet.Users(1, 0);
        _users = usersWithPagination.Item2;

        // Get authentication sate
        AuthenticationState authenticationState = await AuthenticationState.GetAuthenticationStateAsync();

        // Find the logged-in user in the list of all users
        _loggedInUser = _users.Single(u =>
            u.EmailAddress == authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value);

        // Initialize the Model with the logged-in user as the lead investigator and default user
        Model = new CreateCaseForm
        {
            LeadInvestigator = _loggedInUser,
            Users = [_loggedInUser]
        };

        // Remove the logged-in user from dropdown
        _users.Remove(_loggedInUser);

        // Mark page load as complete
        PageLoad?.LoadComplete();
    }

    // User search function - searches by given name or last name
    protected async Task<IEnumerable<API.User>> UserSearchFunc(string search, CancellationToken token)
    {
        if (string.IsNullOrEmpty(search)) return _users;
        return await Task.FromResult(_users.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)).AsEnumerable());
    }

    // After Lead investigator set - add the user to case users and remove from dropdown
    protected void LeadInvestigatorAfter()
    {
        // Add the lead investigator as a case user
        Model.Users.Add(Model.LeadInvestigator);

        // Remove user from drop down
        _users.Remove(Model.LeadInvestigator);
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
        // If the user is trying to remove the lead investigator show an error message and return
        if (chip.Value == Model.LeadInvestigator)
        {
            Snackbar.Add(
                $"Can't remove {chip.Value.DisplayName} while they are lead investigator!\nPlease change the lead investigator and try again.",
                Severity.Error);
            return;
        }

        // Remove the user from case users
        Model.Users.Remove(chip.Value!);

        // Add the user back to drop down
        _users.Add(chip.Value!);
    }

    // On valid submit of form - call the API to create the case
    protected async Task OnValidSubmit(EditContext context)
    {
        // Create case entity
        API.AddCase addCase = new()
        {
            DisplayId = Model.CaseId,
            Name = Model.CaseName,
            LeadInvestigatorEmailAddress = Model.LeadInvestigator.EmailAddress
        };

        // Update the model to only contain valid users
        Model.Users = Model.Users.OfType<API.User>().ToList();

        // Handle case users
        // Remove the lead investigator from case users as the API will automatically add them as a case user. They are only added to users model for GUI purposes
        Model.Users.Remove(Model.Users.Single(u => u.EmailAddress == Model.LeadInvestigator.EmailAddress));

        // If the form includes users - create a list of the selected users email addresses
        if (Model.Users.Count >= 1) addCase.EmailAddresses = Model.Users.Select(u => u.EmailAddress).ToList();

        // Call the api to create the case
        API.Case sCase = await LighthouseNotesAPIPost.Case(addCase);

        // Check if the user has access to the case
        if (sCase.Users.Any(u => u.EmailAddress == _loggedInUser.EmailAddress))
        {
            Snackbar.Add($"Case `{sCase.DisplayName}` has successfully been created!\nRedirecting now...",
                Severity.Success);

            // Navigate to the newly created case
            NavigationManager.NavigateTo($"/case/{sCase.DisplayName}");
            return;
        }

        Snackbar.Add(
            $"Case `{sCase.Id}` has successfully been created!\nRedirecting you home now (as you did not give yourself access to the case)...",
            Severity.Success);

        NavigationManager.NavigateTo("/");
    }

    // Create case form
    protected class CreateCaseForm
    {
        [Required]
        [MaxLength(10, ErrorMessage = "Maximum number of characters is 10 for Case ID")]
        public string CaseId { get; set; } = null!;

        [MaxLength(90, ErrorMessage = "Maximum number of characters is 90 for Case Name")]
        [Required]
        public string CaseName { get; set; } = null!;

        public required List<API.User> Users { get; set; }
        public required API.User LeadInvestigator { get; set; }
    }
}