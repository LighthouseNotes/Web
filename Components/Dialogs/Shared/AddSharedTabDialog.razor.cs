using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Web.Components.Dialogs.Shared;

public class AddSharedTabDialogBase : ComponentBase
{
    [CascadingParameter] private MudDialogInstance? MudDialog { get; set; }

    [Parameter] public required string CaseId { get; set; }

    [Inject] private LighthouseNotesAPIPost LighthouseNotesPostAPI { get; set; } = default!;

    // Variables
    protected readonly AddTabForm Model = new();

    // Add tab form
    protected class AddTabForm
    {
        [Required]
        [StringLength(40, ErrorMessage = "Name length can't be more than 40 characters.")]
        public string Name { get; set; } = null!;
    }

    // Cancel button - close dialog on cancel button press
    protected void Cancel()
    {
        MudDialog?.Cancel();
    }

    // On valid submit - call API and close dialog on valid submit
    protected async Task OnValidSubmit(EditContext context)
    {
        await LighthouseNotesPostAPI.SharedTab(Model.Name, CaseId);
        MudDialog?.Close(DialogResult.Ok(true));
    }
}