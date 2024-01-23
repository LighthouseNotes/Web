using System.Text;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Syncfusion.Blazor.RichTextEditor;

namespace Web.Components.Pages.Case.Personal;

public class ContemporaneousNotesBase : ComponentBase
{
    [Parameter] public required string CaseId { get; set; }

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = default!;

    [Inject] private TokenProvider TokenProvider { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] private IDialogService Dialog { get; set; } = default!;

    [Inject] private IConfiguration Configuration { get; set; } = default!;

    [Inject] private ProtectedLocalStorage ProtectedLocalStore { get; set; } = null!;

    // Contemporaneous Notes Expansion Panel Variables
    private protected static MudExpansionPanels? ContemporaneousNotesCollapse;
    protected string ToggleCollapsed = "Expand all";

    // Contemporaneous Notes
    private Dictionary<DateTime, string>? _allContemporaneousNotes;
    protected Dictionary<DateTime, string>? ContemporaneousNotes;
    protected bool? HasContemporaneousNotes;

    // Date Filter
    protected DateRange FilterDateRange = new(null, null);

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

    // Settings
    protected Models.Settings Settings = new();

    // On parameters set 
    protected override async Task OnParametersSetAsync()
    {
        // Get case details
        SCase = await LighthouseNotesAPIGet.Case(CaseId);

        // Set users from case details
        CaseUsers = SCase.Users.ToList();

        // Get exhibits linked to case
        Exhibits = await LighthouseNotesAPIGet.Exhibits(CaseId);

        // Set image save url
        ImageSaveUrl = $"{Configuration["LighthouseNotesApiUrl"]}/case/{CaseId}/contemporaneous-note/image";

        // Get contemporaneous notes 
        await GetNotes(CaseId);

        // Mark page load as complete 
        PageLoad?.LoadComplete();

        // Re-render component
        await InvokeAsync(StateHasChanged);
    }

    // After page render
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
            ImagePath =
                $"{Settings.S3Endpoint}/cases/{CaseId}/{Settings.UserId}/contemporaneous-notes/images/";

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
    protected async Task OnImageUploadSuccessHandler(ImageSuccessEventArgs args)
    {
        // Get presigned url for image
        string presignedUrl = await LighthouseNotesAPIGet.Image(CaseId, "contemporaneous-note", args.File.Name);

        // Remove s3 endpoint as its already in the image path and set the file name to the filename ame with a presigned string
        args.File.Name = presignedUrl.Replace(ImagePath, "");
    }

    // Image upload failed, so notify the user
    protected void OnImageUploadFailedHandler(ImageFailedEventArgs args)
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
        await LighthouseNotesAPIPost.ContemporaneousNote(CaseId,
            Encoding.UTF8.GetBytes(htmlDoc.DocumentNode.OuterHtml));

        // Refresh notes, empty rich text editor, re-render component, and notify user
        await GetNotes(CaseId);
        RteValue = null;
        await InvokeAsync(StateHasChanged);
        Snackbar.Add("Note has successfully been added!", Severity.Success);
    }

    // Change which notes are being displayed based on the data range
    protected void OnTimeRangeChange(DateRange? dateRange)
    {
        // This function should not be available to be called if ContemporaneousNotes are null but to satisfy null value checks
        if (_allContemporaneousNotes == null) return;

        // If no notes are found then set to null and return
        if (ContemporaneousNotes == null)
        {
            ContemporaneousNotes = _allContemporaneousNotes;
            return;
        }

        // If date range is not set then display all the notes
        if (dateRange == null)
        {
            ContemporaneousNotes = _allContemporaneousNotes;
            return;
        }

        // Set the date range on the page  
        FilterDateRange = dateRange;

        // Create variables for start date and end date 
        DateTime? startDate = dateRange.Start;
        DateTime? endDate = dateRange.End;

        // If either are null then display all the notes 
        if (startDate == null || endDate == null)
            ContemporaneousNotes = _allContemporaneousNotes;

        // Else filter the notes based on the provided start and end dates
        else
            ContemporaneousNotes = _allContemporaneousNotes.Where(e =>
                e.Key >= startDate &&
                e.Key <= endDate).ToDictionary(x => x.Key, x => x.Value);

        // Re-render component
        StateHasChanged();
    }

    // Get notes from s3 bucket, handling errors and fetching images
    private async Task GetNotes(string caseId)
    {
        // Get a list of Contemporaneous Notes
        List<API.ContemporaneousNotes> contemporaneousNotes =
            await LighthouseNotesAPIGet.ContemporaneousNotes(caseId);

        // If list is empty set variables and return
        if (contemporaneousNotes.Count == 0)
        {
            HasContemporaneousNotes = false;
            ContemporaneousNotes = null;

            await InvokeAsync(StateHasChanged);
            return;
        }

        // Empty dictionary
        _allContemporaneousNotes = new Dictionary<DateTime, string>();

        // Loop through each contemporaneous note in the list
        foreach (API.ContemporaneousNotes contemporaneousNote in contemporaneousNotes)
        {
            // Get note content
            string noteContent = await LighthouseNotesAPIGet.ContemporaneousNote(caseId, contemporaneousNote.Id);

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
                    string presignedS3Url = await LighthouseNotesAPIGet.Image(caseId, "contemporaneous-note", fileName);
                    img.Attributes["src"].Value = presignedS3Url;
                }

            // Add contemporaneous note to dictionary
            _allContemporaneousNotes.Add(contemporaneousNote.Created, htmlDoc.DocumentNode.OuterHtml);
        }

        // Set values
        HasContemporaneousNotes = true;
        ContemporaneousNotes = _allContemporaneousNotes;
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

    // Open rich text editor dialog
    protected async Task OpenRichTextEditor()
    {
        // Create dialog parameters
        DialogParameters<ContemporaneousNoteRTEDialog> parameters = new()
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
            await Dialog.ShowAsync<ContemporaneousNoteRTEDialog>("Rich text editor", parameters, options);

        // Await for dialog result
        DialogResult? result = await dialog.Result;

        // If result is not canceled, means note has been added so get notes
        if (!result.Canceled)
        {
            await GetNotes(CaseId);
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