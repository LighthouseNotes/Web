namespace Web.Components.Pages;

public class UserBase : ComponentBase
{
    // Page variables
    protected PageLoad? PageLoad;
    protected API.User User = null!;

    // Component parameters and dependency injection
    [Parameter] public required string EmailAddress { get; set; }

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - get user from API
    protected override async Task OnParametersSetAsync()
    {
        // Fetch user with the provided ID
        User = await LighthouseNotesAPIGet.User(EmailAddress);

        // Mark page loaded
        PageLoad?.LoadComplete();
    }
}