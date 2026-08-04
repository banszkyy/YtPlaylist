using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Tags
{
    [J("encoder")] public string? Encoder { get; set; }
    [J("title")] public string? Title { get; set; }
    [J("comment")] public string? Comment { get; set; }
    [J("Description")] public string? Description { get; set; }
    [J("artist")] public string? Artist { get; set; }
}
