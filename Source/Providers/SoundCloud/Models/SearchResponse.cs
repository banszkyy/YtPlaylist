using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class SearchResponse
{
    [JsonPropertyName("collection")] public IReadOnlyList<SearchResultItem>? Collection { get; init; }
    [JsonPropertyName("total_results")] public long? TotalResults { get; init; }
    [JsonPropertyName("facets")] public IReadOnlyList<Facet>? Facets { get; init; }
    [JsonPropertyName("next_href")] public string? NextHref { get; init; }
    [JsonPropertyName("query_urn")] public string? QueryUrn { get; init; }
}
