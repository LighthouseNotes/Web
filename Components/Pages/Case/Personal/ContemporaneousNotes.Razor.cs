using System.Text;
using System.Text.Json;
using System.Web;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Routing;

namespace Web.Components.Pages.Case.Personal;

public class ContemporaneousNotesBase : ComponentBase
{
    // Text Editor Variables
    protected readonly QuillTool[] Tools =
    [
        new("ql-header", group: 1, options: ["", "1", "2", "3", "4", "5", "6"]),
        new("ql-bold", group: 2),
        new("ql-italic", group: 2),
        new("ql-underline", group: 2),
        new("ql-strike", group: 2),
        new("ql-color", group: 3, options: []),
        new("ql-background", group: 3, options: []),
        new("ql-list", group: 4, value: "ordered"),
        new("ql-list", group: 4, value: "bullet"),
        new("ql-indent", group: 4, value: "-1"),
        new("ql-indent", group: 4, value: "+1"),
        new("ql-align", group: 4, options: ["", "center", "right", "justify"]),
        new("ql-blockquote", group: 5),
        new("ql-code-block", group: 5),
        new("ql-link", group: 6),
        new("ql-image", group: 6)
    ];

    protected IQuillModule[] Modules = null!;
    protected MudExRichTextEdit TextEditor = null!;

    // Contemporaneous Notes
    private List<API.ContemporaneousNotes>
        _allContemporaneousNotes = new(); // List of all contemporaneous notes with note id and created date

    protected Dictionary<DateTime, string>? ContemporaneousNotes; // Contemporaneous notes displayed on the page
    private Dictionary<DateTime, string>? _contemporaneousNotes; // Default contemporaneous notes with no filtering

    // Pagination
    private int _page = 1;
    protected int Pages = 1;
    protected int PageSize = 10;

    // Contemporaneous Notes Expansion Panel Variables
    protected MudExpansionPanels? ContemporaneousNotesCollapse;
    protected bool? HasContemporaneousNotes;
    protected string ToggleCollapseValue = "Expand all";

    // Date Filter and search query
    protected DateRange FilterDateRange = new(null, null);
    protected string SearchQuery = "";

    // Page load element
    protected PageLoad? PageLoad;

    // API Objects
    protected API.Case SCase = null!;
    private List<API.User> _caseUsers = [];
    private List<API.Exhibit> _exhibits = [];

    // Settings
    protected Settings Settings = null!;

    // Component parameters and dependency injection
    [Parameter] public required string CaseId { get; set; }
    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = null!;
    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService Dialog { get; set; } = null!;
    [Inject] private ISettingsService SettingsService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - gets case details, exhibits, sets modules and get notes
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

        // Set mention modules
        Modules =
        [
            new QuillMentionModule<API.User>(
                    (_, s) => Task.FromResult(_caseUsers.Where(u =>
                        u.DisplayName.Contains(s, StringComparison.InvariantCultureIgnoreCase))), '@')
                .SetProperties(m => m.MentionClicked = UserMentionClicked),
            new QuillMentionModule<API.Exhibit>(
                    (_, s) => Task.FromResult(_exhibits.Where(e =>
                        e.Reference.Contains(s, StringComparison.InvariantCultureIgnoreCase))), '#')
                .SetProperties(m => m.MentionClicked = ExhibitMentionClicked)
        ];

        // Get contemporaneous notes
        await GetNotes();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // Lifecycle method called after the component has rendered - get settings
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

    // On upload - use API to upload the image and return presigned url
    protected async Task<string> OnUpload(UploadableFile arg)
    {
        return await LighthouseNotesAPIPost.File(CaseId, "contemporaneous-note", arg);
    }

    // Save Content - Save rich text content to s3 bucket using the API
    protected async Task SaveContent()
    {
        // Get text editor content
        string content = await TextEditor.GetHtml();

        // If no content is provided show and error and return
        if (content == null)
        {
            Snackbar.Add("You have not added any content into the rich text editor, please add content and try again.",
                Severity.Error);
            return;
        }

        // Create HTML document
        HtmlDocument htmlDoc = new();

        // Load value into HTML
        htmlDoc.LoadHtml(content);

        // Get all image elements
        IEnumerable<HtmlNode>? imagesWithPath = htmlDoc.DocumentNode
            .SelectNodes("//img");

        // If we have image elements
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (imagesWithPath != null)
            // For each image that starts with .path/
            foreach (HtmlNode img in imagesWithPath)
            {
                // Create uri from image src
                Uri uri = new(img.Attributes["src"].Value);

                // Remove query string from url
                string fixedUri = uri.AbsoluteUri.Replace(uri.Query, string.Empty);

                // Set src from s3url/100.jpg to .path/100.jpg
                img.Attributes["src"].Value = $".path/{Path.GetFileName(fixedUri)}";
            }

        // Get all span elements with the class mention
        IEnumerable<HtmlNode>? mentions = htmlDoc.DocumentNode
            .SelectNodes("//span[@class='mention']");

        // If we have mention elements
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (mentions != null)
            // Loop through each mention
            foreach (HtmlNode mention in mentions)
                // Handle exhibit and user mentions separately
                switch (mention.Attributes["data-denotation-char"].Value)
                {
                    // User
                    case "@":
                    {
                        // Get the user from json data
                        API.User user = JsonSerializer.Deserialize<API.User>(
                            HttpUtility.HtmlDecode(mention.Attributes["data-__data-json"].Value),
                            JsonOptions.DefaultOptions)!;

                        // Create the new node
                        HtmlNode newNode =
                            HtmlNode.CreateNode(
                                $"<a class=\"mention-link\" href=\"/user/{user.EmailAddress}\"> @{user.DisplayName} </a>");

                        // Replace the node
                        mention.ParentNode.ReplaceChild(newNode, mention);
                        break;
                    }
                    // Exhibit
                    case "#":
                    {
                        // Get the exhibit from json data
                        API.Exhibit exhibit = JsonSerializer.Deserialize<API.Exhibit>(
                            HttpUtility.HtmlDecode(mention.Attributes["data-__data-json"].Value),
                            JsonOptions.DefaultOptions)!;

                        // Create the new node
                        HtmlNode newNode =
                            HtmlNode.CreateNode(
                                $"<a class=\"mention-link\" href=\"/case/{CaseId}/exhibit/{exhibit.Id}\"> #{exhibit.Reference} </a>");

                        // Replace the node
                        mention.ParentNode.ReplaceChild(newNode, mention);
                        break;
                    }
                }

        // Add Contemporaneous Note
        await LighthouseNotesAPIPost.ContemporaneousNote(CaseId,
            Encoding.UTF8.GetBytes(htmlDoc.DocumentNode.OuterHtml));

        // Refresh notes, empty rich text editor, re-render component, and notify user
        await GetNotes();
        await TextEditor.SetHtml(null);
        await InvokeAsync(StateHasChanged);
        Snackbar.Add("Note has successfully been added!", Severity.Success);
    }

    // Time range changed - Change which notes are being displayed based on the data range
    protected async Task TimeRangeChanged(DateRange? dateRange)
    {
        // This function should not be available to be called if ContemporaneousNotes are null but to satisfy null value checks
        if (_contemporaneousNotes == null) return;

        // If no notes are found then set to null and return
        if (ContemporaneousNotes == null)
        {
            ContemporaneousNotes = _contemporaneousNotes;
            await InvokeAsync(StateHasChanged);
            return;
        }

        // If date range is not set then display all the notes
        if (dateRange == null)
        {
            ContemporaneousNotes = _contemporaneousNotes;
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Set the date range on the page
        FilterDateRange = dateRange;

        await PaginateNotes();
    }

    // Search Query Changed - Change which notes are being displayed based on the search
    protected async Task SearchQueryChanged()
    {
        // This function should not be available to be called if ContemporaneousNotes are null but to satisfy null value checks
        if (_contemporaneousNotes == null) return;

        // If no notes are found then set to null and return
        if (ContemporaneousNotes == null)
        {
            ContemporaneousNotes = _contemporaneousNotes;
            await InvokeAsync(StateHasChanged);
            return;
        }

        // If search query is not set then display all the notes
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            ContemporaneousNotes = _contemporaneousNotes;
            await InvokeAsync(StateHasChanged);
            return;
        }

        await PaginateNotes();
    }

    // Page size select change event - recalculate pages and update number of notes displayed
    protected async Task PageSizeChanged(int pageSize)
    {
        // Update page size variable
        PageSize = pageSize;

        // Re Calculate pages
        Pages = (_allContemporaneousNotes.Count + PageSize - 1) / PageSize;

        // Paginate notes
        await PaginateNotes();
    }

    // Page changed event - move on to next page
    protected async Task PageChanged(int page)
    {
        // Update page variable
        _page = page;

        // Paginate notes
        await PaginateNotes();
    }

    // Get a list of all notes, calculate number of pages and paginate
    private async Task GetNotes()
    {
        // Get a list of Contemporaneous Notes
        _allContemporaneousNotes = await LighthouseNotesAPIGet.ContemporaneousNotes(CaseId);

        // If list is empty set variables and return
        if (_allContemporaneousNotes.Count == 0)
        {
            HasContemporaneousNotes = false;
            ContemporaneousNotes = null;

            await InvokeAsync(StateHasChanged);
            return;
        }

        // Calculate pages
        Pages = (_allContemporaneousNotes.Count + PageSize - 1) / PageSize;

        await PaginateNotes();
    }

    // Paginate notes - Get notes from s3 bucket, handling errors and fetching images
    private async Task PaginateNotes()
    {
        // Empty dictionary
        ContemporaneousNotes = new Dictionary<DateTime, string>();

        // If filter date range is set, then use it
        if (FilterDateRange is { Start: not null, End: not null })
        {
            // If no notes match the date filter, then show notes without filter and notify user
            if (!_allContemporaneousNotes.Any(cn =>
                    cn.Created >= FilterDateRange.Start && cn.Created <= FilterDateRange.End)) return;

            // Loop through each contemporaneous note in the list
            foreach (API.ContemporaneousNotes contemporaneousNote in _allContemporaneousNotes
                         .Where(cn => cn.Created >= FilterDateRange.Start && cn.Created <= FilterDateRange.End)
                         .Skip((_page - 1) * PageSize).Take(PageSize))
            {
                // Get note content
                string noteContent = await LighthouseNotesAPIGet.ContemporaneousNote(CaseId, contemporaneousNote.Id);

                // Create HTML document
                HtmlDocument htmlDoc = new();

                // Load ContemporaneousNotes Content to HTML
                htmlDoc.LoadHtml(noteContent);

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
                            await LighthouseNotesAPIGet.File(CaseId, "contemporaneous-note", fileName);
                        img.Attributes["src"].Value = presignedS3Url;
                    }

                // Add contemporaneous note to dictionary
                ContemporaneousNotes.Add(contemporaneousNote.Created, htmlDoc.DocumentNode.OuterHtml);
            }

            await InvokeAsync(StateHasChanged);
            return;
        }

        // If search query is set then use it
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            List<API.ContemporaneousNotes> contemporaneousNotes =
                await LighthouseNotesAPIPost.ContemporaneousNotesSearch(CaseId, SearchQuery);

            // If no notes match the search, then show notes without filter and notify user
            if (contemporaneousNotes.Count == 0) return;

            // Re Calculate pages
            Pages = (contemporaneousNotes.Count + PageSize - 1) / PageSize;

            // Loop through each contemporaneous note in the list
            foreach (API.ContemporaneousNotes contemporaneousNote in contemporaneousNotes.Skip((_page - 1) * PageSize)
                         .Take(PageSize))
            {
                // Get note content
                string noteContent = await LighthouseNotesAPIGet.ContemporaneousNote(CaseId, contemporaneousNote.Id);

                // Create HTML document
                HtmlDocument htmlDoc = new();

                // Load ContemporaneousNotes Content to HTML
                htmlDoc.LoadHtml(noteContent);

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
                            await LighthouseNotesAPIGet.File(CaseId, "contemporaneous-note", fileName);
                        img.Attributes["src"].Value = presignedS3Url;
                    }

                // Add contemporaneous note to dictionary
                ContemporaneousNotes.Add(contemporaneousNote.Created, htmlDoc.DocumentNode.OuterHtml);
            }

            await InvokeAsync(StateHasChanged);
            return;
        }

        _contemporaneousNotes = new Dictionary<DateTime, string>();

        // Loop through each contemporaneous note in the list
        foreach (API.ContemporaneousNotes contemporaneousNote in _allContemporaneousNotes.Skip((_page - 1) * PageSize)
                     .Take(PageSize))
        {
            // Get note content
            string noteContent = await LighthouseNotesAPIGet.ContemporaneousNote(CaseId, contemporaneousNote.Id);

            // Create HTML document
            HtmlDocument htmlDoc = new();

            // Load ContemporaneousNotes Content to HTML
            htmlDoc.LoadHtml(noteContent);

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
                        await LighthouseNotesAPIGet.File(CaseId, "contemporaneous-note", fileName);
                    img.Attributes["src"].Value = presignedS3Url;
                }

            // Add contemporaneous note to dictionary
            _contemporaneousNotes.Add(contemporaneousNote.Created, htmlDoc.DocumentNode.OuterHtml);
        }

        // Set values
        HasContemporaneousNotes = true;
        ContemporaneousNotes = _contemporaneousNotes;
    }

    // Before Navigation - Block user navigation if rich text editor contains content
    protected async Task BeforeInternalNavigation(LocationChangingContext context)
    {
        // Get text editor content
        string content = await TextEditor.GetHtml();

        //If content is empty allow navigation
        if (content == "<p><br></p>" || string.IsNullOrWhiteSpace(content))
            return;

        // content is not empty, display a modal
        bool? result = await Dialog.ShowMessageBox(
            "Warning",
            "Are you sure you want to leave this page without saving?",
            "Yes!", cancelText: "No!");

        // Get state of modal
        bool state = result != null;

        // If state is false then stop navigation (User clicked "no")
        if (!state)
            context.PreventNavigation();
    }

    // Open rich text editor dialog - show rich text editor in a dialog on mobile devices
    protected async Task OpenRichTextEditor()
    {
        // Create dialog parameters
        DialogParameters<ContemporaneousNoteTextEditorDialog> parameters = new()
        {
            { p => p.CaseId, CaseId },
            { p => p.SCase, SCase },
            { p => p.CaseUsers, _caseUsers },
            { p => p.Exhibits, _exhibits }
        };

        // Set dialog options
        DialogOptions options = new() { FullScreen = true, CloseButton = true };

        // Show dialog
        IDialogReference dialog =
            await Dialog.ShowAsync<ContemporaneousNoteTextEditorDialog>("Rich text editor", parameters, options);

        // Await for dialog result
        DialogResult? result = await dialog.Result;

        // If result is not canceled, means note has been added so get notes
        if (result is { Canceled: false })
        {
            await GetNotes();
            await InvokeAsync(StateHasChanged);
        }
    }

    // Toggle collapse - expand / collapse expansion panels containing notes
    protected async Task ToggleCollapse()
    {
        if (ToggleCollapseValue == "Collapse all")
        {
            await ContemporaneousNotesCollapse?.CollapseAllAsync()!;
            ToggleCollapseValue = "Expand all";
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            await ContemporaneousNotesCollapse?.ExpandAllAsync()!;
            ToggleCollapseValue = "Collapse all";
            await InvokeAsync(StateHasChanged);
        }
    }

    // Exhibit mention clicked - open exhibit details in a new tab
    private Task ExhibitMentionClicked(API.Exhibit exhibit)
    {
        return JSRuntime.InvokeVoidAsync("open", $"/case/{CaseId}/exhibit/{exhibit.Id}", "_blank").AsTask();
    }

    // User mention clicked - open user details in a new tab
    private Task UserMentionClicked(API.User user)
    {
        return JSRuntime.InvokeVoidAsync("open", $"/user/{user.EmailAddress}", "_blank").AsTask();
    }
}