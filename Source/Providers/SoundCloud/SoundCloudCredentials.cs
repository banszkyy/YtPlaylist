using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class SoundCloudCredentials
{
    [JsonPropertyName("token")] public string? Token { get; init; }
    [JsonPropertyName("jspl")] public string? Jspl { get; init; }
    [JsonPropertyName("sessionId")] public string? SessionId { get; init; }
    [JsonPropertyName("userAgent")] public string? UserAgent { get; init; }
}
