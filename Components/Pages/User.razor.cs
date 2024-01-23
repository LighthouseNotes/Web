namespace Web.Components.Pages;

public class UserBase : ComponentBase
{
    [Parameter] public required string UserId { get; set; }

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    // Variables
    protected PageLoad? PageLoad;
    protected API.User User = null!;

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // Fetch user with the provided ID
        User = await LighthouseNotesAPIGet.User(UserId);

        // Mark page loaded
        PageLoad?.LoadComplete();
    }
}