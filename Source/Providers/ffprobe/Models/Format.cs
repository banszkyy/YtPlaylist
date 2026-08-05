using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Format
{
    [J("filename")] public string? Filename { get; init; }
    [J("nb_streams")] public int? NbStreams { get; init; }
    [J("nb_programs")] public int? NbPrograms { get; init; }
    [J("nb_stream_groups")] public int? NbStreamGroups { get; init; }
    [J("format_name")] public string? FormatName { get; init; }
    [J("format_long_name")] public string? FormatLongName { get; init; }
    [J("start_time")] public string? StartTime { get; init; }
    [J("duration")] public string? Duration { get; init; }
    [J("size")] public string? Size { get; init; }
    [J("bit_rate")] public string? BitRate { get; init; }
    [J("probe_score")] public int? ProbeScore { get; init; }
    [J("tags")] public Tags? Tags { get; init; }
}
