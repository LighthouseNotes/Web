namespace Web.Services.API;

public class LighthouseNotesAPIDelete
{
    private readonly HttpClient _http;
    private readonly TokenService _tokenService;

    public LighthouseNotesAPIDelete(IHttpClientFactory clientFactory, TokenService tokenService,
        IConfiguration configuration)
    {
        // Create http client
        _http = clientFactory.CreateClient();

        // Set base address with trailing slash
        _http.BaseAddress = new Uri($"{configuration["LighthouseNotesApiUrl"]}/");

        // Set token provider
        _tokenService = tokenService;
    }

    // DELETE: /user/?
    public async Task User(string userId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Delete, $"/user/{userId}");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    // DELETE: /case/?/user?
    public async Task CaseUser(string caseId, string userId)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Delete, $"/case/{caseId}/user/{userId}");

        // Add Bearer token
        string? token = _tokenService.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }
}