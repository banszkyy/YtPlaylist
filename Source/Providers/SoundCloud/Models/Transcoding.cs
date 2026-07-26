using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Transcoding
{
    [JsonPropertyName("url")] public string? Url { get; init; }
    [JsonPropertyName("preset")] public string? Preset { get; init; }
    [JsonPropertyName("duration")] public long? Duration { get; init; }
    [JsonPropertyName("snipped")] public bool? Snipped { get; init; }
    [JsonPropertyName("format")] public Format? Format { get; init; }
    [JsonPropertyName("quality")] public string? Quality { get; init; }
    [JsonPropertyName("is_legacy_transcoding")] public bool? IsLegacyTranscoding { get; init; }
}
