namespace Web.Models;

public class Settings
{
    public required string EmailAddress { get; init; }
    public required string TimeZone { get; init; }
    public required string DateFormat { get; init; }
    public required string TimeFormat { get; init; }
    public required string DateTimeFormat { get; init; }
}