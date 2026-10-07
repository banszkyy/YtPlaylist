using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class UpdatePlaylistContent
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("artwork_url")] public object? ArtworkUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("description")] public object? Description { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("genre")] public string? Genre { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("permalink")] public string? Permalink { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("sharing")] public string? Sharing { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("release_date")] public DateTimeOffset? ReleaseDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("tag_list")] public string? TagList { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("title")] public string? Title { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("tracks")] public List<long>? Tracks { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonPropertyName("_resource_id")] public long ResourceId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyName("_resource_type")] public string? ResourceType { get; init; }
}
