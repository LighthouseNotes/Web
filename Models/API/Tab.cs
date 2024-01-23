// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Web.Models.API;

public class Tab
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DateTime Created { get; init; }
}

public class SharedTab
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DateTime Created { get; init; }
    public required User Creator { get; init; }
}

public class AddTab
{
    public required string Name { get; set; }
}