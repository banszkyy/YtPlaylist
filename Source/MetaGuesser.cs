using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using YoutubeExplode.Playlists;

namespace YtPlaylist;

public static partial class MetaGuesser
{
    static readonly FrozenDictionary<char, char> BracketPairs = new Dictionary<char, char>()
    {
        { '(', ')' },
        { '<', '>' },
        { '[', ']' },
        { '|', '|' },
        { '「', '」' },
        { '【', '】' },
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

    static void GuessFeaturing(ref ReadOnlySpan<char> title, out string? featuring, List<Warning>? warnings)
    {
        featuring = null;

        ReadOnlySpan<string> prefixes = ["feat. ", "feat ", "ft. "];

        int i = title.IndexOfAny(prefixes, StringComparison.InvariantCultureIgnoreCase);
        if (i == -1) return;

        int l = i;
        int r = i;
        char closingBracket = '\0';
        while (l > 0 && !BracketPairs.TryGetValue(title[l], out closingBracket)) l--;
        while (r + 1 < title.Length && title[r] != closingBracket) r++;

        ReadOnlySpan<char> part = title[(l + 1)..r];

        foreach (string prefix in prefixes)
        {
            if (!part.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase)) continue;

            part = part[prefix.Length..].TrimEnd();

            featuring = part.ToString();
            title = title[..l].ToString().TrimEnd() + title[(r + 1)..].ToString();
            return;
        }

        warnings?.Add(new Warning($"Feat prefix not found", l + 1));
        return;
    }

    static void GuessRemix(ref ReadOnlySpan<char> title, out string? remixedBy, List<Warning>? warnings)
    {
        remixedBy = null;

        ReadOnlySpan<string> suffixes = ["remix vip", "remix"];

        int i = title.IndexOfAny(suffixes, StringComparison.InvariantCultureIgnoreCase);
        if (i == -1) return;

        {
            int l = i;
            int r = i;
            char closingBracket = default;
            while (l > 0 && !BracketPairs.TryGetValue(title[l], out closingBracket)) l--;
            if (closingBracket != default)
            {
                while (r + 1 < title.Length && title[r] != closingBracket) r++;

                ReadOnlySpan<char> part = title[(l + 1)..r];

                foreach (string suffix in suffixes)
                {
                    if (!part.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)) continue;

                    part = part[..^suffix.Length].TrimEnd();

                    remixedBy = part.ToString();
                    title = title[..l].ToString().TrimEnd() + title[(r + 1)..].ToString();
                    return;
                }
            }
        }

        {
            string[] parts = Split(title.ToString(), " - ");
            if (parts.Length > 1)
            {
                for (int j = 0; j < parts.Length; j++)
                {
                    string? part = parts[j];
                    foreach (string suffix in suffixes)
                    {
                        if (!part.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)) continue;

                        part = part[..^suffix.Length].TrimEnd();

                        remixedBy = part.ToString();
                        title = string.Join(" - ", parts.Where((_, k) => k != j));
                        return;
                    }
                }
            }
        }

        warnings?.Add(new Warning($"Unimplemented remix format", i));
        return;
    }

    public static ImmutableArray<string> ParseArtists(string text)
    {
        string[] artistParts = [text];
        foreach (string separator in (string[])[" & ", ", ", " x "])
        {
            string[] v = Split(text, separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (v.Length > artistParts.Length) artistParts = v;
        }

        return [.. artistParts];
    }

    [DebuggerStepThrough]
    class GenericVideo(string uploader, string title)
    {
        public string Uploader { get; } = uploader;
        public string Title { get; } = title;
    }

    public static MusicMeta Guess(TagLib.Tag tag, List<Warning>? warnings = null)
    {
        MusicMeta submeta = Guess(tag.Title, warnings);

        if (!string.IsNullOrWhiteSpace(tag.Album)) submeta.Album = tag.Album;
        if (!string.IsNullOrWhiteSpace(tag.Copyright)) submeta.Copyright = tag.Copyright;
        if (tag.Year != default) submeta.Year = tag.Year;
        if (!string.IsNullOrWhiteSpace(tag.RemixedBy)) submeta.RemixedBy = tag.RemixedBy;
        if (tag.Performers.Length != 0) submeta.Performers = [.. tag.Performers.Where(v => !v.Equals(submeta.RemixedBy, StringComparison.InvariantCultureIgnoreCase) && !v.Equals(submeta.Featuring, StringComparison.InvariantCultureIgnoreCase))];

        return submeta;
    }

    static string RemoveDummies(string text)
    {
        Range[] brackets = GetBracketMeta(text);
        ImmutableArray<string> dummies = ["official", "music", "video", "audio", "visualizer", "original mix", "unreleased", "download"];

        bool IsDummy(string text)
        {
            foreach (string item in dummies)
            {
                if (text.Contains(item, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!text.Equals(item, StringComparison.InvariantCultureIgnoreCase))
                    {
                        string[] keywords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (string keyword in keywords)
                        {
                            if (!dummies.Contains(keyword, StringComparer.InvariantCultureIgnoreCase))
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        foreach (Range bracket in brackets.Reverse())
        {
            if (IsDummy(text[bracket]))
            {
                Range removeRange = Extend(bracket, 1, 1);
                text = RemoveRange(text, removeRange);
            }
        }

        foreach (string separator in new string[] { " - ", " | " })
        {
            string[] parts = Split(text, separator);
            if (parts.Length > 1)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (IsDummy(parts[i]))
                    {
                        text = string.Join(separator, parts.Where((_, j) => j != i));
                    }
                }
            }
        }

        //if (IsDummy(text))
        //{
        //    //Debugger.Break();
        //}

        return text;
    }

    static MusicMeta Guess(GenericVideo video, List<Warning>? warnings = null)
    {
        ReadOnlySpan<char> artist = Confusables.Replace(video.Uploader, Confusables.Equivalents).Trim();
        ReadOnlySpan<char> title = Confusables.Replace(video.Title, Confusables.Equivalents).Trim();

        const string TopicSuffix = " - Topic";

        bool isTopic = false;

        if (artist.EndsWith(TopicSuffix, StringComparison.InvariantCulture))
        {
            artist = artist[..^TopicSuffix.Length].TrimEnd();
            isTopic = true;
        }

        title = RemoveDummies(title.ToString());
        title = RemoveExtraWhitespace(title.ToString());

        ImmutableArray<string> artists = [artist.ToString()];

        if (!isTopic)
        {
            string[] titleSegments = [title.ToString()];
            string? separator = null;
            string[] separators = [" - ", " | "];
            foreach (string _separator in separators)
            {
                string[] v = Split(title.ToString(), _separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
        GuessFeaturing(ref title, out string? featuring, warnings);

        if (string.IsNullOrWhiteSpace(remixedBy)) remixedBy = null;
        if (string.IsNullOrWhiteSpace(featuring)) featuring = null;

        artists = [.. artists.Where(artist => !string.Equals(remixedBy, artist, StringComparison.InvariantCultureIgnoreCase))];

        if (artist.Equals("Release", StringComparison.InvariantCulture)) artists = [];

        return new MusicMeta(artists, title.ToString())
        {
            RemixedBy = remixedBy,
            Featuring = featuring,
        };
    }

    public static MusicMeta Guess(PlaylistVideo video, List<Warning>? warnings = null) => Guess(new GenericVideo(video.Author.ChannelTitle, video.Title), warnings);

    public static MusicMeta Guess(YoutubeExplode.Videos.Video video, List<Warning>? warnings = null)
    {
        MusicMeta res = Guess(new GenericVideo(video.Author.ChannelTitle, video.Title), warnings);

        List<string> _warnings = [];

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
                        res.Year = _year;
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
                    if (res.Performers.IsNullOrEmpty() || IsMentioned(name))
                    {
                        artists.Add(name);
                    }
                }
                else if (role.Any(r => r is "Authors"))
                {
                    foreach (string n in Split(name, ", "))
                    {
                        if (res.Performers.IsNullOrEmpty() || IsMentioned(n))
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
                if ((res.Title is not null && res.Title.Equals(keyword, StringComparison.InvariantCultureIgnoreCase)) || res.GetTitleText().Equals(keyword, StringComparison.InvariantCultureIgnoreCase)) continue;
                if (artists.Contains(keyword)) continue;
                if (remixers.Contains(keyword)) continue;
                if (publishers.Contains(keyword)) continue;
                if (composers.Contains(keyword)) continue;

                if (res.Performers.Any(v => v.Equals(keyword, StringComparison.InvariantCultureIgnoreCase)))
                {
                    artists.Add(keyword);
                    continue;
                }

                //List<string> role = roles.TryGetValue(keyword, out List<string>? _0) ? _0 : [];
                //if (role.Count == 0) continue;
            }


            res.Copyright = desc.Copyright;

            if (!desc.Album.Equals(res.Album, StringComparison.InvariantCultureIgnoreCase))
            {
                res.Album = desc.Album;
            }

            if (remixers.Count == 1)
            {
                res.RemixedBy = remixers.First();
            }
            else if (remixers.Count > 1)
            {
                warnings?.Add($"Multiple remixers");
            }

            if (artists.Count > 0)
            {
                res.Performers = [.. artists];
            }

            warnings?.AddRange(_warnings);
        }

        return res;
    }

    public static MusicMeta Guess(ReadOnlySpan<char> text, List<Warning>? warnings = null)
    {
        text = RemoveDummies(text.ToString());
        text = RemoveExtraWhitespace(text.ToString());

        GuessRemix(ref text, out string? remixedBy, warnings);
        GuessFeaturing(ref text, out string? featuring, warnings);

        ReadOnlySpan<string> parts = Split(text.ToString(), " - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            warnings?.Add(new Warning($"It has {parts.Length} part", 0));
            return new MusicMeta([], text.ToString()) { RemixedBy = remixedBy };
        }

        if (parts.Length > 2)
        {
            if (parts[1].Equals(parts[0], StringComparison.InvariantCultureIgnoreCase))
            {
                parts = parts[1..];
            }
        }

        if (parts.Length >= 2 && remixedBy is null)
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

        if (parts.Length == 1)
        {
            return new MusicMeta([], parts[0]) { RemixedBy = remixedBy };
        }

        if (parts.Length > 2)
        {
            warnings?.Add(new Warning($"It has more than two parts", 0));
        }

        ImmutableArray<string> artists = [.. ParseArtists(parts[0]).Where(artist => !string.Equals(remixedBy, artist, StringComparison.InvariantCultureIgnoreCase))];
        string title = string.Join(" - ", parts[1..]);

        return new MusicMeta(artists, title)
        {
            RemixedBy = remixedBy,
            Featuring = featuring,
        };
    }

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

        ImmutableArray<string> keywords = [.. Split(lines[2], " · ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(v => Confusables.Replace(v, Confusables.Equivalents))];

        string album = Confusables.Replace(lines[4], Confusables.Equivalents);

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

            string[] k = Split(line.Split(':')[0].Trim(), ",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string v = Confusables.Replace(line.Split(':')[1], Confusables.Equivalents).Trim();

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
            Keywords = keywords,
            Album = album,
            Copyright = copyright,
            Metadata = [.. metadata.Select(v => new KeyValuePair<string, ImmutableArray<string>>(v.Key, [.. v.Value]))],
        };
    }
}
