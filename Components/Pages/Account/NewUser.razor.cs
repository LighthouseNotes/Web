using Web.Models.API;

namespace Web.Components.Pages.Account;

public class NewUserBase : ComponentBase
{
    // Page variables
    protected readonly NewUserForm Model = new();
    protected PageLoad? PageLoad;

    // Component parameters and dependency injection
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = null!;
    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - prefill form based on user claims
    protected override async Task OnParametersSetAsync()
    {
        // Get authentication state
        AuthenticationState authenticationState = await AuthenticationState.GetAuthenticationStateAsync();

        // Get user email address from JWT token
        string? emailAddress =
            authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        // If email address is null throw exception
        if (emailAddress == null)
            throw new AuthenticationException("Email Address cannot be found in the JSON Web Token (JWT)!");

        // Set the email address form field
        Model.EmailAddress = emailAddress;

        // Attempt to get more information from JWT
        string? givenName = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        string? lastName = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        string? jobTitle = authenticationState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        // Attempt to set form details if the values are not null
        if (givenName != null) Model.GivenName = givenName;
        if (lastName != null) Model.LastName = lastName;
        if (jobTitle != null) Model.JobTitle = jobTitle;
        if (givenName != null && lastName != null) Model.DisplayName = $"{givenName[0]}{lastName}";

        // Mark page load as complete
        PageLoad?.LoadComplete();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // On valid submit - call API to create user
    protected async Task OnValidSubmit(EditContext context)
    {
        // Create add user model
        AddUser newUser = new()
        {
            EmailAddress = Model.EmailAddress,
            DisplayName = Model.DisplayName,
            GivenName = Model.GivenName,
            LastName = Model.LastName,
            JobTitle = Model.JobTitle
        };

        // Call api
        await LighthouseNotesAPIPost.User(newUser);

        // Navigate home
        NavigationManager.NavigateTo("/");
    }

    protected class NewUserForm
    {
        [Required(AllowEmptyStrings = false)] public string GivenName { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string LastName { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string DisplayName { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string EmailAddress { get; set; } = null!;

        [Required(AllowEmptyStrings = false)] public string JobTitle { get; set; } = null!;
    }
}