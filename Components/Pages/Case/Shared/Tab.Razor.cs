using HtmlAgilityPack;

namespace Web.Components.Pages.Case.Shared;

public class SharedTabBase : ComponentBase
{
    // Page load element
    protected PageLoad? PageLoad;

    // Settings
    protected Settings Settings = null!;

    // API Objects
    protected API.SharedTab Tab = null!;
    protected API.Case SCase = null!;
    private List<API.User> _caseUsers = [];
    private List<API.Exhibit> _exhibits = [];

    // Tab content
    protected (bool?, HtmlNode?) TabContent;

    // Component parameters and dependency injection
    [Parameter] public required string CaseId { get; set; }
    [Parameter] public required string TabId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;
    [Inject] private IDialogService Dialog { get; set; } = null!;
    [Inject] private ISettingsService SettingsService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - get case, exhibits and tab content
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

        // Get tab content
        await GetTabContent();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    //  Lifecycle method called after the component has rendered - get settings
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Call Check, Get or Set - to get the settings or a redirect url
            (string?, Settings?) settingsCheckOrSetResult = await SettingsService.CheckGetOrSet();

            // If a redirect url is provided then use it
            if (settingsCheckOrSetResult.Item1 != null)
            {
                NavigationManager.NavigateTo(settingsCheckOrSetResult.Item1, true);
                return;
            }

            // Set settings to the result
            Settings = settingsCheckOrSetResult.Item2!;

            // Mark page load as complete
            PageLoad?.LoadComplete();

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

        // Get all image elements that start with ./path
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        IEnumerable<HtmlNode>? imagesWithPath = htmlDoc.DocumentNode
            .SelectNodes("//img")
            ?.Where(img => img.GetAttributeValue("src", "").Contains(".path/"));

        // If we have image elements
        if (imagesWithPath != null)
            // For each image that starts with .path/
            foreach (HtmlNode img in imagesWithPath)
            {
                // Create variable with file name
                string fileName = img.Attributes["src"].Value.Replace(".path/", "");

                // Get presigned s3 url and update image src
                string presignedS3Url =
                    await LighthouseNotesAPIGet.SharedFile(CaseId, "tab", fileName);
                img.Attributes["src"].Value = presignedS3Url;
            }


        // Create a list of panels
        TabContent.Item1 = true;
        TabContent.Item2 = htmlDoc.DocumentNode;
    }

    // User click - username clicked redirect to user details page
    protected void UserClick()
    {
        NavigationManager.NavigateTo($"/user/{Tab.Creator.EmailAddress}", true);
    }

    // Open rich text editor dialog on edit content button clicked
    protected async Task OpenRichTextEditor()
    {
        // Create dialog parameters
        DialogParameters<SharedTabTextEditorDialog> parameters = new()
        {
            { p => p.CaseId, CaseId },
            { p => p.TabId, TabId },
            { p => p.SCase, SCase },
            { p => p.CaseUsers, _caseUsers },
            { p => p.Exhibits, _exhibits },
            { p => p.TabContent, TabContent }
        };

        // Set dialog options
        DialogOptions options = new() { FullScreen = true, CloseButton = true };

        // Show dialog
        IDialogReference dialog =
            await Dialog.ShowAsync<SharedTabTextEditorDialog>("Rich text editor", parameters, options);

        // Await for dialog result
        DialogResult? result = await dialog.Result;

        // If result is not canceled, means tab content has been modified so get new content
        if (!result!.Canceled)
        {
            await GetTabContent();
            await InvokeAsync(StateHasChanged);
        }
    }
}