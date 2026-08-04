using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Stream
{
    [J("index")] public int? Index { get; set; }
    [J("codec_name")] public string? CodecName { get; set; }
    [J("codec_long_name")] public string? CodecLongName { get; set; }
    [J("codec_type")] public string? CodecType { get; set; }
    [J("codec_tag_string")] public string? CodecTagString { get; set; }
    [J("codec_tag")] public string? CodecTag { get; set; }
    [J("mime_codec_string")] public string? MimeCodecString { get; set; }
    [J("sample_fmt")] public string? SampleFmt { get; set; }
    [J("sample_rate")] public string? SampleRate { get; set; }
    [J("channels")] public int? Channels { get; set; }
    [J("channel_layout")] public string? ChannelLayout { get; set; }
    [J("bits_per_sample")] public int? BitsPerSample { get; set; }
    [J("initial_padding")] public int? InitialPadding { get; set; }
    [J("r_frame_rate")] public string? RFrameRate { get; set; }
    [J("avg_frame_rate")] public string? AvgFrameRate { get; set; }
    [J("time_base")] public string? TimeBase { get; set; }
    [J("start_pts")] public int? StartPts { get; set; }
    [J("start_time")] public string? StartTime { get; set; }
    [J("duration_ts")] public long? DurationTs { get; set; }
    [J("duration")] public string? Duration { get; set; }
    [J("bit_rate")] public string? BitRate { get; set; }
    [J("disposition")] public Disposition? Disposition { get; set; }
    [J("tags")] public Tags? Tags { get; set; }
    [J("profile")] public string? Profile { get; set; }
    [J("width")] public int? Width { get; set; }
    [J("height")] public int? Height { get; set; }
    [J("coded_width")] public int? CodedWidth { get; set; }
    [J("coded_height")] public int? CodedHeight { get; set; }
    [J("has_b_frames")] public int? HasBFrames { get; set; }
    [J("sample_aspect_ratio")] public string? SampleAspectRatio { get; set; }
    [J("display_aspect_ratio")] public string? DisplayAspectRatio { get; set; }
    [J("pix_fmt")] public string? PixFmt { get; set; }
    [J("level")] public int? Level { get; set; }
    [J("color_range")] public string? ColorRange { get; set; }
    [J("color_space")] public string? ColorSpace { get; set; }
    [J("chroma_location")] public string? ChromaLocation { get; set; }
    [J("bits_per_raw_sample")] public string? BitsPerRawSample { get; set; }
}
