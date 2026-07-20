// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
using System.Text.Json.Serialization;

namespace FFMPEG.Probe;

public class Stream
{
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("codec_name")]
    public string? CodecName { get; set; }

    [JsonPropertyName("codec_long_name")]
    public string? CodecLongName { get; set; }

    [JsonPropertyName("codec_type")]
    public string? CodecType { get; set; }

    [JsonPropertyName("codec_tag_string")]
    public string? CodecTagString { get; set; }

    [JsonPropertyName("codec_tag")]
    public string? CodecTag { get; set; }

    [JsonPropertyName("mime_codec_string")]
    public string? MimeCodecString { get; set; }

    [JsonPropertyName("sample_fmt")]
    public string? SampleFmt { get; set; }

    [JsonPropertyName("sample_rate")]
    public string? SampleRate { get; set; }

    [JsonPropertyName("channels")]
    public int? Channels { get; set; }

    [JsonPropertyName("channel_layout")]
    public string? ChannelLayout { get; set; }

    [JsonPropertyName("bits_per_sample")]
    public int? BitsPerSample { get; set; }

    [JsonPropertyName("initial_padding")]
    public int? InitialPadding { get; set; }

    [JsonPropertyName("r_frame_rate")]
    public string? RFrameRate { get; set; }

    [JsonPropertyName("avg_frame_rate")]
    public string? AvgFrameRate { get; set; }

    [JsonPropertyName("time_base")]
    public string? TimeBase { get; set; }

    [JsonPropertyName("start_pts")]
    public int? StartPts { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("duration_ts")]
    public long? DurationTs { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? BitRate { get; set; }

    [JsonPropertyName("disposition")]
    public Disposition? Disposition { get; set; }

    [JsonPropertyName("tags")]
    public Tags? Tags { get; set; }

    [JsonPropertyName("profile")]
    public string? Profile { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("coded_width")]
    public int? CodedWidth { get; set; }

    [JsonPropertyName("coded_height")]
    public int? CodedHeight { get; set; }

    [JsonPropertyName("has_b_frames")]
    public int? HasBFrames { get; set; }

    [JsonPropertyName("sample_aspect_ratio")]
    public string? SampleAspectRatio { get; set; }

    [JsonPropertyName("display_aspect_ratio")]
    public string? DisplayAspectRatio { get; set; }

    [JsonPropertyName("pix_fmt")]
    public string? PixFmt { get; set; }

    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("color_range")]
    public string? ColorRange { get; set; }

    [JsonPropertyName("color_space")]
    public string? ColorSpace { get; set; }

    [JsonPropertyName("chroma_location")]
    public string? ChromaLocation { get; set; }

    [JsonPropertyName("bits_per_raw_sample")]
    public string? BitsPerRawSample { get; set; }
}

