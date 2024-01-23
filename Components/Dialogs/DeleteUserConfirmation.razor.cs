using MudBlazor;

namespace Web.Components.Dialogs;

public class DeleteUserConfirmationBase : ComponentBase
{
    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public required string DisplayName { get; set; }

    protected void Submit()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }

    protected void Cancel()
    {
        MudDialog.Cancel();
    }
}