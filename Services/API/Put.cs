using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Web.Models.API;

namespace Web.Services.API;

public class LighthouseNotesAPIPut
{
    private readonly HttpClient _http;
    private readonly TokenProvider _tokenProvider;

    public LighthouseNotesAPIPut(IHttpClientFactory clientFactory, TokenProvider tokenProvider,
        IConfiguration configuration)
    {
        // Create http client
        _http = clientFactory.CreateClient();

        // Set base address with trailing slash
        _http.BaseAddress = new Uri($"{configuration["LighthouseNotesApiUrl"]}/");

        // Set token provider
        _tokenProvider = tokenProvider;
    }

    // PUT: /case/?
    public async Task Case(string caseId, UpdateCase content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Put,
            $"case/{caseId}");

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

    // PUT: /user/?
    public async Task User(UpdateUser content, string userId = "")
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Put,
            $"user/{userId}");

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

    // PUT: /organization/config
    public async Task Config(OrganizationSettings content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Put,
            "organization/config");

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

    // PUT: /user/settings
    public async Task UserSettings(UserSettings content)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Put,
            "user/settings");

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

    // Put: /case/?/user?
    public async Task CaseUser(string caseId, string userId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Put, $"/case/{caseId}/user/{userId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }
}