using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using YoutubeExplode.Playlists;

namespace YtPlaylist;

public static partial class MetaGuesser
{
    public readonly struct Meta(ImmutableArray<string> artists, string title, string? remixedBy) : IEquatable<Meta>
    {
        public readonly ImmutableArray<string> Artists = artists;
        public readonly string Title = title;
        public readonly string? RemixedBy = remixedBy;

        public string GetArtistsText() => string.Join(" & ", Artists);
        public string GetTitleText() => $"{Title}{(RemixedBy is null ? null : $" ({RemixedBy} remix)")}";

        public bool Equals(Meta other) => Equals(other, StringComparison.Ordinal);

        public bool Equals(Meta other, StringComparison stringComparisonType)
        {
            if (Artists.Length != other.Artists.Length) return false;
            for (int i = 0; i < Artists.Length; i++)
            {
                if (!string.Equals(Artists[i], other.Artists[i], stringComparisonType)) return false;
            }
            if (!string.Equals(Title, other.Title, stringComparisonType)) return false;
            if (!string.Equals(RemixedBy, other.RemixedBy, stringComparisonType)) return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is Meta other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Artists, Title, RemixedBy);

        public static bool operator ==(Meta left, Meta right) => left.Equals(right);
        public static bool operator !=(Meta left, Meta right) => !(left == right);

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

    public static Meta Guess(PlaylistVideo video, List<Warning>? warnings = null)
    {
        ReadOnlySpan<char> artist = video.Author.ChannelTitle.Trim();
        ReadOnlySpan<char> title = video.Title.Trim();

        const string TopicSuffix = " - Topic";

        if (artist.EndsWith(TopicSuffix, StringComparison.InvariantCulture))
        {
            artist = artist[..^TopicSuffix.Length].TrimEnd();
        }

        Match dummyMatch = DummySuffixRegex.Match(title.ToString());
        if (dummyMatch.Success)
        {
            if (dummyMatch.Index + dummyMatch.Length == title.Length)
            {
                title = title[..dummyMatch.Index].TrimEnd();
                if (title.EndsWith(" -")) title = title[..^2];
            }
            else
            {
                int l = dummyMatch.Index;
                int r = l + dummyMatch.Length;
                title = title[..l].TrimEnd().ToString() + title[r..].ToString();
            }
        }

        string[] titleSegments = title.ToString().Split(" - ");

        ImmutableArray<string> artists;

        if (titleSegments.Length > 1)
        {
            artists = ParseArtists(titleSegments[0]);
            if (artists.Length != 1 || !artists[0].Equals(artist.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                warnings?.Add(new Warning($"Guessing artists from video title", 0));
            }
            title = string.Join(" - ", titleSegments[1..]);
        }
        else
        {
            artists = [artist.ToString()];
        }

        GuessRemix(ref title, out string? remixedBy, warnings);

        artists = [.. artists.Where(artist => !string.Equals(remixedBy, artist, StringComparison.InvariantCultureIgnoreCase))];

        return new Meta(artists, title.ToString(), remixedBy);
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
}