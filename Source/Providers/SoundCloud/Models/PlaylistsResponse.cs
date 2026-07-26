using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class PlaylistsResponse
{
    [JsonPropertyName("collection")] public required IReadOnlyList<Playlist> Collection { get; init; }
    [JsonPropertyName("next_href")] public object? NextHref { get; init; }
    [JsonPropertyName("query_urn")] public object? QueryUrn { get; init; }
}
