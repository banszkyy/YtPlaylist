using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Disposition
{
    [J("default")] public int? Default { get; init; }
    [J("dub")] public int? Dub { get; init; }
    [J("original")] public int? Original { get; init; }
    [J("comment")] public int? Comment { get; init; }
    [J("lyrics")] public int? Lyrics { get; init; }
    [J("karaoke")] public int? Karaoke { get; init; }
    [J("forced")] public int? Forced { get; init; }
    [J("hearing_impaired")] public int? HearingImpaired { get; init; }
    [J("visual_impaired")] public int? VisualImpaired { get; init; }
    [J("clean_effects")] public int? CleanEffects { get; init; }
    [J("attached_pic")] public int? AttachedPic { get; init; }
    [J("timed_thumbnails")] public int? TimedThumbnails { get; init; }
    [J("non_diegetic")] public int? NonDiegetic { get; init; }
    [J("captions")] public int? Captions { get; init; }
    [J("descriptions")] public int? Descriptions { get; init; }
    [J("metadata")] public int? Metadata { get; init; }
    [J("dependent")] public int? Dependent { get; init; }
    [J("still_image")] public int? StillImage { get; init; }
    [J("multilayer")] public int? Multilayer { get; init; }
}

