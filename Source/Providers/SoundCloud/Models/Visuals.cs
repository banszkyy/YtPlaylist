using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Visuals
{
    [JsonPropertyName("urn")] public string? Urn { get; init; }
    [JsonPropertyName("enabled")] public bool Enabled { get; init; }
    [JsonPropertyName("visuals")] public IReadOnlyList<Visual>? VisualsVisuals { get; init; }
    [JsonPropertyName("tracking")] public object? Tracking { get; init; }
}
