using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Hqub.MusicBrainz;
using Hqub.MusicBrainz.Entities;

namespace YtPlaylist;

static class Extensions
{
    public static string[] SplitAll(this string value, char[] separators, StringSplitOptions options = StringSplitOptions.None)
    {
        List<string> res = [];

        int j = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (separators.Contains(value[i]))
            {
                string v = value[j..i];
                if (options.HasFlag(StringSplitOptions.TrimEntries)) v = v.Trim();
                if (options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && v.Length == 0) goto skip;
                res.Add(v);
            skip:
                j = i + 1;
            }
        }

        if (j < value.Length)
        {
            string v = value[j..];
            if (options.HasFlag(StringSplitOptions.TrimEntries)) v = v.Trim();
            if (!options.HasFlag(StringSplitOptions.RemoveEmptyEntries) || v.Length != 0)
            {
                res.Add(v);
            }
        }

        return [.. res];
    }

    public static string[] SplitAll(this string value, string[] separators, StringSplitOptions options = StringSplitOptions.None)
    {
        List<string> res = [];

        int j = 0;
        for (int i = 0; i < value.Length; i++)
        {
            foreach (string separator in separators)
            {
                if (value[i..].StartsWith(separator))
                {
                    string v = value[j..i];
                    if (options.HasFlag(StringSplitOptions.TrimEntries)) v = v.Trim();
                    if (options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && v.Length == 0) goto skip;
                    res.Add(v);
                skip:
                    j = i + separator.Length;
                    break;
                }
            }
        }

        if (j < value.Length)
        {
            string v = value[j..];
            if (options.HasFlag(StringSplitOptions.TrimEntries)) v = v.Trim();
            if (!options.HasFlag(StringSplitOptions.RemoveEmptyEntries) || v.Length != 0)
            {
                res.Add(v);
            }
        }

        return [.. res];
    }

    public static bool IsEmpty(this IEnumerable objects)
    {
        IEnumerator enumerator = objects.GetEnumerator();
        if (enumerator.MoveNext()) return false;
        return true;
    }

    public static string TrimStart(this string v, string value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        return v.StartsWith(value, comparison) ? v[value.Length..] : v;
    }

    public static string TrimEnd(this string v, string value, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        return v.EndsWith(value, comparison) ? v[..^value.Length] : v;
    }

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyList<T>? list) => list is null || list.Count == 0;
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this QueryResult<T>? list) where T : IEntity => list is null || list.Count == 0;

    public static string Quote(this string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "";
        }

        if (s.IndexOf(' ') < 0)
        {
            return s;
        }

        return "\"" + s + "\"";
    }

    public static IEnumerable<(T Value, bool IsFirst)> WithSeparators<T>(this IEnumerable<T> values)
    {
        bool isFirst = true;
        foreach (T item in values)
        {
            yield return (item, isFirst);
            isFirst = false;
        }
    }

    public static void Add(this List<MetaGuesser.Warning> list, string warning) => list.Add(new MetaGuesser.Warning(warning, 0));

    public static void AddRange(this List<MetaGuesser.Warning> list, IEnumerable<string> warnings)
    {
        foreach (string warning in warnings)
        {
            list.Add(warning);
        }
    }
}
