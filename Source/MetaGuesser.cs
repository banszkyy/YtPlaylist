using System.Collections.Frozen;
using System.Collections.Immutable;
using YoutubeExplode.Playlists;

namespace YtPlaylist;

public static class MetaGuesser
{
    public readonly struct Meta(ImmutableArray<string> artists, string title, string? remixedBy) : IEquatable<Meta>
    {
        public readonly ImmutableArray<string> Artists = artists;
        public readonly string Title = title;
        public readonly string? RemixedBy = remixedBy;

        public bool Equals(Meta other)
        {
            return Equals(other, StringComparison.Ordinal);
        }

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

        public override bool Equals(object? obj)
        {
            return obj is Meta other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Artists, Title, RemixedBy);
        }

        public static bool operator ==(Meta left, Meta right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Meta left, Meta right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{string.Join(" & ", Artists)} - {Title}{(!string.IsNullOrEmpty(RemixedBy) ? $" ({RemixedBy} Remix)" : "")}";
        }
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
        while (l > 0 && !BracketPairs.ContainsKey(title[l])) l--;
        while (r + 1 < title.Length && !BracketPairs.ContainsKey(title[r])) r++;
        if (r + 1 != title.Length)
        {
            warnings?.Add(new Warning($"Remix part is not at the end", r + 1));
            return;
        }

        ReadOnlySpan<char> part = title[l..(r + 1)];
        if (BracketPairs.TryGetValue(part[0], out char expectedClosingBracket))
        {
            if (part[^1] != expectedClosingBracket)
            {
                warnings?.Add(new Warning($"Expected closing bracket '{expectedClosingBracket}' but found '{part[^1]}'", l + part.Length - 1));
                return;
            }

            part = part[1..^1];

            foreach (string suffix in (string[])["remix", "remix vip",])
            {
                if (!part.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)) continue;

                part = part[..^suffix.Length].TrimEnd();

                remixedBy = part.ToString();
                title = title[..l].TrimEnd();
                return;
            }

            warnings?.Add(new Warning($"Remix suffix not found", l + 1));
            return;
        }

        warnings?.Add(new Warning($"Couldn't parse the remix", i));
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
        ReadOnlySpan<char> artist = video.Author.ChannelTitle;
        ReadOnlySpan<char> title = video.Title;

        artist = artist.TrimEnd(" - Topic").TrimEnd();

        if (title.StartsWith($"{artist} - ", StringComparison.InvariantCultureIgnoreCase))
        {
            title = title[(artist.Length + 3)..].TrimStart();
        }

        GuessRemix(ref title, out string? remixedBy, warnings);

        ImmutableArray<string> artists = [.. ParseArtists(artist.ToString()).Where(artist => !string.Equals(remixedBy, artist, StringComparison.InvariantCultureIgnoreCase))];

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
                foreach (string suffix in (string[])["remix", "remix vip",])
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
}