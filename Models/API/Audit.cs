// ReSharper disable ClassNeverInstantiated.Global

namespace Web.Models.API;

public class UserAudit
{
    public DateTime DateTime { get; set; }
    public required string Action { get; set; }
}