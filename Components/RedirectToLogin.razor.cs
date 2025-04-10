namespace Web.Components;

public class RedirectToLoginBase : ComponentBase
{
    // Class variables
    private string _currentUrl = "/";
    private string _loginUrl = "/auth/login?returnUrl=/";

    // Component parameters and dependency injection
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    // Lifecycle method called after the component has rendered
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;

        // Get current url as an escaped string
        _currentUrl = Uri.EscapeDataString(new Uri(NavigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

        // Create login url with redirect ulr to the current page
        _loginUrl = $"/auth/login?returnUrl={_currentUrl}";

        // Navigate to the login URL
        NavigationManager.NavigateTo(_loginUrl, true);
    }
}