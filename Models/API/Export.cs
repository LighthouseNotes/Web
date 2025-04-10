// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Web.Models.API;

public class Export
{
    public required string DisplayName { get; init; }
    public required User LeadInvestigator { get; init; }
    public DateTime Modified { get; init; }
    public DateTime Created { get; init; }
    public required string Status { get; init; }
    public required ICollection<User> Users { get; init; }
    public required List<ContemporaneousNotesExport> ContemporaneousNotes { get; init; }
    public required List<TabExport> Tabs { get; init; }
    public required List<SharedContemporaneousNotesExport> SharedContemporaneousNotes { get; init; }
    public required List<SharedTabExport> SharedTabs { get; init; }
}

public class ContemporaneousNotesExport
{
    public required string Content { get; init; }
    public DateTime DateTime { get; init; }
}

public class SharedContemporaneousNotesExport
{
    public required string Content { get; init; }
    public DateTime Created { get; init; }
    public required User Creator { get; init; }
}

public class TabExport
{
    public required string Name { get; init; }
    public required string Content { get; init; }
}

public class SharedTabExport
{
    public required string Name { get; init; }
    public required string Content { get; init; }
    public required DateTime Created { get; init; }
    public required User Creator { get; init; }
}