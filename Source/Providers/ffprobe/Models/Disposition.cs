using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.FFMPEG.Probe;

sealed class Disposition
{
    [J("default")] public int? Default { get; set; }
    [J("dub")] public int? Dub { get; set; }
    [J("original")] public int? Original { get; set; }
    [J("comment")] public int? Comment { get; set; }
    [J("lyrics")] public int? Lyrics { get; set; }
    [J("karaoke")] public int? Karaoke { get; set; }
    [J("forced")] public int? Forced { get; set; }
    [J("hearing_impaired")] public int? HearingImpaired { get; set; }
    [J("visual_impaired")] public int? VisualImpaired { get; set; }
    [J("clean_effects")] public int? CleanEffects { get; set; }
    [J("attached_pic")] public int? AttachedPic { get; set; }
    [J("timed_thumbnails")] public int? TimedThumbnails { get; set; }
    [J("non_diegetic")] public int? NonDiegetic { get; set; }
    [J("captions")] public int? Captions { get; set; }
    [J("descriptions")] public int? Descriptions { get; set; }
    [J("metadata")] public int? Metadata { get; set; }
    [J("dependent")] public int? Dependent { get; set; }
    [J("still_image")] public int? StillImage { get; set; }
    [J("multilayer")] public int? Multilayer { get; set; }
}

