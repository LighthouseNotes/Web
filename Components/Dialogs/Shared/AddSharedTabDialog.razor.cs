namespace Web.Components.Dialogs.Shared;

public class AddSharedTabDialogBase : ComponentBase
{
    // Page variables
    protected readonly AddTabForm Model = new();

    // Component parameters and dependency injection
    [CascadingParameter] private MudDialog? MudDialog { get; set; }

    [Parameter] public required string CaseId { get; set; }

    [Inject] private LighthouseNotesAPIPost LighthouseNotesPostAPI { get; set; } = null!;

    // Cancel button - close dialog on cancel button press
    protected void Cancel()
    {
        MudDialog?.CloseAsync();
    }

    // On valid submit - call API and close dialog on valid submit
    protected async Task OnValidSubmit(EditContext context)
    {
        await LighthouseNotesPostAPI.SharedTab(Model.Name, CaseId);
        MudDialog?.CloseAsync(DialogResult.Ok(true));
    }

    // Add tab form
    protected class AddTabForm
    {
        [Required]
        [StringLength(40, ErrorMessage = "Name length can't be more than 40 characters.")]
        public string Name { get; set; } = null!;
    }
}