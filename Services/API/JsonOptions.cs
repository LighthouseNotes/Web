using System.Text.Json;

namespace Web.Services.API;

public static class JsonOptions
{
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Use camelCase
        WriteIndented = true
    };
}