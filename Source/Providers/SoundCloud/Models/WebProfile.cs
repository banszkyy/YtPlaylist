using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class WebProfile
{
    [JsonPropertyName("url")] public required string Url { get; set; }
    [JsonPropertyName("network")] public string? Network { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
}
