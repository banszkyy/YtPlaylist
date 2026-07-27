using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Playlist
{
    [JsonPropertyName("artwork_url")] public string? ArtworkUrl { get; init; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("duration")] public long Duration { get; init; }
    [JsonPropertyName("embeddable_by")] public string? EmbeddableBy { get; init; }
    [JsonPropertyName("genre")] public string? Genre { get; init; }
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("kind")] public string? Kind { get; init; }
    [JsonPropertyName("label_name")] public string? LabelName { get; init; }
    [JsonPropertyName("last_modified")] public DateTimeOffset? LastModified { get; init; }
    [JsonPropertyName("license")] public string? License { get; init; }
    [JsonPropertyName("likes_count")] public long LikesCount { get; init; }
    [JsonPropertyName("managed_by_feeds")] public bool ManagedByFeeds { get; init; }
    [JsonPropertyName("permalink")] public required string Permalink { get; init; }
    [JsonPropertyName("permalink_url")] public string? PermalinkUrl { get; init; }
    [JsonPropertyName("public")] public bool Public { get; init; }
    [JsonPropertyName("purchase_title")] public string? PurchaseTitle { get; init; }
    [JsonPropertyName("purchase_url")] public string? PurchaseUrl { get; init; }
    [JsonPropertyName("release_date")] public DateTimeOffset? ReleaseDate { get; init; }
    [JsonPropertyName("reposts_count")] public long RepostsCount { get; init; }
    [JsonPropertyName("secret_token")] public string? SecretToken { get; init; }
    [JsonPropertyName("sharing")] public string? Sharing { get; init; }
    [JsonPropertyName("tag_list")] public string? TagList { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("uri")] public string? Uri { get; init; }
    [JsonPropertyName("user_id")] public long UserId { get; init; }
    [JsonPropertyName("set_type")] public string? SetType { get; init; }
    [JsonPropertyName("is_album")] public bool IsAlbum { get; init; }
    [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; init; }
    [JsonPropertyName("display_date")] public DateTimeOffset? DisplayDate { get; init; }
    [JsonPropertyName("user")] public User? User { get; init; }
    [JsonPropertyName("tracks")] public required IReadOnlyList<Track> Tracks { get; init; }
    [JsonPropertyName("track_count")] public long TrackCount { get; init; }

    public override string ToString() => Title ?? $"<{Id}>";
}
