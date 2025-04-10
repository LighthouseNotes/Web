// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Web.Models.API;

public class Case
{
    public required string Id { get; set; }
    public required string DisplayId { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required User LeadInvestigator { get; set; }
    public DateTime Modified { get; set; }
    public DateTime Accessed { get; set; }
    public DateTime Created { get; set; }
    public required string Status { get; set; }
    public required List<User> Users { get; set; }
}

public class AddCase
{
    public required string DisplayId { get; set; }
    public required string Name { get; set; }
    public string? LeadInvestigatorEmailAddress { get; set; }
    public List<string>? EmailAddresses { get; set; }
}

public class UpdateCase
{
    public string? DisplayId { get; set; } = null;
    public string? Name { get; set; } = null;
    public string? LeadInvestigatorEmailAddress { get; set; } = null;
    public string? Status { get; set; } = null;
}