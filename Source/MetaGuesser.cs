using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using YoutubeExplode.Playlists;

namespace YtPlaylist;

public static partial class MetaGuesser
{
    public readonly record struct Meta(ImmutableArray<string> Artists, string Title, string? RemixedBy = null, string? Album = null, string? Copyright = null, uint? Year = null)
    {
        public string GetArtistsText() => string.Join(" & ", Artists);
        public string GetTitleText() => $"{Title}{(RemixedBy is null ? null : $" ({RemixedBy} remix)")}";

        public override string ToString() => $"{string.Join(" & ", Artists)} - {Title}{(!string.IsNullOrEmpty(RemixedBy) ? $" ({RemixedBy} remix)" : "")}";
    }

    static readonly FrozenDictionary<char, char> BracketPairs = new Dictionary<char, char>()
    {
        { '(', ')' },
        { '<', '>' },
        { '[', ']' },
    }.ToFrozenDictionary();

    public readonly struct Warning(string message, int index)
    {
        public readonly string Message = message;
        public readonly int Index = index;

        public override string ToString()
        {
            return Message;
        }
    }

    static void GuessRemix(ref ReadOnlySpan<char> title, out string? remixedBy, List<Warning>? warnings)
    {
        remixedBy = null;

        if (!title.Contains("remix", StringComparison.InvariantCultureIgnoreCase)) return;

        int i = title.IndexOf("remix", StringComparison.InvariantCultureIgnoreCase);
        int l = i;
        int r = i;
        char closingBracket = '\0';
        while (l > 0 && !BracketPairs.TryGetValue(title[l], out closingBracket)) l--;
        while (r + 1 < title.Length && title[r] != closingBracket) r++;

        ReadOnlySpan<char> part = title[(l + 1)..r];

        foreach (string suffix in (string[])["remix vip", "remix"])
        {
            if (!part.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)) continue;

            part = part[..^suffix.Length].TrimEnd();

            remixedBy = part.ToString();
            title = title[..l].ToString().TrimEnd() + title[(r + 1)..].ToString();
            return;
        }

        warnings?.Add(new Warning($"Remix suffix not found", l + 1));
        return;
    }

    static ImmutableArray<string> ParseArtists(string text)
    {
        string[] artistParts = [text];
        foreach (string separator in (string[])[" & ", ", ", " x "])
        {
            string[] v = text.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (v.Length > artistParts.Length) artistParts = v;
        }

        return [.. artistParts];
    }

    class GenericVideo(string channel, string title)
    {
        public string Channel { get; } = channel;
        public string Title { get; } = title;
    }

    static Meta Guess(GenericVideo video, List<Warning>? warnings = null)
    {
        ReadOnlySpan<char> artist = video.Channel.Trim();
        ReadOnlySpan<char> title = video.Title.Trim();

        const string TopicSuffix = " - Topic";

        bool isTopic = false;

        if (artist.EndsWith(TopicSuffix, StringComparison.InvariantCulture))
        {
            artist = artist[..^TopicSuffix.Length].TrimEnd();
            isTopic = true;
        }

        Match dummyMatch = DummySuffixRegex.Match(title.ToString());
        if (dummyMatch.Success)
        {
            if (dummyMatch.Index + dummyMatch.Length == title.Length)
            {
                title = title[..dummyMatch.Index].TrimEnd();
                if (title.EndsWith(" -"))
                {
                    title = title[..^2];
                }
            }
            else
            {
                int l = dummyMatch.Index;
                int r = l + dummyMatch.Length;
                title = title[..l].TrimEnd().ToString() + title[r..].ToString();
            }
        }

        ImmutableArray<string> artists = [artist.ToString()];

        if (!isTopic)
        {
            string[] titleSegments = [title.ToString()];
            string? separator = null;
            string[] separators = [" - ", " | "];
            foreach (string _separator in separators)
            {
                string[] v = title.ToString().Split(_separator);
                if (v.Length > titleSegments.Length)
                {
                    titleSegments = v;
                    separator = _separator;
                }
            }

            if (titleSegments.Length > 1)
            {
                artists = ParseArtists(titleSegments[0]);
                if (artists.Length != 1 || !artists[0].Equals(artist.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    warnings?.Add(new Warning($"Guessing artists from video title", 0));
                }
                title = string.Join(" - ", titleSegments[1..]);
            }
        }

        GuessRemix(ref title, out string? remixedBy, warnings);

        artists = [.. artists.Where(artist => !string.Equals(remixedBy, artist, StringComparison.InvariantCultureIgnoreCase))];

        return new Meta(artists, title.ToString(), remixedBy);
    }

    public static Meta Guess(PlaylistVideo video, List<Warning>? warnings = null) => Guess(new GenericVideo(video.Author.ChannelTitle, video.Title), warnings);

    public static Meta Guess(YoutubeExplode.Videos.Video video, List<Warning>? warnings = null)
    {
        Meta res = Guess(new GenericVideo(video.Author.ChannelTitle, video.Title), warnings);

        if (video.Author.ChannelTitle.EndsWith(" - Topic"))
        {
            List<string> _warnings = [];

            if (video.Id == "wpolMo9zeOM") Debugger.Break();

            GeneratedDescription? desc = ParseGeneratedDescription(video.Description, _warnings);
            warnings?.AddRange(_warnings);
            _warnings.Clear();

            if (desc is not null)
            {
                Dictionary<string, List<string>> roles = [];

                foreach (KeyValuePair<string, ImmutableArray<string>> item in desc.Metadata)
                {
                    if (item.Key == "Released on")
                    {
                        if (item.Value.Length != 1)
                        {
                            warnings?.Add($"Multiple release dates");
                        }
                        else if (!uint.TryParse(item.Value[0].Split('-')[0], out uint _year))
                        {
                            warnings?.Add($"Invalid release date");
                        }
                        else
                        {
                            res = res with { Year = _year };
                        }
                    }
                    else
                    {
                        foreach (string name in item.Value)
                        {
                            if (!roles.TryGetValue(name, out List<string>? others))
                            {
                                others = roles[name] = [];
                            }
                            others.Add(item.Key);
                        }
                    }
                }

                HashSet<string> artists = new(StringComparer.InvariantCultureIgnoreCase);
                HashSet<string> remixers = new(StringComparer.InvariantCultureIgnoreCase);
                HashSet<string> publishers = new(StringComparer.InvariantCultureIgnoreCase);
                HashSet<string> composers = new(StringComparer.InvariantCultureIgnoreCase);

                bool IsMentioned(string name)
                {
                    return desc.Keywords.Any(v => v.Contains(name, StringComparison.InvariantCultureIgnoreCase));
                }

                foreach ((string name, List<string> role) in roles)
                {
                    if (role.Any(r => r is "Artist" or "Performer" or "Main Artist" or "Author"))
                    {
                        if (IsMentioned(name))
                        {
                            artists.Add(name);
                        }
                    }
                    else if (role.Any(r => r is "Authors"))
                    {
                        foreach (string n in name.Split(", "))
                        {
                            if (IsMentioned(n))
                            {
                                artists.Add(n);
                            }
                        }
                    }
                    else if (role.Any(r => r is "Remixer"))
                    {
                        if (IsMentioned(name))
                        {
                            remixers.Add(name);
                        }
                        else
                        {
                            Debugger.Break();
                            warnings?.Add($"Remixer \"{name}\" not mentioned");
                        }
                    }
                    else if (role.Any(r => r is "Music Publisher"))
                    {
                        publishers.Add(name);
                    }
                    else if (role.Any(r => r is "Composer" or "Composer Lyricist"))
                    {
                        composers.Add(name);
                    }
                }

                foreach (string keyword in desc.Keywords)
                {
                    if (res.Title.Equals(keyword, StringComparison.InvariantCultureIgnoreCase) || res.GetTitleText().Equals(keyword, StringComparison.InvariantCultureIgnoreCase)) continue;
                    if (artists.Contains(keyword)) continue;
                    if (remixers.Contains(keyword)) continue;
                    if (publishers.Contains(keyword)) continue;
                    if (composers.Contains(keyword)) continue;

                    if (res.Artists.Any(v => v.Equals(keyword, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        artists.Add(keyword);
                        continue;
                    }

                    List<string> role = roles.TryGetValue(keyword, out List<string>? _0) ? _0 : [];
                    if (role.Count == 0) continue;
                }


                res = res with
                {
                    Copyright = desc.Copyright,
                };

                if (!desc.Album.Equals(res.Title, StringComparison.InvariantCultureIgnoreCase))
                {
                    res = res with { Album = desc.Album };
                }

                if (remixers.Count == 1)
                {
                    res = res with { RemixedBy = remixers.First() };
                }
                else if (remixers.Count > 1)
                {
                    warnings?.Add($"Multiple remixers");
                }

                if (artists.Count > 0)
                {
                    res = res with { Artists = [.. artists] };
                }

                warnings?.AddRange(_warnings);
            }
        }

        return res;
    }

    public static Meta Guess(ReadOnlySpan<char> text, List<Warning>? warnings = null)
    {
        GuessRemix(ref text, out string? remixedBy, warnings);

        ReadOnlySpan<string> parts = text.ToString().Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            warnings?.Add(new Warning($"It has {parts.Length} part", 0));
            return new Meta([], text.ToString(), remixedBy);
        }

        if (parts.Length > 2)
        {
            if (parts[1].Equals(parts[0], StringComparison.InvariantCultureIgnoreCase))
            {
                parts = parts[1..];
            }
        }

        if (parts.Length > 2 && remixedBy is null)
        {
            if (parts[^1].Contains("remix", StringComparison.InvariantCultureIgnoreCase))
            {
                string part = parts[^1];
                foreach (string suffix in (string[])["remix vip", "remix"])
                {
                    if (!part.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)) continue;

                    part = part[..^suffix.Length].TrimEnd();
                    remixedBy = part.ToString();

                    parts = parts[..^1];
                    goto ok;
                }
                warnings?.Add(new Warning($"Remix suffix not found", 0));
            ok:;
            }
        }

        if (parts.Length > 2)
        {
            warnings?.Add(new Warning($"It has more than two parts", 0));
        }

        ImmutableArray<string> artists = [.. ParseArtists(parts[0]).Where(artist => !string.Equals(remixedBy, artist, StringComparison.InvariantCultureIgnoreCase))];
        string title = string.Join(" - ", parts[1..]);

        return new Meta(artists, title, remixedBy);
    }

    [GeneratedRegex(@"([\(\[\|]\s*)?official(( music)?( video)?( audio)?( visualizer)?)?(\s*[\)\]\|])?", RegexOptions.IgnoreCase, "en-US")]
    static partial Regex DummySuffixRegex { get; }


    public class GeneratedDescription
    {
        public required string ProvidedBy { get; init; }
        public required ImmutableArray<string> Keywords { get; init; }
        public required string Album { get; init; }
        public required string Copyright { get; init; }
        public required ImmutableDictionary<string, ImmutableArray<string>> Metadata { get; init; }
    }

    public static GeneratedDescription? ParseGeneratedDescription(string description, List<string>? issues)
    {
        string[] lines = description.Split('\n');
        if (lines[^1] != "Auto-generated by YouTube.")
        {
            return null;
        }

        if (lines.Length < 8)
        {
            issues?.Add($"Invalid generated description");
            return null;
        }

        const string ProvidedByPrefix = "Provided to YouTube by ";
        string providedBy = lines[0];
        if (!providedBy.StartsWith(ProvidedByPrefix))
        {
            issues?.Add($"Invalid provider line");
            return null;
        }
        else
        {
            providedBy = providedBy[ProvidedByPrefix.Length..];
        }

        string[] keywords = lines[2].Split(" · ");

        string album = lines[4];

        if (string.IsNullOrWhiteSpace(album))
        {
            issues?.Add($"Empty album");
        }

        string copyright = lines[6];

        if (string.IsNullOrWhiteSpace(copyright))
        {
            issues?.Add($"Empty copyright");
        }
        else if (!copyright.StartsWith("℗ "))
        {
            issues?.Add($"Invalid copyright");
        }
        else
        {
            copyright = copyright[2..];
        }

        Dictionary<string, List<string>> metadata = [];
        for (int i = 7; i < lines.Length - 1; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;

            if (!line.Contains(':'))
            {
                issues?.Add($"Invalid meta line \"{line}\"");
                continue;
            }

            string[] k = line.Split(':')[0].Trim().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string v = line.Split(':')[1].Trim();

            for (int j = 0; j < k.Length; j++)
            {
                string w = k[j];
                w = w.Replace("  ", " ").Replace("  ", " ");

                if (!metadata.TryGetValue(w, out List<string>? values))
                {
                    values = metadata[w] = [];
                }
                values.Add(v);
            }
        }

        return new()
        {
            ProvidedBy = providedBy,
            Keywords = [.. keywords],
            Album = album,
            Copyright = copyright,
            Metadata = [.. metadata.Select(v => new KeyValuePair<string, ImmutableArray<string>>(v.Key, [.. v.Value]))],
        };
    }
}