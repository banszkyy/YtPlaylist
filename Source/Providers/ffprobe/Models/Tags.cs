using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Tags
{
    [J("encoder")] public string? Encoder { get; init; }
    [J("title")] public string? Title { get; init; }
    [J("comment")] public string? Comment { get; init; }
    [J("Description")] public string? Description { get; init; }
    [J("artist")] public string? Artist { get; init; }
}
