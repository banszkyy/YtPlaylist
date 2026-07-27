using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class TrackSearchResponse
{
    [JsonPropertyName("collection")] public required IReadOnlyList<Track> Collection { get; init; }
    [JsonPropertyName("total_results")] public required long TotalResults { get; init; }
    [JsonPropertyName("facets")] public IReadOnlyList<Facet>? Facets { get; init; }
    [JsonPropertyName("query_urn")] public string? QueryUrn { get; init; }
}
