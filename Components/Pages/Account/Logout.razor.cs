namespace Web.Components.Pages.Account;

public class LogoutBase : ComponentBase
{
    // Class variables
    private string _currentUrl = "/";
    private string _loginUrl = "/auth/login?returnUrl=/";

    // Component parameters and dependency injection
    [Inject] private ISettingsService SettingsService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - check auth state and set login url
    protected override async Task OnParametersSetAsync()
    {
        // Get the authentication state
        AuthenticationState authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        // If the user is authenticated then redirect to the server side logout
        if (authenticationState.User.Identity is { IsAuthenticated: true })
            NavigationManager.NavigateTo("/auth/logout");

        // Get current url as an escaped string
        _currentUrl = Uri.EscapeDataString(new Uri(NavigationManager.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped));

        // If the current url contains logout then set current url to / so don't redirect to log out on login
        if (_currentUrl.Contains("logout")) _currentUrl = "/";

        // Create login url with redirect ulr to the current page
        _loginUrl = $"/auth/login?returnUrl={_currentUrl}";
    }

    // Lifecycle method called after the component has rendered - remove settings
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await SettingsService.Remove();
    }

    // Login link clicked
    protected void LoginClick()
    {
        NavigationManager.NavigateTo(_loginUrl, true);
    }
}