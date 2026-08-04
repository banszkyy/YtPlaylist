using System.Collections.Immutable;

namespace YtPlaylist;

sealed class NetscapeCookieFile
{
    public readonly record struct Cookie(string Domain, bool IncludeSubdomains, string Path, bool Secure, long Expires, string Name, string Value);

    public static ImmutableArray<Cookie> Parse(string text)
    {
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Span<Range> cols = stackalloc Range[7];
        List<Cookie> res = [];
        for (int i = 0; i < lines.Length; i++)
        {
            ReadOnlySpan<char> line = lines[i];
            if (line[0] == '#') continue;
            int l = line.Split(cols, '\t');
            if (l != 7) continue;
            res.Add(new Cookie(
                Domain: line[cols[0]].ToString(),
                IncludeSubdomains: line[cols[1]].Equals("true", StringComparison.OrdinalIgnoreCase),
                Path: line[cols[2]].ToString(),
                Secure: line[cols[3]].Equals("true", StringComparison.OrdinalIgnoreCase),
                Expires: long.Parse(line[cols[4]]),
                Name: line[cols[5]].ToString(),
                Value: line[cols[6]].ToString()
            ));
        }
        return [.. res];
    }
}
