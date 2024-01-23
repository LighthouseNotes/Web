using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Web.Models.API;

namespace Web.Services.API;

public class LighthouseNotesAPIPost
{
    private readonly HttpClient _http;
    private readonly TokenProvider _tokenProvider;

    public LighthouseNotesAPIPost(IHttpClientFactory clientFactory, TokenProvider tokenProvider,
        IConfiguration configuration)
    {
        // Create http client
        _http = clientFactory.CreateClient();

        // Set base address with trailing slash
        _http.BaseAddress = new Uri($"{configuration["LighthouseNotesApiUrl"]}/");

        // Set token provider
        _tokenProvider = tokenProvider;
    }

    // POST: /user
    public async Task User(AddUser content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            "user");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
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

    // POST: /case
    public async Task Case(AddCase content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            "case");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
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


    // POST: /case/?/contemporaneous-notes
    public async Task ContemporaneousNote(string caseId, byte[] tabContent)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/contemporaneous-note");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
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

    // POST: /case/?/shared/contemporaneous-note
    public async Task SharedContemporaneousNote(string caseId, byte[] tabContent)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/shared/contemporaneous-note");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
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

    // POST: /case/?/tab
    public async Task Tab(string name, string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Post,
            $"case/{caseId}/tab");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
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
        string? token = _tokenProvider.AccessToken;
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
        string? token = _tokenProvider.AccessToken;
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
        string? token = _tokenProvider.AccessToken;
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
        string? token = _tokenProvider.AccessToken;
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
        return JsonSerializer.Deserialize<Exhibit>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }
}