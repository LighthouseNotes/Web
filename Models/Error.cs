using System.Text.Json.Serialization;

namespace Web.Models;

public class Error
{
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("status")] public int Status { get; init; }
    [JsonPropertyName("detail")] public string? Detail { get; init; }
    [JsonPropertyName("traceId")] public string? TraceId { get; init; }
}