﻿using HtmlAgilityPack;
using MudBlazor;


namespace Web.Components.Pages.Case.Shared;

public class SharedTabBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }

    [Parameter] public required string TabId { get; set; }

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    [Inject] private IDialogService Dialog { get; set; } = default!;

    [Inject] private IConfiguration Configuration { get; set; } = default!;

    [Inject] private ISettingsService SettingsService { get; set; } = default!;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    // API Objects
    protected API.SharedTab Tab = null!;
    protected API.Case SCase = null!;
    private List<API.User> _caseUsers = new();
    private List<API.Exhibit> _exhibits = new();

    // Page load element 
    protected PageLoad? PageLoad;

    // Images
    private string _imagePath = null!;
    private string _imageSaveUrl = null!;

    // Tab content
    protected (bool?, HtmlNode?) TabContent;

    // Settings
    protected Models.Settings Settings = new();

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // Get case details
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Set users from case details
        _caseUsers = SCase.Users.ToList();

        // Get exhibits linked to case
        (API.Pagination, List<API.Exhibit>?)
            exhibitsWithPagination = await LighthouseNotesAPIGet.Exhibits(CaseId, 1, 0);
        _exhibits = exhibitsWithPagination.Item2!;

        // Get tab details
        Tab = await LighthouseNotesAPIGet.SharedTab(CaseId, TabId);

        // Set image save url
        _imageSaveUrl = $"{Configuration["LighthouseNotesApiUrl"]}/case/{CaseId}/shared/tab/image";

        // Get tab content
        await GetTabContent();

        // Mark page load as complete 
        PageLoad?.LoadComplete();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // After page render
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // If settings is null the get the settings
        if (Settings.Auth0UserId == null || Settings.OrganizationId == null || Settings.UserId == null ||
            Settings.S3Endpoint == null)
        {
            // Get the settings redirect url
            string? settingsRedirect = await SettingsService.CheckOrSet();
            
            // If the settings redirect url is not null then redirect 
            if (settingsRedirect != null)
            {
                NavigationManager.NavigateTo(settingsRedirect, true);
            }
            
            // Use the setting service to retrieve the settings
            Settings = await SettingsService.Get();

            // Set Image Path
            _imagePath =
                $"{Settings.S3Endpoint}/cases/{CaseId}/shared/tabs/images/";

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // Get tab content from s3 bucket
    private async Task GetTabContent()
    {
        (bool, string) tabContent = await LighthouseNotesAPIGet.SharedTabContent(CaseId, TabId);

        // If false then tab does not contain any content
        if (tabContent.Item1 != true)
        {
            TabContent.Item1 = false;
            TabContent.Item2 = null;
            return;
        }

        // Create HTML document
        HtmlDocument htmlDoc = new();

        // Load ContemporaneousNotes Content to HTML
        htmlDoc.LoadHtml(tabContent.Item2);

        // If content contains images
        if (htmlDoc.DocumentNode.SelectNodes("//img") != null)
            // For each image that starts with .path/
            foreach (HtmlNode img in htmlDoc.DocumentNode.SelectNodes("//img")
                         .Where(u => u.Attributes["src"].Value.Contains(".path/")))
            {
                // Create variable with file name
                string fileName = img.Attributes["src"].Value.Replace(".path/", "");

                // Get presigned s3 url and update image src 
                string presignedS3Url = await LighthouseNotesAPIGet.SharedImage(CaseId, "tab", fileName);
                img.Attributes["src"].Value = presignedS3Url;
            }

        // Create a list of panels 
        TabContent.Item1 = true;
        TabContent.Item2 = htmlDoc.DocumentNode;
    }

    protected void UserClick()
    {
        NavigationManager.NavigateTo($"/user/{Tab.Creator.Id}", true);
    }

    // Open rich text editor dialog on edit content button clicked
    protected async Task OpenRichTextEditor()
    {
        // Create dialog parameters
        DialogParameters<SharedTabRTEDialog> parameters = new()
        {
            { p => p.CaseId, CaseId },
            { p => p.TabId, TabId },
            { p => p.SCase, SCase },
            { p => p.CaseUsers, _caseUsers },
            { p => p.Exhibits, _exhibits },
            { p => p.ImageSaveUrl, _imageSaveUrl },
            { p => p.ImagePath, _imagePath },
            { p => p.TabContent, TabContent }
        };

        // Set dialog options
        DialogOptions options = new() { FullScreen = true, CloseButton = true };

        // Show dialog
        IDialogReference? dialog =
            await Dialog.ShowAsync<SharedTabRTEDialog>("Rich text editor", parameters, options);

        // Await for dialog result
        DialogResult? result = await dialog.Result;

        // If result is not canceled, means tab content has been modified so get new content
        if (!result.Canceled)
        {
            await GetTabContent();
            await InvokeAsync(StateHasChanged);
        }
    }
}