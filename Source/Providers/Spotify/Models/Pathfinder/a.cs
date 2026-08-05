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
}

public class SearchV2
{
    [J("query")] public required string Query { get; set; }
    [J("albumsV2")] public SearchResponseList<object?>? Albums { get; set; }
    [J("artists")] public SearchResponseList<object?>? Artists { get; set; }
    [J("audiobooks")] public SearchResponseList<object?>? Audiobooks { get; set; }
    [J("chipOrder")] public ChipOrder? ChipOrder { get; set; }
    [J("episodes")] public SearchResponseList<object?>? Episodes { get; set; }
    [J("genres")] public SearchResponseList<object?>? Genres { get; set; }
    [J("playlists")] public SearchResponseList<object?>? Playlists { get; set; }
    [J("podcasts")] public SearchResponseList<object?>? Podcasts { get; set; }
    [J("topResultsV2")] public TopResults? TopResults { get; set; }
    [J("tracksV2")] public SearchResponseList<MatchedSearchResultItem>? Tracks { get; set; }
    [J("users")] public SearchResponseList<object?>? Users { get; set; }
}

public class SearchResponseList<T>
{
    [J("totalCount")] public required long TotalCount { get; set; }
    [J("items")] public required IReadOnlyList<T> Items { get; set; }
    [J("pagingInfo")] public required PagingInfo PagingInfo { get; set; }
}

public class ChipOrder
{
    [J("items")] public IReadOnlyList<ChipOrderItem>? Items { get; set; }
}

public class ChipOrderItem
{
    [J("typeName")] public required string TypeName { get; set; }
}

public class TopResults
{
    [J("itemsV2")] public required IReadOnlyList<MatchedSearchResultItem> Items { get; set; }
}

public class MatchedSearchResultItem
{
    [J("item")] public required TypedInstanceWrapper<SearchResultItem> Item { get; set; }
    [J("matchedFields")] public IReadOnlyList<object>? MatchedFields { get; set; }

    public override string? ToString() => Item.ToString();
}

public class SearchResultItem
{
    [J("albumOfTrack")] public AlbumOfTrack? AlbumOfTrack { get; set; }
    [J("artists")] public Artists? Artists { get; set; }
    [J("associationsV3")] public Associations? Associations { get; set; }
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
    [J("preReleaseEndDateTime")] public IsoDate? PreReleaseEndDateTime { get; set; }
    [J("type")] public string? Type { get; set; }
    [J("onPlatformReputationTrait")] public OnPlatformReputationTrait? OnPlatformReputationTrait { get; set; }
    [J("profile")] public Profile? Profile { get; set; }
    [J("visuals")] public Visuals? Visuals { get; set; }
    [J("contentRatingsV2")] public ContentRatings? ContentRatings { get; set; }
    [J("description")] public string? Description { get; set; }
    [J("gatedEntityRelations")] public IReadOnlyList<object>? GatedEntityRelations { get; set; }
    [J("mediaTypes")] public IReadOnlyList<string>? MediaTypes { get; set; }
    [J("playedState")] public PlayedState? PlayedState { get; set; }
    [J("podcastV2")] public TypedInstanceWrapper<PodcastData>? PodcastV2 { get; set; }
    [J("releaseDate")] public IsoDate? ReleaseDate { get; set; }
    [J("restrictions")] public Restrictions? Restrictions { get; set; }
    [J("videoPreviewThumbnail")] public VideoPreviewThumbnail? VideoPreviewThumbnail { get; set; }
    [J("attributes")] public IReadOnlyList<Attribute>? Attributes { get; set; }
    [J("format")] public string? Format { get; set; }
    [J("images")] public Images? Images { get; set; }
    [J("ownerV2")] public TypedInstanceWrapper<OwnerData>? Owner { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class AlbumOfTrack
{
    [J("coverArt")] public object? CoverArt { get; set; }
    [J("id")] public string? Id { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("visualIdentity")] public VisualIdentity? VisualIdentity { get; set; }
    [J("date")] public IsoDate? Date { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class AssociationsV3
{
    [J("audioAssociations")] public AudioAssociations? AudioAssociations { get; set; }
    [J("videoAssociations")] public VideoAssociations? VideoAssociations { get; set; }
}

public class AudioAssociations
{
    [J("items")] public IReadOnlyList<object>? Items { get; set; }
}

public class VideoAssociations
{
    [J("totalCount")] public long? TotalCount { get; set; }
}

public class Image
{
    [J("extractedColors")] public ExtractedColors? ExtractedColors { get; set; }
    [J("sources")] public required IReadOnlyList<ImageSource> Sources { get; set; }
}

public class ExtractedColors
{
    [J("colorDark")] public ColorDark? ColorDark { get; set; }
}

public class ColorDark
{
    [J("hex")] public string? Hex { get; set; }
    [J("isFallback")] public bool? IsFallback { get; set; }

    public override string? ToString() => Hex;
}

public class ImageSource
{
    [J("imageFormat")] public string? ImageFormat { get; set; }
    [J("url")] public Uri? Url { get; set; }
    [J("height")] public long? Height { get; set; }
    [J("width")] public long? Width { get; set; }

    public override string? ToString() => Height.HasValue && Width.HasValue ? $"{Width}x{Height} {Url}" : Url?.ToString();
}

public class SquareCoverImage
{
    [J("extractedColorSet")] public ExtractedColorSet? ExtractedColorSet { get; set; }
    [J("image")] public TypedInstanceWrapper<Image>? Image { get; set; }
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

public class Artists
{
    [J("items")] public required IReadOnlyList<ArtistsItem> Items { get; set; }
}

public class ArtistsItem
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

public class Associations
{
    [J("audioAssociations")] public object? AudioAssociations { get; set; }
    [J("videoAssociations")] public object? VideoAssociations { get; set; }
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
    [J("year")] public long? Year { get; set; }
}

public class Duration
{
    [J("totalMilliseconds")] public long? TotalMilliseconds { get; set; }
}

public class Images
{
    [J("items")] public required IReadOnlyList<Image> Items { get; set; }
}

public class OnPlatformReputationTrait
{
    [J("verification")] public Verification? Verification { get; set; }
}

public class Verification
{
    [J("isVerified")] public bool? IsVerified { get; set; }
}

public class OwnerData
{
    [J("avatar")] public AvatarClass? Avatar { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("socialHandle")] public object? SocialHandle { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("username")] public string? Username { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class AvatarClass
{
    [J("sources")] public IReadOnlyList<ImageSource>? Sources { get; set; }
}

public class Playability
{
    [J("playable")] public required bool Playable { get; set; }
    [J("reason")] public required string Reason { get; set; }

    public override string ToString() => Reason;
}

public class PlayedState
{
    [J("playPositionMilliseconds")] public long? PlayPositionMilliseconds { get; set; }
    [J("state")] public string? State { get; set; }
}

public class TypedInstanceWrapper<T> where T : notnull
{
    [J("data")] public required T Data { get; set; }

    public override string? ToString() => Data.ToString();
}

public class PodcastData
{
    [J("coverArt")] public AvatarClass? CoverArt { get; set; }
    [J("mediaType")] public string? MediaType { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("publisher")] public Profile? Publisher { get; set; }
    [J("uri")] public required string Uri { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class IsoDate
{
    [J("isoString")] public required DateTimeOffset IsoString { get; set; }
    [J("precision")] public string? Precision { get; set; }

    public override string ToString() => IsoString.ToString();
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
    [J("maxHeight")] public required long MaxHeight { get; set; }
    [J("maxWidth")] public required long MaxWidth { get; set; }
    [J("url")] public required Uri Url { get; set; }

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

public class Visuals
{
    [J("avatarImage")] public object? AvatarImage { get; set; }
}

public class ResponseExtensions
{
    [J("requestIds")] public required Dictionary<string, object?> RequestIds { get; set; }
}

public class PagingInfo
{
    [J("limit")] public required long Limit { get; set; }
    [J("offset")] public long? Offset { get; set; }
    [J("nextOffset")] public long? NextOffset { get; set; }
}

public class Playlist
{
    [J("content")] public Content? Content { get; set; }
    [J("abuseReportingEnabled")] public bool? AbuseReportingEnabled { get; set; }
    [J("attributes")] public IReadOnlyList<object>? Attributes { get; set; }
    [J("basePermission")] public string? BasePermission { get; set; }
    [J("currentUserCapabilities")] public CurrentUserCapabilities? CurrentUserCapabilities { get; set; }
    [J("description")] public string? Description { get; set; }
    [J("followers")] public long? Followers { get; set; }
    [J("following")] public bool? Following { get; set; }
    [J("format")] public string? Format { get; set; }
    [J("images")] public Images? Images { get; set; }
    [J("members")] public Members? Members { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("ownerV2")] public TypedInstanceWrapper<OwnerData>? Owner { get; set; }
    [J("revisionId")] public string? RevisionId { get; set; }
    [J("sharingInfo")] public SharingInfo? SharingInfo { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("visualIdentity")] public VisualIdentity? VisualIdentity { get; set; }
    [J("watchFeedEntrypoint")] public WatchFeedEntrypoint? WatchFeedEntrypoint { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Content
{
    [J("items")] public IReadOnlyList<ContentItem>? Items { get; set; }
    [J("pagingInfo")] public PagingInfo? PagingInfo { get; set; }
    [J("totalCount")] public long? TotalCount { get; set; }
}

public class ContentItem
{
    [J("addedAt")] public IsoDate? AddedAt { get; set; }
    [J("addedBy")] public TypedInstanceWrapper<AddedByData>? AddedBy { get; set; }
    [J("attributes")] public IReadOnlyList<object>? Attributes { get; set; }
    [J("itemV2")] public TypedInstanceWrapper<ItemV2Data>? ItemV2 { get; set; }
    [J("itemV3")] public TypedInstanceWrapper<ItemV3Data>? ItemV3 { get; set; }
    [J("uid")] public string? Uid { get; set; }
}

public class AddedByData
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
    [J("identityTrait")] public DataIdentityTrait? IdentityTrait { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("uri")] public string? Uri { get; set; }
    [J("visualIdentityTrait")] public VisualIdentity? VisualIdentityTrait { get; set; }

    public override string ToString() => $"<{Uri}>";
}

public class DataIdentityTrait
{
    [J("contentHierarchyParent")] public ContentHierarchyParent? ContentHierarchyParent { get; set; }
    [J("contributors")] public Contributors? Contributors { get; set; }
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

public class Contributors
{
    [J("items")] public IReadOnlyList<ContributorsItem>? Items { get; set; }
    [J("totalCount")] public long? TotalCount { get; set; }
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
    [J("artists")] public Artists? Artists { get; set; }
    [J("associationsV3")] public AssociationsV3? AssociationsV3 { get; set; }
    [J("contentRating")] public ContentRating? ContentRating { get; set; }
    [J("discNumber")] public long? DiscNumber { get; set; }
    [J("mediaType")] public string? MediaType { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("playability")] public Playability? Playability { get; set; }
    [J("playcount")] public string? Playcount { get; set; }
    [J("trackNumber")] public long? TrackNumber { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class CurrentUserCapabilities
{
    [J("canAbuseReport")] public bool? CanAbuseReport { get; set; }
    [J("canAdministratePermissions")] public bool? CanAdministratePermissions { get; set; }
    [J("canCancelMembership")] public bool? CanCancelMembership { get; set; }
    [J("canEditItems")] public bool? CanEditItems { get; set; }
    [J("canMixPlaylist")] public bool? CanMixPlaylist { get; set; }
    [J("canView")] public bool? CanView { get; set; }
}

public class Members
{
    [J("items")] public IReadOnlyList<MembersItem>? Items { get; set; }
    [J("totalCount")] public long? TotalCount { get; set; }
}

public class MembersItem
{
    [J("isOwner")] public bool? IsOwner { get; set; }
    [J("permissionLevel")] public string? PermissionLevel { get; set; }
    [J("user")] public TypedInstanceWrapper<OwnerData>? User { get; set; }
}

public class SharingInfo
{
    [J("shareId")] public string? ShareId { get; set; }
    [J("shareUrl")] public Uri? ShareUrl { get; set; }
}

public class WatchFeedEntrypoint
{
    [J("entrypointUri")] public string? EntrypointUri { get; set; }
    [J("thumbnailImage")] public ThumbnailImage? ThumbnailImage { get; set; }
    [J("video")] public object? Video { get; set; }
}

public class ThumbnailImage
{
    [J("data")] public ThumbnailImageData? Data { get; set; }
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
    [J("avatar")] public required object? Avatar { get; set; }
    [J("avatarBackgroundColor")] public required long AvatarBackgroundColor { get; set; }
    [J("name")] public required string Name { get; set; }
    [J("socialHandle")] public required object? SocialHandle { get; set; }
    [J("uri")] public required string Uri { get; set; }
    [J("username")] public required string Username { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class Library
{
    [J("availableFilters")] public required IReadOnlyList<SelectedSortOrder> AvailableFilters { get; set; }
    [J("availableSortOrders")] public required IReadOnlyList<SelectedSortOrder> AvailableSortOrders { get; set; }
    [J("breadcrumbs")] public required IReadOnlyList<object> Breadcrumbs { get; set; }
    [J("items")] public IReadOnlyList<ItemElement>? Items { get; set; }
    [J("pagingInfo")] public required PagingInfo PagingInfo { get; set; }
    [J("selectedFilters")] public required IReadOnlyList<SelectedSortOrder> SelectedFilters { get; set; }
    [J("selectedSortOrder")] public SelectedSortOrder? SelectedSortOrder { get; set; }
    [J("totalCount")] public required long TotalCount { get; set; }
}

public class SelectedSortOrder
{
    [J("id")] public required string Id { get; set; }
    [J("name")] public required string Name { get; set; }

    public override string ToString() => $"{Name} <{Id}>";
}

public class ItemElement
{
    [J("addedAt")] public IsoDate? AddedAt { get; set; }
    [J("depth")] public long? Depth { get; set; }
    [J("item")] public required ItemItem Item { get; set; }
    [J("pinnable")] public bool? Pinnable { get; set; }
    [J("pinned")] public bool? Pinned { get; set; }
    [J("playedAt")] public IsoDate? PlayedAt { get; set; }

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
    [J("currentUserCapabilities")] public CurrentUserCapabilities? CurrentUserCapabilities { get; set; }
    [J("description")] public string? Description { get; set; }
    [J("format")] public string? Format { get; set; }
    [J("images")] public Images? Images { get; set; }
    [J("ownerV2")] public OwnerV2? OwnerV2 { get; set; }
    [J("revisionId")] public string? RevisionId { get; set; }

    public override string ToString() => $"{Name} <{Uri}>";
}

public class OwnerV2
{
    [J("data")] public OwnerV2Data? Data { get; set; }

    public override string? ToString() => Data?.ToString();
}

public class OwnerV2Data
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
    [J("revision")] public required string Revision { get; set; }

    public override string ToString() => $"<{Uri}>";
}
