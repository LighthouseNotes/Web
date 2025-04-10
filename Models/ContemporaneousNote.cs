namespace Web.Models;

public class SharedContemporaneousNote
{
    public required DateTime Created { get; init; }
    public required string Content { get; init; }
    public required API.User Creator { get; init; }
}