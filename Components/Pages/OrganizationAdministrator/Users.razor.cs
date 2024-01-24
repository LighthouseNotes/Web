using System.ComponentModel.DataAnnotations;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Web.Components.Dialogs;

namespace Web.Components.Pages.OrganizationAdministrator;

public class UsersBase : ComponentBase
{
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;
    [Inject] private LighthouseNotesAPIDelete LighthouseNotesAPIDelete { get; set; } = default!;
    [Inject] private IDialogService Dialog { get; set; } = default!;
    [Inject] private IManagementApiClient AuthOManagementClient { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;
    
    // Page variables
    protected PageLoad? PageLoad;
    protected string RoleSelectValue = "Nothing selected";
    protected List<API.User> Users = null!;
    protected string? SearchString;
    protected InviteUserForm Model = new();
    
    private Models.Settings _settings = new();

    protected class InviteUserForm
    {
        [Required(AllowEmptyStrings = false)] public string EmailAddress { get; set; } = null!;

        public IEnumerable<string> Roles { get; set; } = new List<string> { "user" };
    }
    
    // Page rendered
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Get user settings from browser storage
            ProtectedBrowserStorageResult<Models.Settings> result =
                await ProtectedLocalStore.GetAsync<Models.Settings>("settings");

            // If result is success and not null assign value from browser storage, if result is success and null assign default values, if result is unsuccessful assign default values
            _settings = result.Success ? result.Value ?? new Models.Settings() : new Models.Settings();
            
        }
    }

    // Page initialized
    protected override async Task OnInitializedAsync()
    {
        // Fetch all cases the user has access to 
        Users = await LighthouseNotesAPIGet.Users();

        // Mark page loaded
        PageLoad?.LoadComplete();
    }

    // User filter 
    protected Func<API.User, bool> QuickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(SearchString))
            return true;

        if (x.DisplayName.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            return true;

        if (x.GivenName.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            return true;

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (x.LastName.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    };

    // Edit user change committed 
    protected async Task CommittedItemChanges(API.User item)
    {
        // Create updated user
        API.UpdateUser updateUser = new()
        {
            DisplayName = item.DisplayName,
            EmailAddress = item.EmailAddress,
            GivenName = item.GivenName,
            JobTitle = item.JobTitle,
            LastName = item.LastName,
            Roles = item.Roles.ToList()
        };

        // Call API to update user
        await LighthouseNotesAPIPut.User(updateUser, item.Id);

        // Update auth0
        // Get authentication state 
        AuthenticationState authenticationState = await AuthenticationState.GetAuthenticationStateAsync();

        // Get organization id from claim
        string? organizationId = authenticationState.User.Claims.FirstOrDefault(c => c.Type == "org_id")?.Value;

        // Get users current roles
        IPagedList<Role>? userRoles =
            await AuthOManagementClient.Organizations.GetAllMemberRolesAsync(organizationId, item.Id,
                new PaginationInfo());
        List<string> userCurrentRoles = userRoles.Select(x => x.Id).ToList();

        // Remove users current roles
        await AuthOManagementClient.Organizations.DeleteMemberRolesAsync(organizationId, item.Id,
            new OrganizationDeleteMemberRolesRequest
            {
                Roles = userCurrentRoles.ToArray()
            });

        // Create a list of the users new roles with role ids 
        List<string> newUserRoles = new();
        foreach (string role in item.Roles)
            switch (role)
            {
                case "user":
                    newUserRoles.Add(Configuration["Auth0:Roles:user"] ??
                                     throw new InvalidOperationException("Auth0:Roles:user not found in appsettings.json!"));
                    break;
                case "sio":
                    newUserRoles.Add(Configuration["Auth0:Roles:sio"] ??
                                     throw new InvalidOperationException("Auth0:Roles:sio not found in appsettings.json!"));
                    break;
                case "organization-administrator":
                    newUserRoles.Add(Configuration["Auth0:Roles:organization-administrator"] ??
                                     throw new InvalidOperationException(
                                         "Auth0:Roles:organization-administrator not found in appsettings.json!"));
                    break;
            }

        // Assign the user the new roles
        await AuthOManagementClient.Organizations.AddMemberRolesAsync(organizationId, item.Id,
            new OrganizationAddMemberRolesRequest
            {
                Roles = newUserRoles.ToArray()
            });

        // Notify the user
        Snackbar.Add($"Successfully updated `{item.DisplayName}`", Severity.Success);
    }

    // Delete user clicked
    protected async Task DeleteClick(string userId, string displayName)
    {
        // Conformation dialog
        DialogParameters<DeleteUserConfirmation> parameters = new()
        {
            { p => p.DisplayName, displayName }
        };
        DialogOptions options = new() { CloseOnEscapeKey = true };
        IDialogReference? dialog =
            await Dialog.ShowAsync<DeleteUserConfirmation>("Delete user confirmation", parameters, options);

        DialogResult? result = await dialog.Result;

        // If delete is confirmed
        if (!result.Canceled)
        {
            // Delete the user
            await LighthouseNotesAPIDelete.User(userId);

            // Remove deleted user from users list
            Users.RemoveAll(u => u.Id == userId);

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }

        // Notify the user
        Snackbar.Add("User deleted!", Severity.Success);
    }

    // On user invite form valid submission
    protected async Task OnValidSubmit(EditContext context)
    {
        // Get authentication state 
        AuthenticationState authenticationState = await AuthenticationState.GetAuthenticationStateAsync();

        // Get organization id from JWT
        string? organizationId = authenticationState.User.Claims.FirstOrDefault(c => c.Type == "org_id")?.Value;
        
        // Create a base list of roles
        List<string> roles = new()
        {
            Configuration["Auth0:Roles:user"] ??
            throw new InvalidOperationException("Auth0:Roles:user not found in appsettings.json!")
        };

        // If form contains the role SIO add the role ID
        if (Model.Roles.Contains("sio"))
            roles.Add(Configuration["Auth0:Roles:sio"] ??
                      throw new InvalidOperationException("Auth0:Roles:sio not found in appsettings.json!"));

        // If the form contains the role organization administrator add the role ID
        if (Model.Roles.Contains("organization-administrator"))
            roles.Add(Configuration["Auth0:Roles:organization-administrator"] ??
                      throw new InvalidOperationException(
                          "Auth0:Roles:organization-administrator not found in appsettings.json!"));

        // Create invite
        await AuthOManagementClient.Organizations.CreateInvitationAsync(organizationId,
            new OrganizationCreateInvitationRequest
            {
                // Invitee
                Invitee = new OrganizationInvitationInvitee
                {
                    Email = Model.EmailAddress
                },

                // Inviter
                Inviter = new OrganizationInvitationInviter
                {
                    Name = Users.First(u => u.Id == _settings.UserId).DisplayName
                },

                // Web App Client ID
                ClientId = Configuration["Auth0:Auth:ClientId"] ??
                           throw new InvalidOperationException("Auth0:Auth:ClientId not found in appsettings.json!"),

                // Authentication provider connection ID
                ConnectionId = Configuration["Auth0:ConnectionId"] ??
                               throw new InvalidOperationException(
                                   "Auth0:Auth:ClientId not found in appsettings.json!"),

                // Roles
                Roles = roles,
                SendInvitationEmail = true
            });

        // Notify the user 
        Snackbar.Add("User has successfully been invited!", Severity.Success);
        
        // Clear the form fields
        Model = new InviteUserForm();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }
}