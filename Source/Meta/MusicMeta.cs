using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace YtPlaylist;

public class MusicMeta(ImmutableArray<string> performers, string title)
{
    [JsonPropertyName("performers"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public ImmutableArray<string> Performers { get; set; } = performers;
    [JsonPropertyName("title"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Title { get; set; } = title;
    [JsonPropertyName("remixedBy"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? RemixedBy { get; set; } = null;
    [JsonPropertyName("isRemix")] public bool IsRemix { get; set; } = false;
    [JsonPropertyName("featuring"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Featuring { get; set; } = null;
    [JsonPropertyName("album"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Album { get; set; } = null;
    [JsonPropertyName("albumArtists"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public ImmutableArray<string> AlbumArtists { get; set; } = [];
    [JsonPropertyName("copyright"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Copyright { get; set; } = null;
    [JsonPropertyName("year"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public uint? Year { get; set; } = null;
    [JsonPropertyName("genres"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public ImmutableArray<string> Genres { get; set; } = [];

    public string GetArtistsText() => string.Join(" & ", Performers);
    public string GetTitleText() => $"{Title}{(Featuring is null ? null : $" (feat. {Featuring})")}{(RemixedBy is null ? null : $" ({RemixedBy} remix)")}";

    public override string ToString() => Performers.IsDefaultOrEmpty ? GetTitleText() : $"{GetArtistsText()} - {GetTitleText()}";
}
