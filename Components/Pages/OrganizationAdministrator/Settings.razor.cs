using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Web.Components.Pages.OrganizationAdministrator;

public class ConfigBase : ComponentBase
{
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private LighthouseNotesAPIPut LighthouseNotesAPIPut { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // Page variables
    protected PageLoad? PageLoad;
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    protected SettingsForm Model = new();

    // Class variables
    private API.OrganizationSettings _organizationSettings = null!;

    protected class SettingsForm
    {
        [Required] public string S3Endpoint { get; set; } = null!;

        [Required] public string S3BucketName { get; set; } = null!;

        [Required] public bool S3NetworkEncryption { get; set; } = true;

        [Required] public string S3AccessKey { get; set; } = null!;

        [Required] public string S3SecretKey { get; set; } = null!;
        [Required] public string MeilisearchUrl { get; set; } = null!;
        [Required] public string MeilisearchApiKey { get; set; } = null!;
    }

    // Page initialized
    protected override async Task OnInitializedAsync()
    {
        // Get organization settings from API
        _organizationSettings = await LighthouseNotesAPIGet.OrganizationSettings();

        // Set form fields
        Model.S3Endpoint = _organizationSettings.S3Endpoint!;
        Model.S3BucketName = _organizationSettings.S3BucketName!;
        Model.S3NetworkEncryption = _organizationSettings.S3NetworkEncryption;
        Model.S3AccessKey = _organizationSettings.S3AccessKey!;
        Model.S3SecretKey = _organizationSettings.S3SecretKey!;
        Model.MeilisearchUrl = _organizationSettings.MeilisearchUrl!;
        Model.MeilisearchApiKey = _organizationSettings.MeilisearchApiKey!;
        
        // Mark page load as complete
        PageLoad?.LoadComplete();
    }

    // On valid form submission
    protected async Task OnValidSubmit(EditContext context)
    {
        // Call API to save changes if value is changed
        await LighthouseNotesAPIPut.Settings(new API.OrganizationSettings
        {
            S3Endpoint = Model.S3Endpoint != _organizationSettings.S3Endpoint ? Model.S3Endpoint : null,
            S3BucketName = Model.S3BucketName != _organizationSettings.S3BucketName ? Model.S3BucketName : null,
            S3NetworkEncryption = Model.S3NetworkEncryption,
            S3AccessKey = Model.S3AccessKey != _organizationSettings.S3AccessKey ? Model.S3AccessKey : null,
            S3SecretKey = Model.S3SecretKey != _organizationSettings.S3SecretKey ? Model.S3SecretKey : null,
            MeilisearchUrl = Model.MeilisearchUrl != _organizationSettings.MeilisearchUrl ? Model.MeilisearchUrl : null,
            MeilisearchApiKey = Model.MeilisearchApiKey != _organizationSettings.MeilisearchApiKey ? Model.MeilisearchApiKey : null
        });

        // Notify the user
        Snackbar.Add("Organization settings saved!", Severity.Success);
    }
}