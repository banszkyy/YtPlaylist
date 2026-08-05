using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class FFProbeResult
{
    [J("streams")] public IReadOnlyList<Stream>? Streams { get; init; }
    [J("format")] public Format? Format { get; init; }
}
