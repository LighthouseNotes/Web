namespace Web.Components.Layout.Containers;

public class PageLoadBase : ComponentBase
{
    // Page variables
    protected bool IsVisible { get; set; } = true;

    // Component parameters and dependency injection
    [Parameter] public required RenderFragment ChildContent { get; set; }
    [Parameter] public MaxWidth MaxWidth { get; set; } = MaxWidth.ExtraLarge;
    [Inject] public SpinnerService SpinnerService { get; set; } = null!;


    // Lifecycle method triggered when parameters are set or changed - register the load complete function
    protected override void OnParametersSet()
    {
        SpinnerService.LoadCompleted += LoadComplete;
    }

    // Load Complete - Set IsVisible to false which will hide loading and re-render
    public void LoadComplete()
    {
        IsVisible = false;
        StateHasChanged();
    }
}