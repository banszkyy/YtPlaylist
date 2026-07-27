using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class SearchResultItem
{
    [JsonPropertyName("artwork_url")] public string? ArtworkUrl { get; init; }
    [JsonPropertyName("caption")] public string? Caption { get; init; }
    [JsonPropertyName("commentable")] public bool? Commentable { get; init; }
    [JsonPropertyName("comment_count")] public long? CommentCount { get; init; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("downloadable")] public bool? Downloadable { get; init; }
    [JsonPropertyName("download_count")] public long? DownloadCount { get; init; }
    [JsonPropertyName("duration")] public long? Duration { get; init; }
    [JsonPropertyName("full_duration")] public long? FullDuration { get; init; }
    [JsonPropertyName("embeddable_by")] public string? EmbeddableBy { get; init; }
    [JsonPropertyName("genre")] public string? Genre { get; init; }
    [JsonPropertyName("has_downloads_left")] public bool? HasDownloadsLeft { get; init; }
    [JsonPropertyName("id")] public required long Id { get; init; }
    [JsonPropertyName("kind")] public string? Kind { get; init; }
    [JsonPropertyName("label_name")] public string? LabelName { get; init; }
    [JsonPropertyName("last_modified")] public DateTimeOffset? LastModified { get; init; }
    [JsonPropertyName("license")] public string? License { get; init; }
    [JsonPropertyName("likes_count")] public long? LikesCount { get; init; }
    [JsonPropertyName("managed_by_feeds")] public bool? ManagedByFeeds { get; init; }
    [JsonPropertyName("permalink")] public string? Permalink { get; init; }
    [JsonPropertyName("permalink_url")] public string? PermalinkUrl { get; init; }
    [JsonPropertyName("playback_count")] public long? PlaybackCount { get; init; }
    [JsonPropertyName("public")] public bool? Public { get; init; }
    [JsonPropertyName("publisher_metadata")] public PublisherMetadata? PublisherMetadata { get; init; }
    [JsonPropertyName("purchase_title")] public string? PurchaseTitle { get; init; }
    [JsonPropertyName("purchase_url")] public string? PurchaseUrl { get; init; }
    [JsonPropertyName("release_date")] public DateTimeOffset? ReleaseDate { get; init; }
    [JsonPropertyName("reposts_count")] public long? RepostsCount { get; init; }
    [JsonPropertyName("secret_token")] public object? SecretToken { get; init; }
    [JsonPropertyName("sharing")] public string? Sharing { get; init; }
    [JsonPropertyName("state")] public string? State { get; init; }
    [JsonPropertyName("streamable")] public bool? Streamable { get; init; }
    [JsonPropertyName("tag_list")] public string? TagList { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("uri")] public string? Uri { get; init; }
    [JsonPropertyName("set_type")] public string? SetType { get; init; }
    [JsonPropertyName("is_album")] public bool? IsAlbum { get; init; }
    [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; init; }
    [JsonPropertyName("tracks")] public IReadOnlyList<Track>? Tracks { get; init; }
    [JsonPropertyName("urn")] public string? Urn { get; init; }
    [JsonPropertyName("user_id")] public long? UserId { get; init; }
    [JsonPropertyName("visuals")] public Visuals? Visuals { get; init; }
    [JsonPropertyName("waveform_url")] public string? WaveformUrl { get; init; }
    [JsonPropertyName("display_date")] public DateTimeOffset? DisplayDate { get; init; }
    [JsonPropertyName("media")] public Media? Media { get; init; }
    [JsonPropertyName("station_urn")] public string? StationUrn { get; init; }
    [JsonPropertyName("station_permalink")] public string? StationPermalink { get; init; }
    [JsonPropertyName("track_authorization")] public string? TrackAuthorization { get; init; }
    [JsonPropertyName("monetization_model")] public string? MonetizationModel { get; init; }
    [JsonPropertyName("policy")] public string? Policy { get; init; }
    [JsonPropertyName("user")] public User? User { get; init; }
    [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; init; }
    [JsonPropertyName("city")] public string? City { get; init; }
    [JsonPropertyName("comments_count")] public long? CommentsCount { get; init; }
    [JsonPropertyName("country_code")] public object? CountryCode { get; init; }
    [JsonPropertyName("creator_subscriptions")] public List<CreatorSubscription>? CreatorSubscriptions { get; init; }
    [JsonPropertyName("creator_subscription")] public CreatorSubscription? CreatorSubscription { get; init; }
    [JsonPropertyName("followers_count")] public long? FollowersCount { get; init; }
    [JsonPropertyName("followings_count")] public long? FollowingsCount { get; init; }
    [JsonPropertyName("first_name")] public string? FirstName { get; init; }
    [JsonPropertyName("full_name")] public string? FullName { get; init; }
    [JsonPropertyName("groups_count")] public long? GroupsCount { get; init; }
    [JsonPropertyName("last_name")] public string? LastName { get; init; }
    [JsonPropertyName("playlist_likes_count")] public long? PlaylistLikesCount { get; init; }
    [JsonPropertyName("playlist_count")] public long? PlaylistCount { get; init; }
    [JsonPropertyName("track_count")] public long? TrackCount { get; init; }
    [JsonPropertyName("username")] public string? Username { get; init; }
    [JsonPropertyName("verified")] public bool? Verified { get; init; }
    [JsonPropertyName("badges")] public Badges? Badges { get; init; }
    [JsonPropertyName("date_of_birth")] public object? DateOfBirth { get; init; }

    public Track ToTrack() => new()
    {
        ArtworkUrl = ArtworkUrl,
        Caption = Caption,
        Commentable = Commentable,
        CommentCount = CommentCount,
        CreatedAt = CreatedAt,
        Description = Description,
        Downloadable = Downloadable,
        DownloadCount = DownloadCount,
        Duration = Duration,
        FullDuration = FullDuration,
        EmbeddableBy = EmbeddableBy,
        Genre = Genre,
        HasDownloadsLeft = HasDownloadsLeft,
        Id = Id,
        Kind = Kind,
        LabelName = LabelName,
        LastModified = LastModified,
        License = License,
        LikesCount = LikesCount,
        Permalink = Permalink,
        PermalinkUrl = PermalinkUrl,
        PlaybackCount = PlaybackCount,
        Public = Public,
        PublisherMetadata = PublisherMetadata,
        PurchaseTitle = PurchaseTitle,
        PurchaseUrl = PurchaseUrl,
        ReleaseDate = ReleaseDate,
        RepostsCount = RepostsCount,
        SecretToken = SecretToken,
        Sharing = Sharing,
        State = State,
        Streamable = Streamable,
        TagList = TagList,
        Title = Title,
        Uri = Uri,
        Urn = Urn,
        UserId = UserId,
        Visuals = Visuals,
        WaveformUrl = WaveformUrl,
        DisplayDate = DisplayDate,
        Media = Media,
        StationUrn = StationUrn,
        StationPermalink = StationPermalink,
        TrackAuthorization = TrackAuthorization,
        MonetizationModel = MonetizationModel,
        Policy = Policy,
        User = User,
    };

    public override string ToString() => Title ?? $"<{Id}>";
}
