using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Visual
{
    [JsonPropertyName("urn")] public string? Urn { get; init; }
    [JsonPropertyName("entry_time")] public long? EntryTime { get; init; }
    [JsonPropertyName("visual_url")] public string? VisualUrl { get; init; }
    [JsonPropertyName("enabled")] public bool? Enabled { get; init; }
    [JsonPropertyName("visuals")] public IReadOnlyList<Visual>? Visuals { get; init; }
    [JsonPropertyName("tracking")] public object? Tracking { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("link")] public string? Link { get; init; }
}
