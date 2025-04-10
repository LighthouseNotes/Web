using System.Text;
using System.Text.Json;
using System.Web;
using HtmlAgilityPack;
using Web.Models.API;

namespace Web.Components.Dialogs.Shared;

public class SharedTabTextEditorDialogBase : ComponentBase
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

    // Component parameters and dependency injection
    [CascadingParameter] private MudDialog? MudDialog { get; set; }

    [Parameter] public required string CaseId { get; set; }

    [Parameter] public required string TabId { get; set; }

    [Parameter] public Case? SCase { get; set; }

    [Parameter] public required List<User> CaseUsers { get; set; }

    [Parameter] public required List<Exhibit> Exhibits { get; set; }

    [Parameter] public (bool?, HtmlNode?) TabContent { get; set; }

    [Inject] private LighthouseNotesAPIPost LighthouseNotesAPIPost { get; set; } = null!;

    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    // Lifecycle method triggered when parameters are set or changed - set modules
    protected override async Task OnParametersSetAsync()
    {
        // Set mention modules
        Modules =
        [
            new QuillMentionModule<User>(
                    (_, s) => Task.FromResult(CaseUsers.Where(u =>
                        u.DisplayName.Contains(s, StringComparison.InvariantCultureIgnoreCase))), '@')
                .SetProperties(m => m.MentionClicked = UserMentionClicked),
            new QuillMentionModule<Exhibit>(
                    (_, s) => Task.FromResult(Exhibits.Where(e =>
                        e.Reference.Contains(s, StringComparison.InvariantCultureIgnoreCase))), '#')
                .SetProperties(m => m.MentionClicked = ExhibitMentionClicked)
        ];

        // Loading
        if (TabContent.Item1 == null && TabContent.Item2 == null)
        {
            await TextEditor.SetHtml(
                "<div Class=\"d-flex justify-center\">\n<MudProgressCircular Color=\"Color.Primary\" Indeterminate=\"true\" Size=\"Size.Medium\"/>\n</div>");
        }
        // No content
        else if (TabContent is { Item1: false, Item2: null })
        {
            await TextEditor.SetHtml("<p> No content </p>");
        }
        // Display content
        else if (TabContent is { Item1: true, Item2: not null })
        {
            await TextEditor.InsertHtmlAsync(TabContent.Item2.OuterHtml);
        }

        await InvokeAsync(StateHasChanged);
    }

    // Exit button clicked
    protected void Cancel()
    {
        MudDialog?.CloseAsync();
    }


    // On upload - use API to upload the image and return presigned url
    protected async Task<string> OnUpload(UploadableFile arg)
    {
        return await LighthouseNotesAPIPost.SharedFile(CaseId, "tab", arg);
    }

    // Save content - Save rich text content to s3 bucket
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
                        User user = JsonSerializer.Deserialize<User>(
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
                        Exhibit exhibit = JsonSerializer.Deserialize<Exhibit>(
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

        // Update tab content
        await LighthouseNotesAPIPost.SharedTabContent(CaseId, TabId,
            Encoding.UTF8.GetBytes(htmlDoc.DocumentNode.OuterHtml));

        // Refresh notes, empty rich text editor, re-render component, and notify user
        await TextEditor.SetHtml(null);
        Snackbar.Add("Tab content has successfully been updated!", Severity.Success);
        MudDialog?.CloseAsync(DialogResult.Ok(true));
    }

    // Exhibit mention clicked - open exhibit details in a new tab
    private Task ExhibitMentionClicked(Exhibit exhibit)
    {
        return JSRuntime.InvokeVoidAsync("open", $"/case/{CaseId}/exhibit/{exhibit.Id}", "_blank").AsTask();
    }

    // User mention clicked - open user details in a new tab
    private Task UserMentionClicked(User user)
    {
        return JSRuntime.InvokeVoidAsync("open", $"/user/{user.EmailAddress}", "_blank").AsTask();
    }
}