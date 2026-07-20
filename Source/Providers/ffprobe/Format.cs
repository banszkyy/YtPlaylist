// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
using System.Text.Json.Serialization;

namespace FFMPEG.Probe;

public class Format
{
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("nb_streams")]
    public int? NbStreams { get; set; }

    [JsonPropertyName("nb_programs")]
    public int? NbPrograms { get; set; }

    [JsonPropertyName("nb_stream_groups")]
    public int? NbStreamGroups { get; set; }

    [JsonPropertyName("format_name")]
    public string? FormatName { get; set; }

    [JsonPropertyName("format_long_name")]
    public string? FormatLongName { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? BitRate { get; set; }

    [JsonPropertyName("probe_score")]
    public int? ProbeScore { get; set; }

    [JsonPropertyName("tags")]
    public Tags? Tags { get; set; }
}
