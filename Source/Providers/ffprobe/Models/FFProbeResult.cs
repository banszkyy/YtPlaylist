using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class FFProbeResult
{
    [J("streams")] public List<Stream>? Streams { get; set; }
    [J("format")] public Format? Format { get; set; }
}
