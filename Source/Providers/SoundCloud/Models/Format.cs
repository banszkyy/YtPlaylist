using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Format
{
    [JsonPropertyName("protocol")] public string? Protocol { get; init; }
    [JsonPropertyName("mime_type")] public string? MimeType { get; init; }
}
