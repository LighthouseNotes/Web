namespace Web.Models;

public class Settings
{
    public string? Auth0UserId { get; init; }
    public string? OrganizationId  { get; init; }
    public string? UserId { get; init; }
    public string TimeZone { get; init; } = "GMT Standard Time";
    public string DateFormat { get; init; } = "yyyy-MM-dd";
    public string TimeFormat { get; init; }  = "HH:mm";
    public string DateTimeFormat { get; init; } = "yyyy-MM-dd'T'HH:mm:ss.FFF";
    public string? S3Endpoint { get; init; }
}