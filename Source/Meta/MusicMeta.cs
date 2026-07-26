using System.Collections.Immutable;

namespace YtPlaylist;

public class MusicMeta(ImmutableArray<string> performers, string title)
{
    public ImmutableArray<string> Performers { get; set; } = performers;
    public string Title { get; set; } = title;
    public string? RemixedBy { get; set; } = null;
    public string? Featuring { get; set; } = null;
    public string? Album { get; set; } = null;
    public ImmutableArray<string> AlbumArtists { get; set; } = [];
    public string? Copyright { get; set; } = null;
    public uint? Year { get; set; } = null;
    public ImmutableArray<string> Genres { get; set; } = [];

    public string GetArtistsText() => string.Join(" & ", Performers);
    public string GetTitleText() => $"{Title}{(Featuring is null ? null : $" (feat. {Featuring})")}{(RemixedBy is null ? null : $" ({RemixedBy} remix)")}";

    public override string ToString() => Performers.IsDefaultOrEmpty ? GetTitleText() : $"{GetArtistsText()} - {GetTitleText()}";
}
