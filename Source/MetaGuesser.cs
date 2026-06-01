using System.Collections.Frozen;
using System.Collections.Immutable;
using YoutubeExplode.Playlists;

namespace YtPlaylist;

public static class MetaGuesser
{
    public readonly struct Meta(ImmutableArray<string> artists, string title, string? remixedBy)
    {
        public readonly ImmutableArray<string> Artists = artists;
        public readonly string Title = title;
        public readonly string? RemixedBy = remixedBy;
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

    public static Meta Guess(PlaylistVideo video, List<Warning> warnings)
    {
        string artist = video.Author.ChannelTitle;
        string title = video.Title;

        if (title.StartsWith($"{artist} - ", StringComparison.InvariantCultureIgnoreCase))
        {
            title = title[(artist.Length + 3)..].TrimStart();
        }

        artist = artist.TrimEnd(" - Topic").TrimEnd();
        string[] artists = artist.Split('&', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return new Meta([.. artists], title, null);
    }

    public static Meta Guess(string name, List<Warning> warnings)
    {
        ReadOnlySpan<char> _name = name.AsSpan();

        string? remixedBy = null;
        if (_name.Contains("remix", StringComparison.InvariantCultureIgnoreCase))
        {
            int i = _name.IndexOf("remix", StringComparison.InvariantCultureIgnoreCase);
            int l = i;
            int r = i;
            while (l > 0 && !BracketPairs.ContainsKey(_name[l])) l--;
            while (r + 1 < _name.Length && !BracketPairs.ContainsKey(_name[r])) r++;
            if (r + 1 != _name.Length)
            {
                warnings.Add(new Warning($"Remix part is not at the end", r + 1));
            }
            else
            {
                ReadOnlySpan<char> part = _name[l..(r + 1)];
                if (BracketPairs.TryGetValue(part[0], out char expectedClosingBracket))
                {
                    if (part[^1] == expectedClosingBracket)
                    {
                        part = part[1..^1];
                        foreach (string suffix in (string[])["remix", "remix vip",])
                        {
                            if (!part.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)) continue;

                            part = part[..^suffix.Length].TrimEnd();
                            remixedBy = part.ToString();

                            _name = _name[..l].TrimEnd();
                            name = _name.ToString();

                            goto ok;
                        }
                        warnings.Add(new Warning($"Remix suffix not found", l + 1));
                    ok:;
                    }
                    else
                    {
                        warnings.Add(new Warning($"Expected closing bracket '{expectedClosingBracket}' but found '{part[^1]}'", l + part.Length - 1));
                    }
                }
                else
                {
                    warnings.Add(new Warning($"Unknown bracket '{part[0]}'", l));
                }
            }
        }

        ReadOnlySpan<string> parts = name.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            warnings.Add(new Warning($"It has {parts.Length} part(s)", 0));
            return new Meta([], name, remixedBy);
        }

        if (parts.Length > 2)
        {
            if (parts[1].Contains(parts[0], StringComparison.InvariantCultureIgnoreCase))
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
                warnings.Add(new Warning($"Remix suffix not found", 0));
            ok:;
            }
        }

        if (parts.Length > 2)
        {
            warnings.Add(new Warning($"It has more than two parts", 0));
        }

        List<string> artists = [];

        string[] artistParts = [parts[0]];
        foreach (string separator in (string[])[" & ", ", ", " x "])
        {
            string[] v = parts[0].Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (v.Length > artistParts.Length) artistParts = v;
        }

        foreach (string artist in artistParts)
        {
            if (!string.Equals(remixedBy, artist, StringComparison.InvariantCultureIgnoreCase))
            {
                artists.Add(artist);
            }
        }

        string title = string.Join(" - ", parts[1..]);

        return new Meta([.. artists], title, remixedBy);
    }
}