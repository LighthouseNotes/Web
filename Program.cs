using System.Reflection;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor.Extensions;
using Web.Components;
using Web.Services.OidcCookie;

// Version and copyright message
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("Lighthouse Notes Web");
Console.WriteLine(Assembly.GetEntryAssembly()!.GetName().Version?.ToString(3));
Console.WriteLine();
Console.WriteLine("(C) Copyright 2024 Lighthouse Notes");
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.White;

// Create builder
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Static assets
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add MVC controllers
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Add standard razor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Change signalRs maximum message size so we can upload large images
builder.Services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = 100000000; // bytes - 100mb
});

// Use Redis for key storage if running in production
if (builder.Environment.IsProduction())
{
    ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ??
                                                                throw new InvalidOperationException(
                                                                    "Connection string 'Redis' not found in appsettings.json or environment variable!"));
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
}

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

// Add Oidc Authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.ClientId = builder.Configuration["Authentication:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = true;

        options.Scope.Add(OpenIdConnectScope.OfflineAccess);
        options.Scope.Add(OpenIdConnectScope.OpenId);
        options.Scope.Add(OpenIdConnectScope.Profile);
        options.Scope.Add(OpenIdConnectScope.Email);
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

// Configure cookie refresh
builder.Services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);

// Add cascading authentication
builder.Services.AddCascadingAuthenticationState();

// Localization
builder.Services.AddLocalization();

// Add MudBlazor Services
builder.Services.AddMudServicesWithExtensions(option => { option.PopoverOptions.ThrowOnDuplicateProvider = false; });

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
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Build app
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   app.UseDeveloperExceptionPage();
}

if (app.Environment.IsProduction())
{
    // Use certificate forwarding and header forwarding as production environment runs behind a reverse proxy
    app.UseCertificateForwarding();
    app.UseForwardedHeaders();

    // Use exception handler
    app.UseExceptionHandler("/error");
}

// HTTPS Redirection
app.UseHttpsRedirection();
app.UseAntiforgery();

// Static files
app.UseStaticFiles();

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Create an array of supported cultures
string[] supportedCultures = ["en-US", "en-GB"];

// Create localization options
RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Use request localization and the options defined above
app.UseRequestLocalization(localizationOptions);

// Use Mud extensions middleware
app.Use(MudExWebApp.MudExMiddleware);

// Map MVC controllers
app.MapControllers();

// Map blazor pages and set fallback page
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapRazorPages();

// Redirect to /error with the status code in the url if there is an error status code
app.UseStatusCodePagesWithRedirects("/error/{0}");

// Run app
app.Run();