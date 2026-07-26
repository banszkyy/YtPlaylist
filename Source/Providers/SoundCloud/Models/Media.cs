using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Media
{
    [JsonPropertyName("transcodings")] public IReadOnlyList<Transcoding>? Transcodings { get; init; }
}
