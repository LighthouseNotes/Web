using System.Text;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using Syncfusion.Blazor.RichTextEditor;

namespace Web.Components.Pages.Case.Shared;

public class SharedContemporaneousNotesBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = default!;

    [Inject] private TokenProvider TokenProvider { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] private IDialogService Dialog { get; set; } = default!;

    [Inject] private IConfiguration Configuration { get; set; } = default!;

    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;


    // Contemporaneous Notes Expansion Panel Variables
    private protected static MudExpansionPanels? ContemporaneousNotesCollapse;
    protected string ToggleCollapsed = "Expand all";

    // Contemporaneous Notes
    private  List<SharedContemporaneousNote>? _contemporaneousNotes; // Default contemporaneous notes with no filtering
    protected List<SharedContemporaneousNote>? ContemporaneousNotes; // Contemporaneous notes displayed on the page
    protected bool? HasContemporaneousNotes;

    // Date Filter and search query
    protected DateRange FilterDateRange = new(null, null);
    protected string SearchQuery = "";

    // Images
    protected string ImageSaveUrl = null!;
    protected string ImagePath = null!;

    // Page load element 
    protected PageLoad? PageLoad;

    // RichText Editor Variables
    protected string? RteValue;
    protected string ToggleUserMention = "Exhibit mention";

    // API Objects
    protected API.Case SCase = null!;
    protected List<API.User> CaseUsers = new();
    protected List<API.Exhibit> Exhibits = new();
    private List<API.SharedContemporaneousNotes> _allContemporaneousNotes = new(); // List of all contemporaneous notes with note id and created date

    // Settings
    protected Models.Settings Settings = new();
    
    // Pagination
    private int _page = 1;
    protected int PageSize = 10;
    protected int Pages = 1;

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // Get case details
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Set users from case details
        CaseUsers = SCase.Users.ToList();

        // Get exhibits linked to case
        (API.Pagination, List<API.Exhibit>?) exhibitsWithPagination = await LighthouseNotesAPIGet.Exhibits(CaseId, 1, 0);
        Exhibits = exhibitsWithPagination.Item2!;

        // Set image save url
        ImageSaveUrl = $"{Configuration["LighthouseNotesApiUrl"]}/case/{CaseId}/shared/contemporaneous-note/image";

        // Get contemporaneous notes 
        await GetNotes();

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
            // Use the setting service to retrieve the settings
            Settings = await SettingsService.Get();
            
            // Set Image Path
            ImagePath =
                $"{Settings.S3Endpoint}/cases/{CaseId}/shared/contemporaneous-notes/images/";

            // Re-render component
            await InvokeAsync(StateHasChanged);
        }
    }

    // Add authorization header before image upload request
    protected void BeforeImageUpload(ImageUploadingEventArgs args)
    {
        // Add bearer token to request
        args.CurrentRequest = new List<object> { new { Authorization = $"Bearer {TokenProvider.AccessToken}" } };
    }

    // Add presigned string to image name
    protected async Task ImageUploadSuccess(ImageSuccessEventArgs args)
    {
        // Get presigned url for image
        string presignedUrl = await LighthouseNotesAPIGet.SharedImage(CaseId, "contemporaneous-note", args.File.Name);

        // Remove s3 endpoint as its already in the image path and set the file name to the filename ame with a presigned string
        args.File.Name = presignedUrl.Replace(ImagePath, "");
    }

    // Image upload failed, so notify the user
    protected void ImageUploadFailed(ImageFailedEventArgs args)
    {
        Snackbar.Add(
            args.Response.StatusCode == 409
                ? $"An image with the file name `{args.File.Name}` already exists!"
                : "Image upload failed!", Severity.Error);
    }

    // Save rich text content to s3 bucket
    protected async Task SaveContent()
    {
        // If no content is provided show and error and return
        if (RteValue == null)
        {
            Snackbar.Add("You have not added any content into the rich text editor, please add content and try again.",
                Severity.Error);
            return;
        }

        // Create HTML document
        HtmlDocument htmlDoc = new();

        // Load value into HTML
        htmlDoc.LoadHtml(RteValue);

        // If content contains images
        if (htmlDoc.DocumentNode.SelectNodes("//img") != null)

            // For each image that starts with S3 images path
            foreach (HtmlNode img in htmlDoc.DocumentNode.SelectNodes("//img")
                         .Where(u => u.Attributes["src"].Value.Contains(ImagePath)))
            {
                // Create uri from image src
                Uri uri = new(img.Attributes["src"].Value);

                // Remove query string from url
                string fixedUri = uri.AbsoluteUri.Replace(uri.Query, string.Empty);

                // Set src from s3url/100.jpg to .path/100.jpg 
                img.Attributes["src"].Value = $".path/{Path.GetFileName(fixedUri)}";
            }

        // Add Contemporaneous Note
        await LighthouseNotesAPIPost.SharedContemporaneousNote(CaseId,
            Encoding.UTF8.GetBytes(htmlDoc.DocumentNode.OuterHtml));

        // Refresh notes, empty rich text editor, re-render component, and notify user
        await GetNotes();
        RteValue = null;
        await InvokeAsync(StateHasChanged);
        Snackbar.Add("Note has successfully been added!", Severity.Success);
    }

    // Change which notes are being displayed based on the data range
    protected async Task TimeRangeChanged(DateRange? dateRange)
    {
        // This function should not be available to be called if ContemporaneousNotes are null but to satisfy null value checks
        if (_contemporaneousNotes == null) return;

        // If no notes are found then set to null and return
        if (ContemporaneousNotes == null)
        {
            ContemporaneousNotes = _contemporaneousNotes;
            return;
        }

        // If date range is not set then display all the notes
        if (dateRange == null)
        {
            ContemporaneousNotes = _contemporaneousNotes;
            return;
        }

        // Set the date range on the page  
        FilterDateRange = dateRange;

        await PaginateNotes();
    }
    
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

        List<API.SharedContemporaneousNotes> contemporaneousNotes = await LighthouseNotesAPIPost.SharedContemporaneousNotesSearch(CaseId, SearchQuery);
        
        // If list is empty set variables and return
        if (contemporaneousNotes.Count == 0)
        {
            ContemporaneousNotes = _contemporaneousNotes;
            Snackbar.Add("Your search returned no results. Showing all contemporaneous notes.", Severity.Warning);

            await InvokeAsync(StateHasChanged);
            return;
        }

        await PaginateNotes();
    }

    // Page size select change event
    protected async Task PageSizeChanged(int pageSize)
    {
        // Update page size variable 
        PageSize = pageSize;
        
        // Re Calculate pages
        Pages = (_allContemporaneousNotes.Count + PageSize - 1) / PageSize;
        
        // Paginate notes
        await PaginateNotes();
    }

    // Page changed event
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
        _allContemporaneousNotes =  await LighthouseNotesAPIGet.SharedContemporaneousNotes(CaseId);
        
        // If list is empty set variables and return
        if ( _allContemporaneousNotes.Count == 0)
        {
            HasContemporaneousNotes = false;
            ContemporaneousNotes = null;

            await InvokeAsync(StateHasChanged);
            return;
        }

        // Calculate pages
        Pages = ( _allContemporaneousNotes.Count + PageSize - 1) / PageSize;
        
        await PaginateNotes();
    }
    
    // Get notes from s3 bucket, handling errors and fetching images
    private async Task PaginateNotes()
    {
        // Empty list
        ContemporaneousNotes = new List<SharedContemporaneousNote>();

        // If filter date range is set, then use it
        if (FilterDateRange is { Start: not null, End: not null })
        {
            // If no notes match the date filter, then show notes without filter and notify user
            if (!_allContemporaneousNotes.Any(cn => cn.Created >= FilterDateRange.Start && cn.Created <= FilterDateRange.End))
            {
                return;
            }
            
            // Loop through each contemporaneous note in the list
            foreach (API.SharedContemporaneousNotes contemporaneousNote in _allContemporaneousNotes.Where(cn => cn.Created >= FilterDateRange.Start && cn.Created <= FilterDateRange.End).Skip((_page - 1) * PageSize).Take(PageSize))
            {
                // Get note content
                string noteContent = await LighthouseNotesAPIGet.SharedContemporaneousNote(CaseId, contemporaneousNote.Id);

                // Create HTML document
                HtmlDocument htmlDoc = new();

                // Load ContemporaneousNotes Content to HTML
                htmlDoc.LoadHtml(noteContent);

                // If content contains images
                if (htmlDoc.DocumentNode.SelectNodes("//img") != null)
                    // For each image that starts with .path/
                    foreach (HtmlNode img in htmlDoc.DocumentNode.SelectNodes("//img")
                                 .Where(u => u.Attributes["src"].Value.Contains(".path/")))
                    {
                        // Create variable with file name
                        string fileName = img.Attributes["src"].Value.Replace(".path/", "");

                        // Get presigned s3 url and update image src 
                        string presignedS3Url = await LighthouseNotesAPIGet.SharedImage(CaseId, "contemporaneous-note", fileName);
                        img.Attributes["src"].Value = presignedS3Url;
                    }

                // Add contemporaneous note to dictionary
                ContemporaneousNotes.Add(new SharedContemporaneousNote
                {
                    Content = htmlDoc.DocumentNode.OuterHtml,
                    Created = contemporaneousNote.Created,
                    Creator = contemporaneousNote.Creator
                });
            }
            
            await InvokeAsync(StateHasChanged);
            return;
        }

        // If search query is set then use it
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            List<API.SharedContemporaneousNotes> contemporaneousNotes = await LighthouseNotesAPIPost.SharedContemporaneousNotesSearch(CaseId, SearchQuery);
        
            // If no notes match the search, then show notes without filter and notify user
            if (contemporaneousNotes.Count == 0)
            { 
                return;
            }
            
            // Re Calculate pages
            Pages = (contemporaneousNotes.Count + PageSize - 1) / PageSize;

            // Loop through each contemporaneous note in the list
            foreach (API.SharedContemporaneousNotes contemporaneousNote in contemporaneousNotes.Skip((_page - 1) * PageSize).Take(PageSize))
            {
                // Get note content
                string noteContent = await LighthouseNotesAPIGet.SharedContemporaneousNote(CaseId, contemporaneousNote.Id);

                // Create HTML document
                HtmlDocument htmlDoc = new();

                // Load ContemporaneousNotes Content to HTML
                htmlDoc.LoadHtml(noteContent);

                // If content contains images
                if (htmlDoc.DocumentNode.SelectNodes("//img") != null)
                    // For each image that starts with .path/
                    foreach (HtmlNode img in htmlDoc.DocumentNode.SelectNodes("//img")
                                 .Where(u => u.Attributes["src"].Value.Contains(".path/")))
                    {
                        // Create variable with file name
                        string fileName = img.Attributes["src"].Value.Replace(".path/", "");

                        // Get presigned s3 url and update image src 
                        string presignedS3Url = await LighthouseNotesAPIGet.SharedImage(CaseId, "contemporaneous-note", fileName);
                        img.Attributes["src"].Value = presignedS3Url;
                    }

                // Add contemporaneous note to dictionary
                ContemporaneousNotes.Add(new SharedContemporaneousNote
                {
                    Content = htmlDoc.DocumentNode.OuterHtml,
                    Created = contemporaneousNote.Created,
                    Creator = contemporaneousNote.Creator
                });
            }
            
            await InvokeAsync(StateHasChanged);
            return;
        }

        _contemporaneousNotes = new List<SharedContemporaneousNote>();
        
        // Loop through each contemporaneous note in the list
        foreach (API.SharedContemporaneousNotes contemporaneousNote in _allContemporaneousNotes.Skip((_page - 1) * PageSize).Take(PageSize))
        {
            // Get note content
            string noteContent = await LighthouseNotesAPIGet.SharedContemporaneousNote(CaseId, contemporaneousNote.Id);

            // Create HTML document
            HtmlDocument htmlDoc = new();

            // Load ContemporaneousNotes Content to HTML
            htmlDoc.LoadHtml(noteContent);

            // If content contains images
            if (htmlDoc.DocumentNode.SelectNodes("//img") != null)
                // For each image that starts with .path/
                foreach (HtmlNode img in htmlDoc.DocumentNode.SelectNodes("//img")
                             .Where(u => u.Attributes["src"].Value.Contains(".path/")))
                {
                    // Create variable with file name
                    string fileName = img.Attributes["src"].Value.Replace(".path/", "");

                    // Get presigned s3 url and update image src 
                    string presignedS3Url = await LighthouseNotesAPIGet.SharedImage(CaseId, "contemporaneous-note", fileName);
                    img.Attributes["src"].Value = presignedS3Url;
                }

            // Add contemporaneous note to dictionary
            _contemporaneousNotes.Add(new SharedContemporaneousNote
            {
                Content = htmlDoc.DocumentNode.OuterHtml,
                Created = contemporaneousNote.Created,
                Creator = contemporaneousNote.Creator
            });
        }

        // Set values
        HasContemporaneousNotes = true;
        ContemporaneousNotes = _contemporaneousNotes;
    }

    // Block user navigation if rich text editor contains content
    protected async Task BeforeInternalNavigation(LocationChangingContext context)
    {
        //If RTEValue empty allow navigation
        if (RteValue == "<p><br/></p>" || string.IsNullOrWhiteSpace(RteValue))
            return;

        // RTEValue is not empty, display a modal
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

    protected void UserClick(string userId)
    {
        NavigationManager.NavigateTo($"/user/{userId}", true);
    }

    // Open rich text editor dialog
    protected async Task OpenRichTextEditor()
    {
        // Create dialog parameters
        DialogParameters<SharedContemporaneousNoteRTEDialog> parameters = new()
        {
            { p => p.CaseId, CaseId },
            { p => p.SCase, SCase },
            { p => p.CaseUsers, CaseUsers },
            { p => p.Exhibits, Exhibits },
            { p => p.ImageSaveUrl, ImageSaveUrl },
            { p => p.ImagePath, ImagePath }
        };

        // Set dialog options
        DialogOptions options = new() { FullScreen = true, CloseButton = true };

        // Show dialog
        IDialogReference? dialog =
            await Dialog.ShowAsync<SharedContemporaneousNoteRTEDialog>("Rich text editor", parameters, options);

        // Await for dialog result
        DialogResult? result = await dialog.Result;

        // If result is not canceled, means note has been added so get notes
        if (!result.Canceled)
        {
            await GetNotes();
            await InvokeAsync(StateHasChanged);
        }
    }

    // Toggle collapsable panels containing notes converter
    protected class ToggleCollapseConverter : BoolConverter<string>
    {
        private const string TrueString = "Collapse all";
        private const string FalseString = "Expand all";
        private const string NullString = "Unknown";

        public ToggleCollapseConverter()
        {
            SetFunc = OnSet;
            GetFunc = OnGet;
        }

        private string OnGet(bool? value)
        {
            try
            {
                return value == true ? TrueString : FalseString;
            }
            catch (Exception e)
            {
                UpdateGetError("Conversion error: " + e.Message);
                return NullString;
            }
        }

        private bool? OnSet(string? arg)
        {
            if (arg == null)
                return null;
            try
            {
                switch (arg)
                {
                    case TrueString:
                        ContemporaneousNotesCollapse?.ExpandAll();
                        return true;
                    case FalseString:
                        ContemporaneousNotesCollapse?.CollapseAll();
                        return false;
                    default:
                        return null;
                }
            }
            catch (FormatException e)
            {
                UpdateSetError("Conversion error: " + e.Message);
                return null;
            }
        }
    }

    // Toggle user / exhibit mention converter
    protected class ToggleUserMentionConverter : BoolConverter<string>
    {
        private const string TrueString = "User mention";
        private const string FalseString = "Exhibit mention";
        private const string NullString = "Unknown";

        public ToggleUserMentionConverter()
        {
            SetFunc = OnSet;
            GetFunc = OnGet;
        }

        private string OnGet(bool? value)
        {
            try
            {
                return value == true ? TrueString : FalseString;
            }
            catch (Exception e)
            {
                UpdateGetError("Conversion error: " + e.Message);
                return NullString;
            }
        }

        private bool? OnSet(string? arg)
        {
            if (arg == null)
                return null;
            try
            {
                return arg switch
                {
                    TrueString => true,
                    FalseString => false,
                    _ => null
                };
            }
            catch (FormatException e)
            {
                UpdateSetError("Conversion error: " + e.Message);
                return null;
            }
        }
    }
}

public class SharedContemporaneousNote
{
    public required DateTime Created { get; init; }
    public required string Content { get; init; }
    public required API.User Creator { get; init; }
}