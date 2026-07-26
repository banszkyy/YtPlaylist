using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace YtPlaylist;

public class MetaStringEqualityComparer(ImmutableDictionary<int, string?> map, StringComparison comparisonType) : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y) => MetaGuesser.Same(x, y, map, comparisonType);
    public int GetHashCode([DisallowNull] string obj) => Confusables.Replace(obj, map).GetHashCode(comparisonType);
}

public static partial class MetaGuesser
{
    public static bool Same(string? a, string? b, ImmutableDictionary<int, string?> map, StringComparison comparisonType)
    {
        if (string.Equals(a, b, comparisonType)) return true;
        a = Confusables.Replace(a, map);
        b = Confusables.Replace(b, map);
        return string.Equals(a, b, comparisonType);
    }

    public static string[] Split(string text, string separator, StringSplitOptions options = default)
    {
        List<string> result = [];
        int bracketStack = 0;
        int segmentStart = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (BracketPairs.ContainsKey(text[i]))
            {
                bracketStack++;
                continue;
            }

            if (bracketStack > 0 && BracketPairs.Values.Contains(text[i]))
            {
                bracketStack--;
                continue;
            }

            if (bracketStack == 0 && text[i..].StartsWith(separator, StringComparison.Ordinal))
            {
                string segment = segmentStart <= i ? text[segmentStart..i] : string.Empty;
                if (options.HasFlag(StringSplitOptions.TrimEntries)) segment = segment.Trim();
                if (options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && segment.Length == 0) goto skip;
                result.Add(segment);
            skip:
                segmentStart = i + separator.Length;
            }
        }

        {
            string segment = text[segmentStart..];
            if (options.HasFlag(StringSplitOptions.TrimEntries)) segment = segment.Trim();
            if (options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && segment.Length == 0) goto skip;
            result.Add(segment);
        skip:;
        }

        return [.. result];
    }

    static int IndexOfAny(this ReadOnlySpan<char> value, ReadOnlySpan<string> values, StringComparison comparisonType)
    {
        int res = -1;

        foreach (string item in values)
        {
            int i = value.IndexOf(item, comparisonType);
            if (i != -1 && (res == -1 || i < res))
            {
                res = i;
            }
        }

        return res;
    }

    static Range[] GetBracketMeta(string text)
    {
        List<Range> res = [];
        int depth = 0;
        int start = -1;

        for (int i = 0; i < text.Length; i++)
        {
            if (depth > 0 && BracketPairs.Values.Contains(text[i]))
            {
                depth--;
                if (depth == 0 && i > start && start > 0)
                {
                    res.Add(start..i);
                }
            }
            else if (BracketPairs.ContainsKey(text[i]))
            {
                depth++;
                if (depth == 1)
                {
                    start = i + 1;
                }
            }
        }

        return [.. res];
    }

    static Range Extend(Range range, int start, int end) => new(
        new Index(range.Start.Value - start * (range.Start.IsFromEnd ? -1 : +1), range.Start.IsFromEnd),
        new Index(range.End.Value + end * (range.End.IsFromEnd ? -1 : +1), range.End.IsFromEnd)
    );

    static string RemoveRange(string text, Range range)
    {
        (int offset, int length) = range.GetOffsetAndLength(text.Length);
        return text.Remove(offset, length);
    }

    static string RemoveExtraWhitespace(string text)
    {
        int start = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                if (start == -1) start = i;
            }
            else if (start != -1)
            {
                int length = i - start;
                if (length > 1)
                {
                    text = text.Remove(start, length - 1);
                    i = start - 1;
                }
                start = -1;
            }
        }
        return text.Trim();
    }

}