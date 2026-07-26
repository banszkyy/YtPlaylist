using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class UpdatePlaylistResponse
{
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("ean")] public object? Ean { get; init; }
    [JsonPropertyName("genre")] public string? Genre { get; init; }
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("kind")] public string? Kind { get; init; }
    [JsonPropertyName("label_name")] public string? LabelName { get; init; }
    [JsonPropertyName("license")] public string? License { get; init; }
    [JsonPropertyName("permalink")] public string? Permalink { get; init; }
    [JsonPropertyName("permalink_url")] public string? PermalinkUrl { get; init; }
    [JsonPropertyName("public")] public bool Public { get; init; }
    [JsonPropertyName("purchase_title")] public string? PurchaseTitle { get; init; }
    [JsonPropertyName("purchase_url")] public string? PurchaseUrl { get; init; }
    [JsonPropertyName("release")] public object? Release { get; init; }
    [JsonPropertyName("release_date")] public DateTimeOffset? ReleaseDate { get; init; }
    [JsonPropertyName("set_type")] public string? SetType { get; init; }
    [JsonPropertyName("secret_token")] public string? SecretToken { get; init; }
    [JsonPropertyName("tag_list")] public string? TagList { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
}
