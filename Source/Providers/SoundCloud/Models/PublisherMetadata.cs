
using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class PublisherMetadata
{
    [JsonPropertyName("id")] public long? Id { get; init; }
    [JsonPropertyName("urn")] public string? Urn { get; init; }
    [JsonPropertyName("artist")] public string? Artist { get; init; }
    [JsonPropertyName("album_title")] public string? AlbumTitle { get; init; }
    [JsonPropertyName("release_title")] public string? ReleaseTitle { get; init; }
    [JsonPropertyName("contains_music")] public bool? ContainsMusic { get; init; }
    [JsonPropertyName("upc_or_ean")] public string? UpcOrEan { get; init; }
    [JsonPropertyName("isrc")] public string? Isrc { get; init; }
    [JsonPropertyName("explicit")] public bool? Explicit { get; init; }
    [JsonPropertyName("p_line")] public string? PLine { get; init; }
    [JsonPropertyName("p_line_for_display")] public string? PLineForDisplay { get; init; }
    [JsonPropertyName("c_line")] public string? CLine { get; init; }
    [JsonPropertyName("c_line_for_display")] public string? CLineForDisplay { get; init; }
    [JsonPropertyName("writer_composer")] public string? WriterComposer { get; init; }
    [JsonPropertyName("publisher")] public string? Publisher { get; init; }
    [JsonPropertyName("iswc")] public string? Iswc { get; set; }
}
