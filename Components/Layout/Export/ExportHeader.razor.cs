namespace Web.Components.Layout.Export;

public class ExportHeaderBase : ComponentBase
{
    [Parameter] public required string DisplayName { get; set; }

    [Parameter] public required string TimeZone { get; set; }
}