﻿using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;

namespace Web.Components.Pages.Case.Personal;

public partial class TabBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }
    [Parameter] public required string TabId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;
    [Inject] private IDialogService Dialog { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;


    // API Objects
    protected API.Tab Tab = null!;
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
        (API.Pagination, List<API.Exhibit>?) exhibitsWithPagination = await LighthouseNotesAPIGet.Exhibits(CaseId, 1, 0);
        _exhibits = exhibitsWithPagination.Item2!;

        // Get tab details
        Tab = await LighthouseNotesAPIGet.Tab(CaseId, TabId);

        // Set image save url
        _imageSaveUrl = $"{Configuration["LighthouseNotesApiUrl"]}/case/{CaseId}/tab/image";

        // Get tab content
        await GetTabContent();

        // Mark page load as complete 
        PageLoad?.LoadComplete();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // Page initialized
    // protected override async Task OnInitializedAsync()
    // {
    //   
    // }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Get user settings from browser storage
            ProtectedBrowserStorageResult<Models.Settings> result =
                await ProtectedLocalStore.GetAsync<Models.Settings>("settings");

            // If result is success and not null assign value from browser storage, if result is success and null assign default values, if result is unsuccessful assign default values
            Settings = result.Success ? result.Value ?? new Models.Settings() : new Models.Settings();

            // Set Image Path
            _imagePath =
                $"{Settings.S3Endpoint}/cases/{CaseId}/{Settings.UserId}/tabs/images/";

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // Get tab content from s3 bucket
    private async Task GetTabContent()
    {
        (bool, string) tabContent = await LighthouseNotesAPIGet.TabContent(CaseId, TabId);

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
                string presignedS3Url = await LighthouseNotesAPIGet.Image(CaseId, "tab", fileName);
                img.Attributes["src"].Value = presignedS3Url;
            }

        // Create a list of panels 
        TabContent.Item1 = true;
        TabContent.Item2 = htmlDoc.DocumentNode;
    }

    // Open rich text editor dialog on edit content button clicked
    protected async Task OpenRichTextEditor()
    {
        // Create dialog parameters
        DialogParameters<TabRTEDialog> parameters = new()
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
            await Dialog.ShowAsync<TabRTEDialog>("Rich text editor", parameters, options);

        // Await for dialog result
        DialogResult? result = await dialog.Result;

        // If result is not canceled, means tab content has been modified so get new content
        if (!result.Canceled)
        {
            await GetTabContent();
            await InvokeAsync(StateHasChanged);
        }
    }

    [GeneratedRegex("Can not find the S3 object for the tab with the ID `.+` at the following path `.+`.")]
    private static partial Regex MyRegex();
}