using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class User
{
    [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; init; }
    [JsonPropertyName("city")] public string? City { get; init; }
    [JsonPropertyName("comments_count")] public long? CommentsCount { get; init; }
    [JsonPropertyName("country_code")] public string? CountryCode { get; init; }
    [JsonPropertyName("created_at")] public object? CreatedAt { get; init; }
    [JsonPropertyName("creator_subscriptions")] public IReadOnlyList<CreatorSubscription>? CreatorSubscriptions { get; init; }
    [JsonPropertyName("creator_subscription")] public CreatorSubscription? CreatorSubscription { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("followers_count")] public long? FollowersCount { get; init; }
    [JsonPropertyName("followings_count")] public long? FollowingsCount { get; init; }
    [JsonPropertyName("first_name")] public string? FirstName { get; init; }
    [JsonPropertyName("full_name")] public string? FullName { get; init; }
    [JsonPropertyName("groups_count")] public long? GroupsCount { get; init; }
    [JsonPropertyName("id")] public required long Id { get; init; }
    [JsonPropertyName("kind")] public string? Kind { get; init; }
    [JsonPropertyName("last_modified")] public DateTimeOffset? LastModified { get; init; }
    [JsonPropertyName("last_name")] public string? LastName { get; init; }
    [JsonPropertyName("likes_count")] public long? LikesCount { get; init; }
    [JsonPropertyName("playlist_likes_count")] public long? PlaylistLikesCount { get; init; }
    [JsonPropertyName("permalink")] public required string Permalink { get; init; }
    [JsonPropertyName("permalink_url")] public string? PermalinkUrl { get; init; }
    [JsonPropertyName("playlist_count")] public long? PlaylistCount { get; init; }
    [JsonPropertyName("reposts_count")] public object? RepostsCount { get; init; }
    [JsonPropertyName("track_count")] public long? TrackCount { get; init; }
    [JsonPropertyName("uri")] public string? Uri { get; init; }
    [JsonPropertyName("urn")] public string? Urn { get; init; }
    [JsonPropertyName("username")] public required string Username { get; init; }
    [JsonPropertyName("verified")] public bool? Verified { get; init; }
    [JsonPropertyName("visuals")] public Visuals? Visuals { get; init; }
    [JsonPropertyName("badges")] public Badges? Badges { get; init; }
    [JsonPropertyName("station_urn")] public string? StationUrn { get; init; }
    [JsonPropertyName("station_permalink")] public string? StationPermalink { get; init; }
    [JsonPropertyName("date_of_birth")] public object? DateOfBirth { get; init; }
    [JsonPropertyName("url")] public string? Url { get; init; }

    public override string ToString() => Username ?? $"<{Id}>";
}
