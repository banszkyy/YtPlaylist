using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Facet
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("facets")] public IReadOnlyList<Facet>? Facets { get; init; }
    [JsonPropertyName("value")] public string? Value { get; init; }
    [JsonPropertyName("count")] public long? Count { get; init; }
    [JsonPropertyName("filter")] public string? Filter { get; init; }

    public override string? ToString() => Filter;
}
