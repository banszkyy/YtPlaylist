using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class PathfinderRequest
{
    [J("variables")] public required PathfinderVariables Variables { get; set; }
    [J("operationName")] public required string OperationName { get; set; }
    [J("extensions")] public required RequestExtensions Extensions { get; set; }
}

public class RequestExtensions
{
    [J("persistedQuery")] public required PersistedQuery PersistedQuery { get; set; }
}

public class PersistedQuery
{
    [J("version")] public required long Version { get; set; }
    [J("sha256Hash")] public required string Sha256Hash { get; set; }
}

public class PathfinderVariables
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("limit")] public long? Limit { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("offset")] public long? Offset { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("expandedFolders")] public IReadOnlyList<object>? ExpandedFolders { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("features")] public IReadOnlyList<string>? Features { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("filters")] public IReadOnlyList<string>? Filters { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("flatten")] public bool? Flatten { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("folderUri")] public string? FolderUri { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includeFoldersWhenFlattening")] public bool? IncludeFoldersWhenFlattening { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("order")] public object? Order { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("textFilter")] public string? TextFilter { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("query")] public string? Query { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("searchTerm")] public string? SearchTerm { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("numberOfTopResults")] public long? NumberOfTopResults { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includeArtistHasConcertsField")] public bool? IncludeArtistHasConcertsField { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includeAudiobooks")] public bool? IncludeAudiobooks { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includeAuthors")] public bool? IncludeAuthors { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includePreReleases")] public bool? IncludePreReleases { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includeAlbumPreReleases")] public bool? IncludeAlbumPreReleases { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includeEpisodeContentRatingsV2")] public bool? IncludeEpisodeContentRatingsV2 { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("isPrefix")] public object? IsPrefix { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("sectionFilters")] public IReadOnlyList<string>? SectionFilters { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("uri")] public string? Uri { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("enableWatchFeedEntrypoint")] public bool? EnableWatchFeedEntrypoint { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("playlistItemUris")] public IReadOnlyList<string>? PlaylistItemUris { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("playlistUri")] public string? PlaylistUri { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("newPosition")] public NewPosition? NewPosition { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("uids")] public IReadOnlyList<string>? Uids { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("includeVideoAssociationItems")] public bool? IncludeVideoAssociationItems { get; set; }
}

public class NewPosition
{
    [J("moveType")] public required string MoveType { get; set; }
    [J("fromUid")] public object? FromUid { get; set; }
}

public class PathfinderResponse
{
    [J("data")] public required PathfinderResponseData Data { get; set; }
    [J("extensions")] public ResponseExtensions? Extensions { get; set; }
}

public class PathfinderResponseData
{
    [J("searchV2")] public SearchV2? Search { get; set; }
    [J("playlistV2")] public Playlist? Playlist { get; set; }
    [J("trackUnion")] public Track0? TrackUnion { get; set; }
}

public class SearchV2
{
    [J("query")] public string? Query { get; set; }
    [J("albumsV2")] public SearchResponseList<object?>? Albums { get; set; }
    [J("artists")] public SearchResponseList<object?>? Artists { get; set; }
    [J("audiobooks")] public SearchResponseList<object?>? Audiobooks { get; set; }
    [J("chipOrder")] public Container<ChipOrderItem>? ChipOrder { get; set; }
    [J("episodes")] public SearchResponseList<object?>? Episodes { get; set; }
    [J("genres")] public SearchResponseList<object?>? Genres { get; set; }
    [J("playlists")] public SearchResponseList<object?>? Playlists { get; set; }
    [J("podcasts")] public SearchResponseList<object?>? Podcasts { get; set; }
    [J("topResultsV2")] public TopResults? TopResults { get; set; }
    [J("tracksV2")] public SearchResponseList<MatchedSearchResultItem>? Tracks { get; set; }
    [J("users")] public SearchResponseList<object?>? Users { get; set; }
}

public class SearchResponseList<T> : Container<T>
{
    [J("pagingInfo")] public PagingInfo? PagingInfo { get; set; }
}

public class ChipOrderItem
{
    [J("typeName")] public string? TypeName { get; set; }

    public override string? ToString() => TypeName;
}

public class TopResults
{
    [J("itemsV2")] public required IReadOnlyList<MatchedSearchResultItem> Items { get; set; }
}

public class MatchedSearchResultItem
{
    [J("item")] public required DataWrapper<SearchResultItem> Item { get; set; }
    [J("matchedFields")] public IReadOnlyList<object>? MatchedFields { get; set; }

    public override string? ToString() => Item.ToString();
}

public class SearchResultItem
{
    [J("albumOfTrack")] public AlbumOfTrack? AlbumOfTrack { get; set; }
    [J("artists")] public Container<Artist0>? Artists { get; set; }
    [J("associationsV3")] public AssociationsV3? Associations { get; set; }
    [J("contentRating")] public ContentRating? ContentRating { get; set; }
    [J("duration")] public Duration? Duration { get; set; }
    [J("id")] public string? Id { get; set; }
    [J("trackMediaType")] public string? TrackMediaType { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("visualIdentity")] public VisualIdentity? VisualIdentity { get; set; }
    [J("coverArt")] public object? CoverArt { get; set; }
    [J("date")] public Date? Date { get; set; }
    [J("isAlbumPreRelease")] public bool? IsAlbumPreRelease { get; set; }
    [J("preReleaseEndDateTime")] public Date? PreReleaseEndDateTime { get; set; }
    [J("type")] public string? Type { get; set; }
    [J("onPlatformReputationTrait")] public ReputationTrait? OnPlatformReputationTrait { get; set; }
    [J("profile")] public Profile? Profile { get; set; }
    [J("visuals")] public Visuals? Visuals { get; set; }
    [J("contentRatingsV2")] public ContentRatings? ContentRatings { get; set; }
    [J("description")] public string? Description { get; set; }
    [J("gatedEntityRelations")] public IReadOnlyList<object>? GatedEntityRelations { get; set; }
    [J("mediaTypes")] public IReadOnlyList<string>? MediaTypes { get; set; }
    [J("playedState")] public PlayedState? PlayedState { get; set; }
    [J("podcastV2")] public DataWrapper<Podcast>? PodcastV2 { get; set; }
    [J("releaseDate")] public Date? ReleaseDate { get; set; }
    [J("restrictions")] public Restrictions? Restrictions { get; set; }
    [J("videoPreviewThumbnail")] public VideoPreviewThumbnail? VideoPreviewThumbnail { get; set; }
    [J("attributes")] public IReadOnlyList<Attribute>? Attributes { get; set; }
    [J("format")] public string? Format { get; set; }
    [J("images")] public Container<Image>? Images { get; set; }
    [J("ownerV2")] public DataWrapper<Owner0>? Owner { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class AlbumOfTrack
{
    [J("id")] public string? Id { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("visualIdentity")] public VisualIdentity? VisualIdentity { get; set; }
    [J("date")] public Date? Date { get; set; }
    [J("copyright")] public Container<CopyrightItem>? Copyright { get; set; }
    [J("courtesyLine")] public string? CourtesyLine { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("sharingInfo")] public SharingInfo? SharingInfo { get; set; }
    [J("tracks")] public Container<TracksItem>? Tracks { get; set; }
    [J("type")] public string? Type { get; set; }
    [J("coverArt")] public CoverArt? CoverArt { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class AssociationsV3
{
    [J("audioAssociations")] public Container<object>? AudioAssociations { get; set; }
    [J("videoAssociations")] public Container<object>? VideoAssociations { get; set; }
}

public class Image
{
    [J("extractedColors")] public ExtractedColors? ExtractedColors { get; set; }
    [J("sources")] public required IReadOnlyList<ImageSource> Sources { get; set; }
}

public class ExtractedColors
{
    [J("colorDark")] public HexColor? ColorDark { get; set; }
    [J("colorRaw")] public HexColor? ColorRaw { get; set; }
}

public class HexColor
{
    [J("hex")] public string? Hex { get; set; }
    [J("isFallback")] public bool? IsFallback { get; set; }

    public override string? ToString() => Hex;
}

public class SquareCoverImage
{
    [J("extractedColorSet")] public ExtractedColorSet? ExtractedColorSet { get; set; }
    [J("image")] public DataWrapper<Image>? Image { get; set; }
}

public class ExtractedColorSet
{
    [J("encoreBaseSetTextColor")] public RgbaColor? EncoreBaseSetTextColor { get; set; }
    [J("highContrast")] public Contrast? HighContrast { get; set; }
    [J("higherContrast")] public Contrast? HigherContrast { get; set; }
    [J("minContrast")] public Contrast? MinContrast { get; set; }
}

public class RgbaColor
{
    [J("alpha")] public long Alpha { get; set; }
    [J("blue")] public long Blue { get; set; }
    [J("green")] public long Green { get; set; }
    [J("red")] public long Red { get; set; }

    public override string ToString() => $"rgba({Red}, {Green}, {Blue}, {Alpha})";
}

public class Contrast
{
    [J("backgroundBase")] public RgbaColor? BackgroundBase { get; set; }
    [J("backgroundTintedBase")] public RgbaColor? BackgroundTintedBase { get; set; }
    [J("textBase")] public RgbaColor? TextBase { get; set; }
    [J("textBrightAccent")] public RgbaColor? TextBrightAccent { get; set; }
    [J("textSubdued")] public RgbaColor? TextSubdued { get; set; }
}

public class Container<T>
{
    [J("items")] public IReadOnlyList<T> Items { get; init; } = [];
    [J("totalCount")] public long TotalCount { get; init; } = 0;
}

public class Artist0
{
    [J("profile")] public Profile? Profile { get; set; }
    [J("uri")] public required string Uri { get; set; }

    public override string ToString() => $"{Profile} <{Uri}>";
}

public class Profile
{
    [J("name")] public string? Name { get; set; }

    public override string? ToString() => Name;
}

public class Attribute
{
    [J("key")] public required string Key { get; set; }
    [J("value")] public required string Value { get; set; }

    public override string ToString() => $"{Key} = {Value}";
}

public class ContentRating
{
    [J("label")] public required string Label { get; set; }

    public override string ToString() => Label;
}

public class ContentRatings
{
    [J("labels")] public IReadOnlyList<string>? Labels { get; set; }
}

public class Date
{
    [J("isoString")] public string? IsoString { get; set; }
    [J("precision")] public string? Precision { get; set; }
    [J("year")] public long? Year { get; set; }

    public override string? ToString() => IsoString ?? Year?.ToString();
}

public class Duration
{
    [J("totalMilliseconds")] public long? TotalMilliseconds { get; set; }

    public override string? ToString() => TotalMilliseconds.HasValue ? TimeSpan.FromMilliseconds(TotalMilliseconds.Value).ToString() : base.ToString();
}

public class ReputationTrait
{
    [J("verification")] public Verification? Verification { get; set; }
}

public class Verification
{
    [J("isVerified")] public bool? IsVerified { get; set; }
}

public class Owner0
{
    [J("avatar")] public CoverArt? Avatar { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("socialHandle")] public object? SocialHandle { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("username")] public string? Username { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Playability
{
    [J("playable")] public bool? Playable { get; set; }
    [J("reason")] public string? Reason { get; set; }

    public override string? ToString() => Reason ?? Playable?.ToString();
}

public class PlayedState
{
    [J("playPositionMilliseconds")] public long? PlayPositionMilliseconds { get; set; }
    [J("state")] public string? State { get; set; }
}

public class DataWrapper<T> where T : notnull
{
    [J("data")] public required T Data { get; set; }

    public override string? ToString() => Data.ToString();
}

public class Podcast
{
    [J("coverArt")] public CoverArt? CoverArt { get; set; }
    [J("mediaType")] public string? MediaType { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("publisher")] public Profile? Publisher { get; set; }
    [J("uri")] public required string Uri { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Restrictions
{
    [J("paywallContent")] public bool PaywallContent { get; set; }
}

public class VideoPreviewThumbnail
{
    [J("imagePreview")] public object? ImagePreview { get; set; }
}

public class DataSource
{
    [J("imageFormat")] public string? ImageFormat { get; set; }
    [J("maxHeight")] public long? MaxHeight { get; set; }
    [J("maxWidth")] public long? MaxWidth { get; set; }
    [J("url")] public Uri? Url { get; set; }

    public override string ToString() => $"{MaxWidth}x{MaxHeight} {Url}";
}

public class VisualIdentity
{
    [J("sixteenByNineCoverImage")] public SixteenByNineCoverImage? SixteenByNineCoverImage { get; set; }
    [J("squareCoverImage")] public SquareCoverImage? SquareCoverImage { get; set; }
}

public class SixteenByNineCoverImage
{
    [J("image")] public object? Image { get; set; }
}

public class ResponseExtensions
{
    [J("requestIds")] public Dictionary<string, object?>? RequestIds { get; set; }
}

public class PagingInfo
{
    [J("limit")] public long? Limit { get; set; }
    [J("offset")] public long? Offset { get; set; }
    [J("nextOffset")] public long? NextOffset { get; set; }
}

public class Playlist
{
    [J("content")] public Contents? Content { get; set; }
    [J("abuseReportingEnabled")] public bool? AbuseReportingEnabled { get; set; }
    [J("attributes")] public IReadOnlyList<object>? Attributes { get; set; }
    [J("basePermission")] public string? BasePermission { get; set; }
    [J("currentUserCapabilities")] public UserCapabilities? CurrentUserCapabilities { get; set; }
    [J("description")] public string? Description { get; set; }
    [J("followers")] public long? Followers { get; set; }
    [J("following")] public bool? Following { get; set; }
    [J("format")] public string? Format { get; set; }
    [J("images")] public Container<Image>? Images { get; set; }
    [J("members")] public Container<Member0>? Members { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("ownerV2")] public DataWrapper<Owner0>? Owner { get; set; }
    [J("revisionId")] public string? RevisionId { get; set; }
    [J("sharingInfo")] public SharingInfo? SharingInfo { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("visualIdentity")] public VisualIdentity? VisualIdentity { get; set; }
    [J("watchFeedEntrypoint")] public WatchFeedEntrypoint? WatchFeedEntrypoint { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Contents : Container<Content>
{
    [J("pagingInfo")] public PagingInfo? PagingInfo { get; set; }
}

public class Content
{
    [J("addedAt")] public Date? AddedAt { get; set; }
    [J("addedBy")] public DataWrapper<Profile1>? AddedBy { get; set; }
    [J("attributes")] public IReadOnlyList<object>? Attributes { get; set; }
    [J("itemV2")] public DataWrapper<ItemV2Data>? ItemV2 { get; set; }
    [J("itemV3")] public DataWrapper<ItemV3Data>? ItemV3 { get; set; }
    [J("uid")] public string? Uid { get; set; }
}

public class Profile1
{
    [J("avatar")] public object? Avatar { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("socialHandle")] public object? SocialHandle { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("username")] public string? Username { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class ItemV3Data
{
    [J("consumptionExperienceTrait")] public ConsumptionExperienceTrait? ConsumptionExperienceTrait { get; set; }
    [J("identityTrait")] public IdentityTrait? IdentityTrait { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("visualIdentityTrait")] public VisualIdentity? VisualIdentityTrait { get; set; }

    public override string ToString() => $"<{Uri}>";
}

public class IdentityTrait
{
    [J("contentHierarchyParent")] public ContentHierarchyParent? ContentHierarchyParent { get; set; }
    [J("contributors")] public Container<ContributorsItem>? Contributors { get; set; }
    [J("description")] public string? Description { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("type")] public string? Type { get; set; }
}

public class ContentHierarchyParent
{
    [J("identityTrait")] public ContentHierarchyParentIdentityTrait? IdentityTrait { get; set; }
    [J("publishingMetadataTrait")] public PublishingMetadataTrait? PublishingMetadataTrait { get; set; }
    [J("uri")] public string? Uri { get; set; }

    public override string ToString() => $"<{Uri}>";
}

public class ContributorsItem
{
    [J("name")] public string? Name { get; set; }
    [J("uri")] public string? Uri { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class ContentHierarchyParentIdentityTrait
{
    [J("name")] public string? Name { get; set; }

    public override string ToString() => $"{Name}";
}

public class PublishingMetadataTrait
{
    [J("firstPublishedAt")] public Date? FirstPublishedAt { get; set; }
}

public class ConsumptionExperienceTrait
{
    [J("contentRatings")] public IReadOnlyList<object>? ContentRatings { get; set; }
    [J("duration")] public Duration? Duration { get; set; }
    [J("formats")] public IReadOnlyList<string>? Formats { get; set; }
}

public class ItemV2Data
{
    [J("trackDuration")] public Duration? TrackDuration { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("albumOfTrack")] public AlbumOfTrack? AlbumOfTrack { get; set; }
    [J("artists")] public Container<Artist0>? Artists { get; set; }
    [J("associationsV3")] public AssociationsV3? Associations { get; set; }
    [J("contentRating")] public ContentRating? ContentRating { get; set; }
    [J("discNumber")] public long? DiscNumber { get; set; }
    [J("mediaType")] public string? MediaType { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("playcount")] public string? Playcount { get; set; }
    [J("trackNumber")] public long? TrackNumber { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class UserCapabilities
{
    [J("canAbuseReport")] public bool? CanAbuseReport { get; set; }
    [J("canAdministratePermissions")] public bool? CanAdministratePermissions { get; set; }
    [J("canCancelMembership")] public bool? CanCancelMembership { get; set; }
    [J("canEditItems")] public bool? CanEditItems { get; set; }
    [J("canMixPlaylist")] public bool? CanMixPlaylist { get; set; }
    [J("canView")] public bool? CanView { get; set; }
}

public class Member0
{
    [J("isOwner")] public bool? IsOwner { get; set; }
    [J("permissionLevel")] public string? PermissionLevel { get; set; }
    [J("user")] public DataWrapper<Owner0>? User { get; set; }
}

public class SharingInfo
{
    [J("shareId")] public string? ShareId { get; set; }
    [J("shareUrl")] public Uri? ShareUrl { get; set; }
}

public class WatchFeedEntrypoint
{
    [J("entrypointUri")] public string? EntrypointUri { get; set; }
    [J("thumbnailImage")] public DataWrapper<ThumbnailImageData>? ThumbnailImage { get; set; }
    [J("video")] public object? Video { get; set; }
}

public class ThumbnailImageData
{
    [J("imageId")] public Uri? ImageId { get; set; }
    [J("imageIdType")] public string? ImageIdType { get; set; }
    [J("sources")] public IReadOnlyList<DataSource>? Sources { get; set; }
}

public class MeResponse
{
    [J("data")] public required MeResponseData Data { get; set; }
}

public class MeResponseData
{
    [J("me")] public required Me Me { get; set; }
}

public class Me
{
    [J("libraryV3")] public Library? Library { get; set; }
    [J("profile")] public MeProfile? Profile { get; set; }
}

public class MeProfile
{
    [J("accountId")] public required string AccountId { get; set; }
    [J("avatar")] public object? Avatar { get; set; }
    [J("avatarBackgroundColor")] public long? AvatarBackgroundColor { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("socialHandle")] public object? SocialHandle { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("username")] public required string Username { get; set; }

    public override string ToString() => $"{Name ?? Username ?? AccountId} <{Uri}>";
}

public class Library
{
    [J("availableFilters")] public IReadOnlyList<SelectedSortOrder>? AvailableFilters { get; set; }
    [J("availableSortOrders")] public IReadOnlyList<SelectedSortOrder>? AvailableSortOrders { get; set; }
    [J("breadcrumbs")] public IReadOnlyList<object>? Breadcrumbs { get; set; }
    [J("items")] public IReadOnlyList<ItemElement>? Items { get; set; }
    [J("pagingInfo")] public PagingInfo? PagingInfo { get; set; }
    [J("selectedFilters")] public IReadOnlyList<SelectedSortOrder>? SelectedFilters { get; set; }
    [J("selectedSortOrder")] public SelectedSortOrder? SelectedSortOrder { get; set; }
    [J("totalCount")] public long? TotalCount { get; set; }
}

public class SelectedSortOrder
{
    [J("id")] public required string Id { get; set; }
    [J("name")] public required string Name { get; set; }

    public override string ToString() => $"{Name} <{Id}>";
}

public class ItemElement
{
    [J("addedAt")] public Date? AddedAt { get; set; }
    [J("depth")] public long? Depth { get; set; }
    [J("item")] public required ItemItem Item { get; set; }
    [J("pinnable")] public bool? Pinnable { get; set; }
    [J("pinned")] public bool? Pinned { get; set; }
    [J("playedAt")] public Date? PlayedAt { get; set; }

    public override string? ToString() => Item.ToString();
}

public class ItemItem
{
    [J("_uri")] public string? Uri { get; set; }
    [J("data")] public ItemData2? Data { get; set; }

    public override string? ToString() => Data?.ToString();
}

public class ItemData2
{
    [J("count")] public long? Count { get; set; }
    [J("image")] public Image? Image { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("attributes")] public IReadOnlyList<object>? Attributes { get; set; }
    [J("currentUserCapabilities")] public UserCapabilities? CurrentUserCapabilities { get; set; }
    [J("description")] public string? Description { get; set; }
    [J("format")] public string? Format { get; set; }
    [J("images")] public Container<Image>? Images { get; set; }
    [J("ownerV2")] public DataWrapper<Owner1>? OwnerV2 { get; set; }
    [J("revisionId")] public string? RevisionId { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Owner1
{
    [J("avatar")] public Avatar? Avatar { get; set; }
    [J("id")] public string? Id { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("socialHandle")] public object? SocialHandle { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("username")] public string? Username { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Avatar
{
    [J("sources")] public IReadOnlyList<ImageSource>? Sources { get; set; }
}

public class CreatePlaylistResponse
{
    [J("uri")] public required string Uri { get; set; }
    [J("revision")] public string? Revision { get; set; }

    public override string ToString() => $"<{Uri}>";
}

public class Track0
{
    [J("associationsV3")] public AssociationsV3? Associations { get; set; }
    [J("contentRating")] public ContentRating? ContentRating { get; set; }
    [J("duration")] public Duration? Duration { get; set; }
    [J("id")] public string? Id { get; set; }
    [J("mediaType")] public string? MediaType { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("playcount")] public string? Playcount { get; set; }
    [J("saved")] public bool? Saved { get; set; }
    [J("sharingInfo")] public SharingInfo? SharingInfo { get; set; }
    [J("trackNumber")] public long? TrackNumber { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("visualIdentity")] public VisualIdentity? VisualIdentity { get; set; }
    [J("albumOfTrack")] public AlbumOfTrack? AlbumOfTrack { get; set; }
    [J("firstArtist")] public Container<Track2>? FirstArtist { get; set; }
    [J("otherArtists")] public Container<Artist1>? OtherArtists { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class CopyrightItem
{
    [J("text")] public string? Text { get; set; }
    [J("type")] public string? Type { get; set; }

    public override string? ToString() => Type;
}

public class CoverArt
{
    [J("extractedColors")] public ExtractedColors? ExtractedColors { get; set; }
    [J("sources")] public IReadOnlyList<ImageSource>? Sources { get; set; }
}


public class ImageSource
{
    [J("url")] public Uri? Url { get; set; }
    [J("width")] public long? Width { get; set; }
    [J("height")] public long? Height { get; set; }

    public override string? ToString() => Height.HasValue && Width.HasValue ? $"{Width}x{Height} {Url}" : Url?.ToString();
}

public class TracksItem
{
    [J("track")] public Track1? Track { get; set; }

    public override string? ToString() => Track?.ToString();
}

public class Track1
{
    [J("trackNumber")] public long? TrackNumber { get; set; }
    [J("uri")] public string? Uri { get; set; }

    public override string ToString() => $"#{TrackNumber} <{Uri}>";
}

public class Track2
{
    [J("discography")] public Discography? Discography { get; set; }
    [J("id")] public string? Id { get; set; }
    [J("profile")] public Profile? Profile { get; set; }
    [J("relatedContent")] public RelatedContent? RelatedContent { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("visuals")] public Visuals? Visuals { get; set; }

    public override string ToString() => $"{Profile} <{Uri}>";
}

public class Artist1
{
    [J("date")] public Date? Date { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("sharingInfo")] public SharingInfo? SharingInfo { get; set; }
    [J("tracks")] public Container<TracksItem>? Tracks { get; set; }
    [J("type")] public string? Type { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("coverArt")] public CoverArt? CoverArt { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Discography
{
    [J("albums")] public Container<Album>? Albums { get; set; }
    [J("popularReleasesAlbums")] public Container<Artist1>? PopularReleasesAlbums { get; set; }
    [J("singles")] public Container<Album>? Singles { get; set; }
    [J("topTracks")] public Container<TopTracksItem>? TopTracks { get; set; }
}

public class Album
{
    [J("releases")] public Container<Artist1>? Releases { get; set; }
}

public class TopTracksItem
{
    [J("track")] public Track3? Track { get; set; }

    public override string? ToString() => Track?.ToString();
}

public class Track3
{
    [J("albumOfTrack")] public AlbumOfTrack? AlbumOfTrack { get; set; }
    [J("artists")] public Container<Artist0>? Artists { get; set; }
    [J("associationsV3")] public AssociationsV3? Associations { get; set; }
    [J("contentRating")] public ContentRating? ContentRating { get; set; }
    [J("duration")] public Duration? Duration { get; set; }
    [J("id")] public string? Id { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("playcount")] public string? Playcount { get; set; }
    [J("previews")] public Previews? Previews { get; set; }
    [J("uri")] public string? Uri { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Previews
{
    [J("audioPreviews")] public Container<AudioPreview>? AudioPreviews { get; set; }
}

public class AudioPreview
{
    [J("url")] public Uri? Url { get; set; }

    public override string ToString() => $"<{Url}>";

}

public class RelatedContent
{
    [J("relatedArtists")] public Container<Artist2>? RelatedArtists { get; set; }
}

public class Artist2
{
    [J("id")] public string? Id { get; set; }
    [J("profile")] public Profile? Profile { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("visuals")] public Visuals? Visuals { get; set; }

    public override string ToString() => $"<{Uri}>";
}

public class Visuals
{
    [J("avatarImage")] public Avatar? AvatarImage { get; set; }
}
