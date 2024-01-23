// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Web.Models.API;

public class User
{
    public required string Id { get; init; }
    public required string JobTitle { get; set; }
    public required string DisplayName { get; set; }
    public required string GivenName { get; set; }
    public required string LastName { get; set; }
    public required string EmailAddress { get; init; }
    public required string ProfilePicture { get; set; }
    public required Organization Organization { get; init; }
    public required IEnumerable<string> Roles { get; set; }
}

public class AddUser
{
    public required string Id { get; set; }
    public required string JobTitle { get; set; }
    public required string GivenName { get; set; }
    public required string LastName { get; set; }
    public required string DisplayName { get; set; }
    public required string EmailAddress { get; set; }
    public required string ProfilePicture { get; set; }
    public required List<string> Roles { get; set; }
}

public class UpdateUser
{
    public string? JobTitle { get; set; }
    public string? GivenName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? EmailAddress { get; set; }
    public string? ProfilePicture { get; set; }
    public List<string>? Roles { get; set; }
}