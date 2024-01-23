// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Web.Models.API;

public class ContemporaneousNotes
{
    public required string Id { get; set; }
    public required DateTime Created { get; set; }
}

public class SharedContemporaneousNotes
{
    public required string Id { get; set; }
    public required DateTime Created { get; set; }
    public required User Creator { get; set; }
}