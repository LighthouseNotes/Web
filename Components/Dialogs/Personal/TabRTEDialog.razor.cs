using System.Text;
using HtmlAgilityPack;
using MudBlazor;
using Syncfusion.Blazor.RichTextEditor;
using Web.Models.API;

namespace Web.Components.Dialogs.Personal;

public class TabRteDialogBase : ComponentBase
{
    [CascadingParameter] private MudDialogInstance? MudDialog { get; set; }

    [Parameter] public required string CaseId { get; set; }

    [Parameter] public required string TabId { get; set; }

    [Parameter] public Case? SCase { get; set; }

    [Parameter] public required List<User> CaseUsers { get; set; }

    [Parameter] public required List<Exhibit> Exhibits { get; set; }

    [Parameter] public required string ImageSaveUrl { get; set; }

    [Parameter] public required string ImagePath { get; set; }

    [Parameter] public (bool?, HtmlNode?) TabContent { get; set; }

    [Inject] private LighthouseNotesAPIGet LighthouseNotesAPIGet { get; set; } = default!;

    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = default!;

    [Inject] private TokenProvider TokenProvider { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // RichText Editor Variables
    protected string? RteValue;
    protected string ToggleUserMention = "Exhibit mention";

    // Exit button clicked
    protected void Cancel()
    {
        MudDialog?.Cancel();
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
        string presignedUrl = await LighthouseNotesAPIGet.Image(CaseId, "tab", args.File.Name);

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

        // Update tab content
        await LighthouseNotesAPIPost.TabContent(CaseId, TabId, Encoding.UTF8.GetBytes(htmlDoc.DocumentNode.OuterHtml));

        // Refresh notes, empty rich text editor, re-render component, and notify user
        RteValue = null;
        Snackbar.Add("Tab content has successfully been updated!", Severity.Success);
        MudDialog?.Close(DialogResult.Ok(true));
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