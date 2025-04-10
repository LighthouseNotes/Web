// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace Web.Models.API;

public class User
{
    public required string EmailAddress { get; init; }
    public required string JobTitle { get; set; }
    public required string DisplayName { get; set; }
    public required string GivenName { get; set; }
    public required string LastName { get; set; }
    public required string ProfilePicture { get; init; }
    public override string ToString() => $"{DisplayName} - {JobTitle}";
}

public class AddUser
{
    public required string EmailAddress { get; set; }
    public required string JobTitle { get; set; }
    public required string GivenName { get; set; }
    public required string LastName { get; set; }
    public required string DisplayName { get; set; }
}

public class UpdateUser
{
    public string? JobTitle { get; set; }
    public string? GivenName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? EmailAddress { get; set; }
    public List<string>? Roles { get; set; }
}

public class UserSettings
{
    public required string TimeZone { get; set; }
    public required string DateFormat { get; set; }
    public required string TimeFormat { get; set; }
    public required string Locale { get; set; }
}