using System.Collections.Immutable;

namespace YtPlaylist;

sealed class AppArguments
{
    public required ImmutableArray<string> PlaylistIds { get; init; }
    public required bool UseCache { get; init; }
    public required string HttpCachePath { get; init; }
    public required string? YouTubeCredentialsPath { get; init; }
    public required string? SoundCloudCredentialsPath { get; init; }
    public required string? CookiesPath { get; init; }
    public required bool DryRun { get; init; }
    public required bool Download { get; init; }
    public required bool Metadata { get; init; }
    public required bool Lyrics { get; init; }
    public required string OutputPath { get; init; }
    public required bool IgnoreMetaWarnings { get; init; }
    public required bool RecreateMetadata { get; init; }
    public required bool CheckRedundancy { get; init; }
    public required bool CheckDuplicates { get; init; }
    public required bool RegenerateAudicousPlaylists { get; init; }
    public required bool SyncSoundCloudPlaylists { get; init; }
    public required bool IgnoreSoundCloudMatchWarnings { get; init; }
    public required string? FixFile { get; init; }
    public required bool SaveIntermediateTags { get; init; }
    public required ImmutableArray<string> SoundCloudIgnore { get; init; }
    public required ImmutableArray<string> YtDlpAdditionalArguments { get; init; }
}
