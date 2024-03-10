using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;


// ReSharper disable InconsistentNaming

namespace Web.Components.Pages.SIO;

public class CreateCaseBase : ComponentBase
{
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // Page variables
    protected readonly CreateCaseForm Model = new();
    protected PageLoad? PageLoad;

    // Class variables
    private List<API.User> _users = null!;
    private List<API.User> _SIOUsers = null!;

    // Create case form
    protected class CreateCaseForm
    {
        [Required]
        [MaxLength(10, ErrorMessage = "Maximum number of characters is 10 for Case ID")]
        public string CaseId { get; set; } = null!;

        [MaxLength(90, ErrorMessage = "Maximum number of characters is 90 for Case Name")]
        [Required]
        public string CaseName { get; set; } = null!;

        public IEnumerable<API.User>? Users { get; set; }

        public API.User? SIO { get; set; }
    }

    // Page initialized
    protected override async Task OnInitializedAsync()
    {
        // Get users from api
        (API.Pagination, List<API.User>) usersWithPagination  = await LighthouseNotesAPIGet.Users(1, 0);
        _users = usersWithPagination.Item2;

        // Create a list of users who has the role SIO
        _SIOUsers = _users.Where(u => u.Roles.Contains("sio")).ToList();

        // Mark page load as complete 
        PageLoad?.LoadComplete();
    }

    // User search function - searches by given name or last name
    protected async Task<IEnumerable<API.User>> UserSearchFunc(string search)
    {
        if (string.IsNullOrEmpty(search)) return _users;
        return await Task.FromResult(_users.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }

    // SIO user search function - searches by given name or last name
    protected async Task<IEnumerable<API.User>> SIOUserSearchFunc(string search)
    {
        if (string.IsNullOrEmpty(search)) return _SIOUsers;
        return await Task.FromResult(_SIOUsers.Where(x =>
            x.GivenName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            x.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }

    // On valid submit of form 
    protected async Task OnValidSubmit(EditContext context)
    {
        // Create case entity 
        API.AddCase addCase = new()
        {
            DisplayId = Model.CaseId,
            Name = Model.CaseName
        };

        // If the form includes users - create a list of the selected users ids
        if (Model.Users != null) addCase.UserIds = Model.Users.Select(u => u.Id).ToList();

        if (Model.SIO != null) addCase.SIOUserId = Model.SIO.Id;

        // Call the api to create the case
        await LighthouseNotesAPIPost.Case(addCase);

        // Notify the user of case creation
        Snackbar.Add("Case has successfully been created!", Severity.Success);

        // Navigate to home
        NavigationManager.NavigateTo("/");
    }
}