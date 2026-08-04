using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Format
{
    [J("filename")] public string? Filename { get; set; }
    [J("nb_streams")] public int? NbStreams { get; set; }
    [J("nb_programs")] public int? NbPrograms { get; set; }
    [J("nb_stream_groups")] public int? NbStreamGroups { get; set; }
    [J("format_name")] public string? FormatName { get; set; }
    [J("format_long_name")] public string? FormatLongName { get; set; }
    [J("start_time")] public string? StartTime { get; set; }
    [J("duration")] public string? Duration { get; set; }
    [J("size")] public string? Size { get; set; }
    [J("bit_rate")] public string? BitRate { get; set; }
    [J("probe_score")] public int? ProbeScore { get; set; }
    [J("tags")] public Tags? Tags { get; set; }
}
