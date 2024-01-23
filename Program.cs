using Auth0.AspNetCore.Authentication;
using Auth0Net.DependencyInjection;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Syncfusion.Blazor;
using System.Security.Cryptography.X509Certificates;

// Create builder
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Static assets
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add MVC controllers
builder.Services.AddControllers();

// Add standard razor services
builder.Services.AddRazorPages(options => { options.RootDirectory = "/Components"; });
builder.Services.AddServerSideBlazor();

// Add certificate forwarding for Nginx reverse proxy 
builder.Services.AddCertificateForwarding(options =>
{
    options.CertificateHeader = "X-SSL-CERT";
    options.HeaderConverter = headerValue =>
    {
        X509Certificate2 clientCertificate = new(System.Web.HttpUtility.UrlDecodeToBytes(headerValue));
        return clientCertificate;
    };
});

// Add forward headers for reverse proxy 
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Add Auth0 Authentication
builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"] ??
                     throw new InvalidOperationException("Auth0:Domain not found in appsettings.json!");
    options.ClientId = builder.Configuration["Auth0:Auth:ClientId"] ??
                       throw new InvalidOperationException("Auth0:Auth:ClientId not found in appsettings.json!");
    options.ClientSecret = builder.Configuration["Auth0:Auth:ClientSecret"];
}).WithAccessToken(options =>
{
    options.Audience = builder.Configuration["Auth0:Auth:Audience"];
    options.UseRefreshTokens = true;
});

// Add Auth0 Management API Authentication 
builder.Services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = builder.Configuration["Auth0:Domain"] ??
                    throw new InvalidOperationException("Auth0:Domain not found in appsettings.json!");
    config.ClientId = builder.Configuration["Auth0:Management:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:Management:ClientSecret"];
});

// Add Auth0 Management API
builder.Services.AddAuth0ManagementClient().AddManagementAccessToken();

// Localization
builder.Services.AddLocalization();

// Add MudBlazor Services
builder.Services.AddMudServices(option => { option.PopoverOptions.ThrowOnDuplicateProvider = false; });

// Syncfusion
builder.Services.AddSyncfusionBlazor();
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(builder.Configuration["Syncfusion:LicenseKey"]);

// Other services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Lighthouse Notes API services
builder.Services.AddScoped<LighthouseNotesAPIGet>();
builder.Services.AddScoped<LighthouseNotesAPIPost>();
builder.Services.AddScoped<LighthouseNotesAPIPut>();
builder.Services.AddScoped<LighthouseNotesAPIDelete>();

// Services
builder.Services.AddScoped<SpinnerService>();
builder.Services.AddScoped<TokenProvider>();

// Build app
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

if (app.Environment.IsProduction())
{
    // Use certificate forwarding and header forwarding as production environment runs behind a reverse proxy
    app.UseCertificateForwarding();
    app.UseForwardedHeaders();

    // Use exception handler
    app.UseExceptionHandler("/Error");
}

// HTTPS Redirection
app.UseHttpsRedirection();

// Static files
app.UseStaticFiles();

// Routing
app.UseRouting();

// Use Authentication and Authorization 
app.UseAuthentication();
app.UseAuthorization();

// Create a array of supported cultures
string[] supportedCultures = { "en-US", "en-GB" };

// Create localization options
RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Use request localization and the options defined above
app.UseRequestLocalization(localizationOptions);

// Map MVC controllers 
app.MapControllers();

// Map blazor pages and set fallback page
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Run app
app.Run();