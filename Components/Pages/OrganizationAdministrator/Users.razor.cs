using System.ComponentModel.DataAnnotations;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Microsoft.AspNetCore.Components.Forms;
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
    
    // Page variables
    protected string RoleSelectValue = "Nothing selected";
    protected string? SearchString;
    protected InviteUserForm Model = new();
    protected MudDataGrid<API.User> UsersTable = null!;
    
    protected class InviteUserForm
    {
        [Required(AllowEmptyStrings = false)] public string EmailAddress { get; set; } = null!;

        public IEnumerable<string> Roles { get; set; } = new List<string> { "user" };
    }
    
    protected async Task<GridData<API.User>> LoadGridData(GridState<API.User> state)
    {
        // Create sort string
        string sortString = "";
        
        // If sort definition is set then set sort string
        if(state.SortDefinitions.Count == 1)
        {
            // if descending is true then column-name desc else column-name asc
            sortString = state.SortDefinitions.First().Descending ? $"{state.SortDefinitions.First().SortBy} desc" : $"{state.SortDefinitions.First().SortBy} asc";
        }
        
        // Fetch cases from API
        (API.Pagination, List<API.User>) users = await LighthouseNotesAPIGet.Users(state.Page + 1, state.PageSize, sortString);
        
        // Create grid data
        GridData<API.User> data = new()
        {
            Items = users.Item2,
            TotalItems = users.Item1.Total
        };
        
        // Return grid data
        return data;
    }

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
            await AuthOManagementClient.Organizations.GetAllMemberRolesAsync(organizationId, item.Auth0Id,
                new PaginationInfo());
        List<string> userCurrentRoles = userRoles.Select(x => x.Id).ToList();

        // Remove users current roles
        await AuthOManagementClient.Organizations.DeleteMemberRolesAsync(organizationId, item.Auth0Id,
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
                case "organization administrator":
                    newUserRoles.Add(Configuration["Auth0:Roles:organization-administrator"] ??
                                     throw new InvalidOperationException(
                                         "Auth0:Roles:organization-administrator not found in appsettings.json!"));
                    break;
            }

        // Assign the user the new roles
        await AuthOManagementClient.Organizations.AddMemberRolesAsync(organizationId, item.Auth0Id,
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
            
            // Notify the user
            Snackbar.Add("User deleted!", Severity.Success);

            // Update data grid
            await UsersTable.ReloadServerData();

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
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

        // Get current user
        API.User user = await LighthouseNotesAPIGet.User();
        
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
                    Name = user.DisplayName
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