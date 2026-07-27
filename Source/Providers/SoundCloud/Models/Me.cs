using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Me
{
    [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; init; }
    [JsonPropertyName("blocked_tracks_count")] public long BlockedTracksCount { get; init; }
    [JsonPropertyName("city")] public string? City { get; init; }
    [JsonPropertyName("comments_count")] public long CommentsCount { get; init; }
    [JsonPropertyName("consumer_subscriptions")] public object? ConsumerSubscriptions { get; init; }
    [JsonPropertyName("consumer_subscription")] public RSubscription? ConsumerSubscription { get; init; }
    [JsonPropertyName("country_code")] public object? CountryCode { get; init; }
    [JsonPropertyName("cpp")] public object? Cpp { get; init; }
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("creator_subscriptions")] public IReadOnlyList<RSubscription>? CreatorSubscriptions { get; init; }
    [JsonPropertyName("creator_subscription")] public RSubscription? CreatorSubscription { get; init; }
    [JsonPropertyName("date_of_birth")] public DateOfBirth? DateOfBirth { get; init; }
    [JsonPropertyName("default_license")] public string? DefaultLicense { get; init; }
    [JsonPropertyName("default_tracks_feedable")] public bool DefaultTracksFeedable { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("downloads_disabled")] public bool DownloadsDisabled { get; init; }
    [JsonPropertyName("downloads_disabled_reason")] public string? DownloadsDisabledReason { get; init; }
    [JsonPropertyName("first_name")] public string? FirstName { get; init; }
    [JsonPropertyName("followers_count")] public long FollowersCount { get; init; }
    [JsonPropertyName("followings_count")] public long FollowingsCount { get; init; }
    [JsonPropertyName("full_name")] public string? FullName { get; init; }
    [JsonPropertyName("gender")] public string? Gender { get; init; }
    [JsonPropertyName("groups_count")] public long GroupsCount { get; init; }
    [JsonPropertyName("hidden_tracks_count")] public long HiddenTracksCount { get; init; }
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("kind")] public string? Kind { get; init; }
    [JsonPropertyName("last_modified")] public DateTimeOffset LastModified { get; init; }
    [JsonPropertyName("last_name")] public string? LastName { get; init; }
    [JsonPropertyName("likes_count")] public long LikesCount { get; init; }
    [JsonPropertyName("playlist_likes_count")] public long PlaylistLikesCount { get; init; }
    [JsonPropertyName("locale")] public string? Locale { get; init; }
    [JsonPropertyName("permalink")] public string? Permalink { get; init; }
    [JsonPropertyName("permalink_url")] public string? PermalinkUrl { get; init; }
    [JsonPropertyName("playlist_count")] public long PlaylistCount { get; init; }
    [JsonPropertyName("primary_email")] public string? PrimaryEmail { get; init; }
    [JsonPropertyName("primary_email_confirmed")] public bool PrimaryEmailConfirmed { get; init; }
    [JsonPropertyName("primary_email_sha256")] public string? PrimaryEmailSha256 { get; init; }
    [JsonPropertyName("private_playlists_count")] public long PrivatePlaylistsCount { get; init; }
    [JsonPropertyName("private_tracks_count")] public long PrivateTracksCount { get; init; }
    [JsonPropertyName("quota")] public Quota? Quota { get; init; }
    [JsonPropertyName("reposts_count")] public long RepostsCount { get; init; }
    [JsonPropertyName("track_count")] public long TrackCount { get; init; }
    [JsonPropertyName("urn")] public string? Urn { get; init; }
    [JsonPropertyName("uri")] public string? Uri { get; init; }
    [JsonPropertyName("username")] public string? Username { get; init; }
    [JsonPropertyName("verified")] public bool Verified { get; init; }
    [JsonPropertyName("visuals")] public Visuals? Visuals { get; init; }
    [JsonPropertyName("confirmed")] public bool Confirmed { get; init; }
    [JsonPropertyName("badges")] public Badges? Badges { get; init; }
    [JsonPropertyName("analytics_id")] public string? AnalyticsId { get; init; }
    [JsonPropertyName("consent_management_jwt")] public ConsentManagementJwt? ConsentManagementJwt { get; init; }
    [JsonPropertyName("station_urn")] public string? StationUrn { get; init; }
    [JsonPropertyName("station_permalink")] public string? StationPermalink { get; init; }
    [JsonPropertyName("marketing_ids")] public MarketingIds? MarketingIds { get; init; }
    [JsonPropertyName("ppid")] public string? Ppid { get; init; }
    [JsonPropertyName("spotlight_limit")] public long SpotlightLimit { get; init; }
}
