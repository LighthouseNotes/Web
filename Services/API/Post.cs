using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Web.Models.API;

namespace Web.Services.API;

public class LighthouseNotesAPIPost
{
    private readonly HttpClient _http;
    private readonly TokenService _tokenService;

    public LighthouseNotesAPIPost(IHttpClientFactory clientFactory, TokenService tokenService,
        IConfiguration configuration)
    {
        // Create http client
        _http = clientFactory.CreateClient();

        // Set base address with trailing slash
        _http.BaseAddress = new Uri($"{configuration["LighthouseNotesApiUrl"]}/");

        // Set token provider
        _tokenService = tokenService;
    }

    // POST: /user
    public async Task User(AddUser content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            "user");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Add data to request body with JSON type
        request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case/?/?/file
    public async Task<string> File(string caseId, string type, UploadableFile file)
    {
        // Create request
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"case/{caseId}/{type}/file");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Prepare file content
        MultipartFormDataContent multipartContent = new();

        // Create stream content
        MemoryStream memoryStream = new(file.Data);
        StreamContent streamContent = new(memoryStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        // Add stream to form
        multipartContent.Add(streamContent, "file", file.FileName);

        // Attach content to request
        request.Content = multipartContent;

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // Handle errors
        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
            if (response.StatusCode != HttpStatusCode.InternalServerError)
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is not an 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError)
            return System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");

        // Parse response
        Error errorMessage = JsonSerializer.Deserialize<Error>(responseContent, JsonOptions.DefaultOptions) ??
                                    throw new LighthouseNotesErrors.ShouldNotBeNullException();

        // If error message is one about hashes display it
        if (errorMessage.Title == "Could not find hash value for the image!" ||
            errorMessage.Title == "MD5 hash verification failed!" ||
            errorMessage.Title == "SHA256 hash verification failed!")
            return $"/img/image-error.jpeg";

        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

     // POST: /case/?/shared/?/file
    public async Task<string> SharedFile(string caseId, string type, UploadableFile file)
    {
        // Create request
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"case/{caseId}/{type}/file");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Prepare file content
        MultipartFormDataContent multipartContent = new();

        // Create stream content
        MemoryStream memoryStream = new(file.Data);
        StreamContent streamContent = new(memoryStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        // Add stream to form
        multipartContent.Add(streamContent, "file", file.FileName);

        // Attach content to request
        request.Content = multipartContent;

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // Handle errors
        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
            if (response.StatusCode != HttpStatusCode.InternalServerError)
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is not an 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError)
            return System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");

        // Parse response
        Error errorMessage = JsonSerializer.Deserialize<Error>(responseContent, JsonOptions.DefaultOptions) ??
                                    throw new LighthouseNotesErrors.ShouldNotBeNullException();

        // If error message is one about hashes display it
        if (errorMessage.Title == "Could not find hash value for the image!" ||
            errorMessage.Title == "MD5 hash verification failed!" ||
            errorMessage.Title == "SHA256 hash verification failed!")
            return $"/img/image-error.jpeg";

        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case
    public async Task<Case> Case(AddCase content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            "case");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Add data to request body with JSON type
        request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Case>(responseContent, JsonOptions.DefaultOptions) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }


    // POST: /case/?/contemporaneous-notes
    public async Task ContemporaneousNote(string caseId, byte[] tabContent)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/contemporaneous-note");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Create memory stream from bytes
        MemoryStream dataStream = new(tabContent);

        // Create form
        using MultipartFormDataContent content = new();
        content.Add(new StreamContent(dataStream), "file", "note.txt");

        // Add form to request body
        request.Content = content;

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case/?/contemporaneous-notes/search
    public async Task<List<ContemporaneousNotes>> ContemporaneousNotesSearch(string caseId, string searchQuery)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/contemporaneous-notes/search");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Add data to request body with JSON type
        request.Content =
            new StringContent(JsonSerializer.Serialize(new Search { Query = searchQuery }), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ContemporaneousNotes>>(responseContent, JsonOptions.DefaultOptions) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // POST: /case/?/shared/contemporaneous-note
    public async Task SharedContemporaneousNote(string caseId, byte[] tabContent)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/shared/contemporaneous-note");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Create memory stream from bytes
        MemoryStream dataStream = new(tabContent);

        // Create form
        using MultipartFormDataContent content = new();
        content.Add(new StreamContent(dataStream), "file", "Tab.txt");

        // Add form to request body
        request.Content = content;

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case/?/shared/contemporaneous-notes/search
    public async Task<List<SharedContemporaneousNotes>> SharedContemporaneousNotesSearch(string caseId,
        string searchQuery)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/shared/contemporaneous-notes/search");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Add data to request body with JSON type
        request.Content =
            new StringContent(JsonSerializer.Serialize(new Search { Query = searchQuery }), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<SharedContemporaneousNotes>>(responseContent, JsonOptions.DefaultOptions) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // POST: /case/?/tab
    public async Task Tab(string name, string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/tab");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Add data to request body with JSON type
        request.Content = new StringContent(JsonSerializer.Serialize(new AddTab { Name = name }), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case/?/tab
    public async Task SharedTab(string name, string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/shared/tab");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Add data to request body with JSON type
        request.Content = new StringContent(JsonSerializer.Serialize(new AddTab { Name = name }), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case/?/tab/?/content
    public async Task TabContent(string caseId, string tabId, byte[] tabContent)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"/case/{caseId}/tab/{tabId}/content");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Create memory stream from bytes
        MemoryStream dataStream = new(tabContent);

        // Create form
        using MultipartFormDataContent content = new();
        content.Add(new StreamContent(dataStream), "file", "Tab.txt");

        // Add form to request body
        request.Content = content;

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case/?/shared/tab/?/content
    public async Task SharedTabContent(string caseId, string tabId, byte[] tabContent)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"/case/{caseId}/shared/tab/{tabId}/content");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Create memory stream from bytes
        MemoryStream dataStream = new(tabContent);

        // Create form
        using MultipartFormDataContent content = new();
        content.Add(new StreamContent(dataStream), "file", "Tab.txt");

        // Add form to request body
        request.Content = content;

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // POST: /case/?/exhibit
    public async Task<Exhibit> Exhibit(string caseId, AddExhibit content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/exhibit");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Add data to request body with JSON type
        request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Exhibit>(responseContent, JsonOptions.DefaultOptions) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }
}