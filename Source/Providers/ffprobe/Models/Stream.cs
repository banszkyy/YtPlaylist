using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Stream
{
    [J("index")] public int? Index { get; init; }
    [J("codec_name")] public string? CodecName { get; init; }
    [J("codec_long_name")] public string? CodecLongName { get; init; }
    [J("codec_type")] public string? CodecType { get; init; }
    [J("codec_tag_string")] public string? CodecTagString { get; init; }
    [J("codec_tag")] public string? CodecTag { get; init; }
    [J("mime_codec_string")] public string? MimeCodecString { get; init; }
    [J("sample_fmt")] public string? SampleFmt { get; init; }
    [J("sample_rate")] public string? SampleRate { get; init; }
    [J("channels")] public int? Channels { get; init; }
    [J("channel_layout")] public string? ChannelLayout { get; init; }
    [J("bits_per_sample")] public int? BitsPerSample { get; init; }
    [J("initial_padding")] public int? InitialPadding { get; init; }
    [J("r_frame_rate")] public string? RFrameRate { get; init; }
    [J("avg_frame_rate")] public string? AvgFrameRate { get; init; }
    [J("time_base")] public string? TimeBase { get; init; }
    [J("start_pts")] public int? StartPts { get; init; }
    [J("start_time")] public string? StartTime { get; init; }
    [J("duration_ts")] public long? DurationTs { get; init; }
    [J("duration")] public string? Duration { get; init; }
    [J("bit_rate")] public string? BitRate { get; init; }
    [J("disposition")] public Disposition? Disposition { get; init; }
    [J("tags")] public Tags? Tags { get; init; }
    [J("profile")] public string? Profile { get; init; }
    [J("width")] public int? Width { get; init; }
    [J("height")] public int? Height { get; init; }
    [J("coded_width")] public int? CodedWidth { get; init; }
    [J("coded_height")] public int? CodedHeight { get; init; }
    [J("has_b_frames")] public int? HasBFrames { get; init; }
    [J("sample_aspect_ratio")] public string? SampleAspectRatio { get; init; }
    [J("display_aspect_ratio")] public string? DisplayAspectRatio { get; init; }
    [J("pix_fmt")] public string? PixFmt { get; init; }
    [J("level")] public int? Level { get; init; }
    [J("color_range")] public string? ColorRange { get; init; }
    [J("color_space")] public string? ColorSpace { get; init; }
    [J("chroma_location")] public string? ChromaLocation { get; init; }
    [J("bits_per_raw_sample")] public string? BitsPerRawSample { get; init; }
}
