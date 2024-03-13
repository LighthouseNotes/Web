using System.Net;
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
    public async Task<(Pagination ,List<User>)> Users(int page = 1, int pageSize = 10, string sort = "", bool sio = false, string search = "")
    {
        
        // Create request
        HttpRequestMessage request;
            
        if (sio)
        {
            request = new HttpRequestMessage(HttpMethod.Get, $"users?sio=true");
        }
        else if(!string.IsNullOrWhiteSpace(sort))
        {
            request = new HttpRequestMessage(HttpMethod.Get, $"users?page={page}&pageSize={pageSize}&sort={sort}");
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            request = new HttpRequestMessage(HttpMethod.Get, $"users?page={page}&pageSize={pageSize}&search={search}");
        }
        else
        {
            request = new HttpRequestMessage(HttpMethod.Get, $"users?page={page}&pageSize={pageSize}");
        }

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        Pagination pagination = new()
        {
            Page = int.Parse(response.Headers.NonValidated["X-Page"].ToString()),
            TotalPages = int.Parse(response.Headers.NonValidated["X-Total-Pages"].ToString()),
            Total = int.Parse(response.Headers.NonValidated["X-Total-Count"].ToString()),
        };
        
        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return (pagination, JsonSerializer.Deserialize<List<User>>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException());
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

        // If response is 404
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
        {
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
        }

        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Settings>(responseContent) ??
               throw new LighthouseNotesErrors.ShouldNotBeNullException();
    }

    ///////////
    // Case //
    //////////

    // GET: /cases
    public async Task<(Pagination, List<Case>?)> Cases(int page = 1, int pageSize = 10, string sort = "", string search = "")
    {
        // Create request
        HttpRequestMessage request;
            
        if(!string.IsNullOrWhiteSpace(sort))
        {
            request = new HttpRequestMessage(HttpMethod.Get, $"cases?page={page}&pageSize={pageSize}&sort={sort}");
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            request = new HttpRequestMessage(HttpMethod.Get, $"cases?page={page}&pageSize={pageSize}&search={search}");
        }
        else
        {
            request = new HttpRequestMessage(HttpMethod.Get, $"cases?page={page}&pageSize={pageSize}");
        }
        
        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request 
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        Pagination pagination = new()
        {
            Page = int.Parse(response.Headers.NonValidated["X-Page"].ToString()),
            TotalPages = int.Parse(response.Headers.NonValidated["X-Total-Pages"].ToString()),
            Total = int.Parse(response.Headers.NonValidated["X-Total-Count"].ToString()),
        };
        
        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return (pagination, JsonSerializer.Deserialize<List<Case>>(responseContent));
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

        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
            }
        }

        // Read response
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is not a a 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError) 
            return responseContent;
        
        // Parse response
        Models.Error errorMessage = JsonSerializer.Deserialize<Models.Error>(responseContent) ?? throw new LighthouseNotesErrors.ShouldNotBeNullException();
        
        // If error message is one about hashes display it
        if(errorMessage.Title == "Could not find hash value for contemporaneous note!" || errorMessage.Title == "MD5 hash verification failed!" || errorMessage.Title == "SHA256 hash verification failed!")
                return $"<span style=color:red> {errorMessage.Detail!} </span>";
            
        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
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

        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
            }
        }

        // Read response
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is not a a 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError) 
            return responseContent;
        
        // Parse response
        Models.Error errorMessage = JsonSerializer.Deserialize<Models.Error>(responseContent) ?? throw new LighthouseNotesErrors.ShouldNotBeNullException();
        
        // If error message is one about hashes display it
        if(errorMessage.Title == "Could not find hash value for shared contemporaneous note!" || errorMessage.Title == "MD5 hash verification failed!" || errorMessage.Title == "SHA256 hash verification failed!")
            return $"<span style=color:red> {errorMessage.Detail!} </span>";
            
        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
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

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);
        
        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
            }
        }

        // Read response
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is not a a 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError) 
            return  (true, responseContent);
        
        // Parse response
        Models.Error errorMessage = JsonSerializer.Deserialize<Models.Error>(responseContent) ?? throw new LighthouseNotesErrors.ShouldNotBeNullException();
        
        // If error message is one about hashes display it
        if (errorMessage.Title == "Can not find the S3 object for the tab!")
            return (false, responseContent);
            
        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
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

        // Send request
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

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);
        
        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
            }
        }

        // Read response
        string responseContent = await response.Content.ReadAsStringAsync();

        // If response is not a a 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError) 
            return  (true, responseContent);
        
        // Parse response
        Models.Error errorMessage = JsonSerializer.Deserialize<Models.Error>(responseContent) ?? throw new LighthouseNotesErrors.ShouldNotBeNullException();
        
        // If error message is one about hashes display it
        if (errorMessage.Title == "Can not find the S3 object for the shared tab!")
            return (false, responseContent);
            
        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
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

        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
            }
        }

        // Read response 
        string responseContent = await response.Content.ReadAsStringAsync();
        
        // If response is not a a 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError) 
            return System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
        
        // Parse response
        Models.Error errorMessage = JsonSerializer.Deserialize<Models.Error>(responseContent) ?? throw new LighthouseNotesErrors.ShouldNotBeNullException();
        
        // If error message is one about hashes display it
        if(errorMessage.Title == "Could not find hash value for the image!" || errorMessage.Title == "MD5 hash verification failed!" || errorMessage.Title == "SHA256 hash verification failed!")
            return $"/img/image-error.jpeg";
       
        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
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

        // If response is anything other than a 500 internal server error throw an exception
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
            }
        }

        // Read response 
        string responseContent = await response.Content.ReadAsStringAsync();
        
        // If response is not a a 500 internal server error return the content
        if (response.StatusCode != HttpStatusCode.InternalServerError) 
            return System.Text.RegularExpressions.Regex.Unescape(responseContent).Replace("\"", "");
        
        // Parse response
        Models.Error errorMessage = JsonSerializer.Deserialize<Models.Error>(responseContent) ?? throw new LighthouseNotesErrors.ShouldNotBeNullException();
        
        // If error message is one about hashes display it
        if(errorMessage.Title == "Could not find hash value for the shared image!" || errorMessage.Title == "MD5 hash verification failed!" || errorMessage.Title == "SHA256 hash verification failed!")
            return $"/img/image-error.jpeg";
       
        throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);
    }

    //////////////
    // Exhibits //
    //////////////

    // GET: /case/?/exhibits
    public async Task<(Pagination, List<Exhibit>)> Exhibits(string caseId, int page = 1, int pageSize = 10, string sort = "")
    {
        // Create request
        HttpRequestMessage request = string.IsNullOrWhiteSpace(sort)
            ? new HttpRequestMessage(HttpMethod.Get, $"/case/{caseId}/exhibits?page={page}&pageSize={pageSize}")
            : new HttpRequestMessage(HttpMethod.Get, $"/case/{caseId}/exhibits?page={page}&pageSize={pageSize}&sort={sort}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        Pagination pagination = new()
        {
            Page = int.Parse(response.Headers.NonValidated["X-Page"].ToString()),
            TotalPages = int.Parse(response.Headers.NonValidated["X-Total-Pages"].ToString()),
            Total = int.Parse(response.Headers.NonValidated["X-Total-Count"].ToString()),
        };
        
        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return (pagination, JsonSerializer.Deserialize<List<Exhibit>>(responseContent) ??
                           throw new LighthouseNotesErrors.ShouldNotBeNullException());
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
    public async Task<(Pagination, List<UserAudit>)> UserAudit(int page = 1, int pageSize = 10)
    {
        // Create request
        HttpRequestMessage request = new(HttpMethod.Get, $"/audit/user?page={page}&pageSize={pageSize}");

        // Add Bearer token
        string? token = _tokenProvider.AccessToken;
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Send request and ensure response
        HttpResponseMessage response = await _http.SendAsync(request);

        // If response is not a success status code, throw exception
        if (!response.IsSuccessStatusCode)
            throw new LighthouseNotesErrors.LighthouseNotesApiException(request, response);

        Pagination pagination = new()
        {
            Page = int.Parse(response.Headers.NonValidated["X-Page"].ToString()),
            TotalPages = int.Parse(response.Headers.NonValidated["X-Total-Pages"].ToString()),
            Total = int.Parse(response.Headers.NonValidated["X-Total-Count"].ToString()),
        };
        
        // Read response and return parsed response
        string responseContent = await response.Content.ReadAsStringAsync();
        return  (pagination, JsonSerializer.Deserialize<List<UserAudit>>(responseContent) ??
                 throw new LighthouseNotesErrors.ShouldNotBeNullException());
    }
}