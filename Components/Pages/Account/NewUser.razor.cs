using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Web.Models.API;

namespace Web.Components.Pages.Account;

public class NewUserBase : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    // Page variables
    protected readonly FindOrganizationForm Model = new();

    protected class FindOrganizationForm
    {
        [Required(AllowEmptyStrings = false)] public string GivenName { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string LastName { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string DisplayName { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string EmailAddress { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string JobTitle { get; set; } = null!;
    }

    // Page rendered
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Get authentication state 
            AuthenticationState authenticationState = await AuthenticationState.GetAuthenticationStateAsync();

            // Get user email address from JWT token
            string? emailAddress =
                authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            // If email address is null throw exception
            if (emailAddress == null)
                throw new AuthenticationException("User ID can not be found in the JSON Web Token (JWT)!");

            // Set the email address form field
            Model.EmailAddress = emailAddress;

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // On valid submit 
    protected async Task OnValidSubmit(EditContext context)
    {
        // Get authentication state 
        AuthenticationState authenticationState = await AuthenticationState.GetAuthenticationStateAsync();

        // Create add user model
        AddUser a = new()
        {
            Id = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value!,
            DisplayName = Model.DisplayName,
            EmailAddress = Model.EmailAddress,
            GivenName = Model.GivenName,
            LastName = Model.LastName,
            JobTitle = Model.JobTitle,
            ProfilePicture = authenticationState.User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value!,
            Roles = authenticationState.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList()
        };

        // Call api
        await LighthouseNotesAPIPost.User(a);

        // Navigate home
        NavigationManager.NavigateTo("/");
    }
}