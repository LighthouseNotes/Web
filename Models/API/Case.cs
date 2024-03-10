// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Web.Models.API;

public class Case
{
    public required string Id { get; init; }
    public required string DisplayId { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; init; }
    public required User SIO { get; set; }
    public DateTime Modified { get; init; }
    public DateTime Accessed { get; init; }
    public DateTime Created { get; init; }
    public required string Status { get; set; }
    public required ICollection<User> Users { get; set; }
}

public class AddCase
{
    public required string DisplayId { get; set; }
    public required string Name { get; set; }

    public string? SIOUserId { get; set; }
    public List<string>? UserIds { get; set; }
}

public class UpdateCase
{
    public string? DisplayId { get; set; }
    public string? Name { get; set; }
    public string? SIOUserId { get; set; }
    public string? Status { get; set; }
}