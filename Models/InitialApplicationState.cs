namespace Web.Models;

public class InitialApplicationState
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? XsrfToken { get; init; }
}