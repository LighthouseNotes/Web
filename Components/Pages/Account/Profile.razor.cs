using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Web.Components.Pages.Account;

public class ProfileBase : ComponentBase
{
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // Page variables
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    protected ProfileForm Model = new();
    protected PageLoad? PageLoad;
    protected API.User User = null!;

    protected class ProfileForm
    {
        [Required] public string JobTitle { get; set; } = null!;

        [Required] public string GivenName { get; set; } = null!;

        [Required] public string LastName { get; set; } = null!;

        [Required] public string DisplayName { get; set; } = null!;

        [Required] public string EmailAddress { get; set; } = null!;
    }

    protected override async Task OnInitializedAsync()
    {
        // Fetch the user details from the API
        User = await LighthouseNotesAPIGet.User();

        // Set form fields from API response
        Model.JobTitle = User.JobTitle;
        Model.GivenName = User.GivenName;
        Model.LastName = User.LastName;
        Model.DisplayName = User.DisplayName;
        Model.EmailAddress = User.EmailAddress;

        // Mark page load as completed
        PageLoad?.LoadComplete();
    }

    protected async Task OnValidSubmit(EditContext context)
    {
        // Create updated user
        API.UpdateUser updateUser = new()
        {
            JobTitle = Model.JobTitle != User.JobTitle ? Model.JobTitle : null,
            GivenName = Model.GivenName != User.GivenName ? Model.GivenName : null,
            LastName = Model.LastName != User.LastName ? Model.LastName : null,
            DisplayName = Model.DisplayName != User.DisplayName ? Model.DisplayName : null
        };

        // Call API to update the user
        await LighthouseNotesAPIPut.User(updateUser);

        // Notify the user of case creation
        Snackbar.Add("User has been updated!", Severity.Success);

        // If values are changed update form field 
        if (User.JobTitle != Model.JobTitle) User.JobTitle = Model.JobTitle;
        if (User.GivenName != Model.GivenName) User.GivenName = Model.GivenName;
        if (User.LastName != Model.LastName) User.LastName = Model.LastName;
        if (User.DisplayName != Model.DisplayName) User.DisplayName = Model.DisplayName;

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // Sync profile picture
    protected async Task SyncProfilePicture()
    {
        // Get authentication state 
        AuthenticationState authenticationState = await AuthenticationState.GetAuthenticationStateAsync();

        // Get profile picture URL from claims
        string profilePicture = authenticationState.User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value!;

        // Create updated user
        API.UpdateUser updateUser = new()
        {
            ProfilePicture = profilePicture
        };

        // Call API 
        await LighthouseNotesAPIPut.User(updateUser);

        // Notify the user 
        Snackbar.Add("Your profile picture has been updated!", Severity.Success);

        // If profile picture has been updated then update it 
        if (User.ProfilePicture != profilePicture) User.ProfilePicture = profilePicture;

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }
}