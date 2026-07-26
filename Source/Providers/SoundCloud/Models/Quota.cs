using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Quota
{
    [JsonPropertyName("unlimited_upload_quota")] public bool UnlimitedUploadQuota { get; init; }
    [JsonPropertyName("upload_seconds_limit")] public long UploadSecondsLimit { get; init; }
    [JsonPropertyName("upload_seconds_used")] public long UploadSecondsUsed { get; init; }
    [JsonPropertyName("upload_seconds_left")] public long UploadSecondsLeft { get; init; }
    [JsonPropertyName("upload_tracks_used")] public long UploadTracksUsed { get; init; }
    [JsonPropertyName("unlimited_upload_duration_quota")] public bool UnlimitedUploadDurationQuota { get; init; }
    [JsonPropertyName("unlimited_upload_track_quota")] public bool UnlimitedUploadTrackQuota { get; init; }
}
