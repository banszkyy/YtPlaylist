using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class UpdatePlaylistContent
{
    [JsonPropertyName("artwork_url")] public object? ArtworkUrl { get; set; }
    [JsonPropertyName("description")] public object? Description { get; set; }
    [JsonPropertyName("genre")] public string Genre { get; set; } = string.Empty;
    [JsonPropertyName("permalink")] public required string Permalink { get; set; }
    [JsonPropertyName("sharing")] public required string Sharing { get; set; }
    [JsonPropertyName("release_date")] public DateTimeOffset? ReleaseDate { get; set; }
    [JsonPropertyName("tag_list")] public string TagList { get; set; } = string.Empty;
    [JsonPropertyName("title")] public required string Title { get; set; }
    [JsonPropertyName("tracks")] public required List<long> Tracks { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("_resource_id")] public long ResourceId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][JsonPropertyName("_resource_type")] public string? ResourceType { get; init; }
}
