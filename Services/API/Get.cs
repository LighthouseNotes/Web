using System.Text.Json;
using Web.Models.API;

namespace Web.Services.API;

public class LighthouseNotesAPIGet
{
    private readonly HttpClient _http;
    private readonly TokenProvider _tokenProvider;

    public LighthouseNotesAPIGet(IHttpClientFactory clientFactory, TokenProvider tokenProvider,
        IConfiguration configuration)
    {
        // Create http client
        _http = clientFactory.CreateClient();

        // Set base address with trailing slash
        _http.BaseAddress = new Uri($"{configuration["LighthouseNotesApiUrl"]}/");

        // Set token provider
        _tokenProvider = tokenProvider;
    }
    //////////////////
    // Organization //
    //////////////////

    // GET: /organization/config
    public async Task<OrganizationSettings> OrganizationSettings()
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, "/organization/settings");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OrganizationSettings>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    ///////////
    // User //
    //////////

    // GET: /users
    public async Task<List<User>> Users()
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, "users");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<User>>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /user/?
    public async Task<User> User(string userId = "")
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"user/{userId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<User>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /user/?/settings
    public async Task<Settings?> UserSettings()
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, "user/settings");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code return null
        if (!response.IsSuccessStatusCode)
            return null;

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Settings>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    ///////////
    // Case //
    //////////

    // GET: /cases
    public async Task<List<Case>?> Cases()
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, "cases");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request 
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Case>>(responseContent);
    }

    // GET: /case/?
    public async Task<Case> Case(string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"case/{caseId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Case>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    ///////////////////////////
    // Contemporaneous Note //
    //////////////////////////

    // GET:  /case/?/contemporaneous-notes
    public async Task<List<ContemporaneousNotes>> ContemporaneousNotes(string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"case/{caseId}/contemporaneous-notes");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
        
        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ContemporaneousNotes>>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /case/?/contemporaneous-note/?
    public async Task<string> ContemporaneousNote(string caseId, string noteId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"case/{caseId}/contemporaneous-note/{noteId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return
        string responseContent = await response.Content.ReadAsStringAsync();
        return responseContent;
    }

    //////////////////////////////////
    // Shared Contemporaneous Note //
    /////////////////////////////////

    // GET:  /case/?/shared/contemporaneous-notes
    public async Task<List<SharedContemporaneousNotes>> SharedContemporaneousNotes(string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"case/{caseId}/shared/contemporaneous-notes");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<SharedContemporaneousNotes>>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /case/?/shared/contemporaneous-note/?
    public async Task<string> SharedContemporaneousNote(string caseId, string noteId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"case/{caseId}/shared/contemporaneous-note/{noteId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return
        string responseContent = await response.Content.ReadAsStringAsync();
        return responseContent;
    }


    //////////
    // Tabs //
    //////////

    // GET: /case/?/tabs
    public async Task<List<Tab>> Tabs(string? caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get,
            $"case/{caseId}/tabs");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Tab>>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /case/?/tab/?
    public async Task<Tab> Tab(string caseId, string tabId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get,
            $"/case/{caseId}/tab/{tabId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and ensure response
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Tab>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /case/?/tab/?/content
    public async Task<(bool, string)> TabContent(string caseId, string tabId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/case/{caseId}/tab/{tabId}/content");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and read response
        HttpResponseMessage response = await _http.SendAsync(request);
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is successful return (true, response) else return (false, response)
        return response.IsSuccessStatusCode ? (true, responseContent) : (false, responseContent);
    }

    /////////////////
    // Shared Tabs //
    /////////////////

    // GET: /case/?/shared/tabs
    public async Task<List<SharedTab>?> SharedTabs(string? caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get,
            $"case/{caseId}/shared/tabs");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and ensure response
        HttpResponseMessage response = await _http.SendAsync(request);
        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<SharedTab>>(responseContent);
    }

    // GET: /case/?/shared/tab/?
    public async Task<SharedTab> SharedTab(string caseId, string tabId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get,
            $"/case/{caseId}/shared/tab/{tabId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and ensure response
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SharedTab>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /case/?/shared/tab/?/content
    public async Task<(bool, string)> SharedTabContent(string caseId, string tabId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/case/{caseId}/shared/tab/{tabId}/content");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and read response
        HttpResponseMessage response = await _http.SendAsync(request);
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is successful return (true, response) else return (false, response)
        return response.IsSuccessStatusCode ? (true, responseContent) : (false, responseContent);
    }
    ///////////
    // Image //
    ///////////

    // GET: /case/?/?/image
    public async Task<string> Image(string caseId, string type, string fileName)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/case/{caseId}/{type}/image/{fileName}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return unescaped string
        string responseContent = await response.Content.ReadAsStringAsync();
        return System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
    }

    // GET: /case/?/shared/?/image
    public async Task<string> SharedImage(string caseId, string type, string fileName)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/case/{caseId}/shared/{type}/image/{fileName}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return unescaped string
        string responseContent = await response.Content.ReadAsStringAsync();
        return System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
    }

    //////////////
    // Exhibits //
    //////////////

    // GET: /case/?/exhibits
    public async Task<List<Exhibit>> Exhibits(string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/case/{caseId}/exhibits");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Exhibit>>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    // GET: /case/?/exhibit/?
    public async Task<Exhibit> Exhibit(string caseId, string exhibitId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/case/{caseId}/exhibit/{exhibitId}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and ensure response
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Exhibit>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    ////////////
    // Export //
    ////////////
    // GET: /case/?/export
    public async Task<string> Export(string caseId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/case/{caseId}/export");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return unescaped string
        string responseContent = await response.Content.ReadAsStringAsync();
        return System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
    }

    ///////////
    // Audit //
    ///////////
    // GET: /audit/user
    public async Task<List<UserAudit>> UserAudit()
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, "/audit/user");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and ensure response
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<UserAudit>>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }
}