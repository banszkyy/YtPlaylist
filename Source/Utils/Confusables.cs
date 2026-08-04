using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace YtPlaylist;

static class Confusables
{
    static ImmutableDictionary<int, string?>? _confusables;

    public static async Task<ImmutableDictionary<int, string?>> Fetch(AppArguments arguments)
    {
        if (_confusables is not null) return _confusables;

        Directory.CreateDirectory(arguments.HttpCachePath);
        string localPath = Path.Combine(arguments.HttpCachePath, "confusables.txt");
        string text;
        if (File.Exists(localPath))
        {
            text = File.ReadAllText(localPath);
        }
        else
        {
            HttpClient httpClient = new();
            text = await httpClient.GetStringAsync("https://www.unicode.org/Public/security/latest/confusables.txt");
            File.WriteAllText(localPath, text);
        }

        List<KeyValuePair<int, string?>> res = [];
        Span<Range> cols = stackalloc Range[3];

        foreach (string _item in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            ReadOnlySpan<char> item = _item;
            int i = item.IndexOf('#');
            if (i != -1) item = item[..i].TrimEnd();
            int colCount = item.Split(cols, ';', StringSplitOptions.TrimEntries);
            if (colCount < 2) continue;

            ReadOnlySpan<char> key = item[cols[0]];
            ReadOnlySpan<char> val = item[cols[1]];

            if (key.Count(' ') > 0) throw new FormatException();
            int keyV = int.Parse(key, NumberStyles.HexNumber);

            StringBuilder valV = new();
            foreach (Range v in val.Split(' '))
            {
                valV.Append(char.ConvertFromUtf32(int.Parse(val[v], NumberStyles.HexNumber)));
            }

            res.Add(new(keyV, valV.ToString()));
        }

        return _confusables = [.. res];
    }

    [return: NotNullIfNotNull(nameof(value))]
    public static string? Replace(string? value, ImmutableDictionary<int, string?> map)
    {
        if (value is null) return null;

        StringBuilder res = new(value.Length);

        foreach (int item in value.ToUtf32())
        {
            if (map.TryGetValue(item, out string? v))
            {
                res.Append(v);
            }
            else
            {
                //if (item >= 128 && map.Count != 12 && map.Count != 6) Debugger.Break();
                res.Append(char.ConvertFromUtf32(item));
            }
        }

        return res.ToString();
    }

    public static readonly List<string> CollectedNonAsciiCharacters = [];

    static List<int> ToUtf32(this string value)
    {
        List<int> codePoints = new(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            codePoints.Add(char.ConvertToUtf32(value, i));
            if (char.IsHighSurrogate(value[i])) i += 1;
        }

        return codePoints;
    }

    static int ToOneUtf32(this string value)
    {
        int i = 0;

        int res = char.ConvertToUtf32(value, i);
        if (char.IsHighSurrogate(value[i])) i += 1;

        if (i < value.Length) throw new ArgumentException($"It contains more than one character", nameof(value));

        return res;
    }

    public static ImmutableDictionary<int, string?> CompileMap(IEnumerable<KeyValuePair<string, string?>> map)
    {
        Dictionary<int, string?> res = [];

        foreach (KeyValuePair<string, string?> item in map)
        {
            foreach (int k in item.Key.ToUtf32())
            {
                res.Add(k, item.Value);
            }
        }

        return res.ToImmutableDictionary();
    }

    public static ImmutableDictionary<int, string?> CombineMaps(IReadOnlyDictionary<int, string?> a, IReadOnlyDictionary<int, string?> b)
    {
        Dictionary<int, string?> res = new(a.Count + b.Count);

        foreach (KeyValuePair<int, string?> item in a)
        {
            res.Add(item.Key, item.Value);
        }

        foreach (KeyValuePair<int, string?> item in b)
        {
            res[item.Key] = item.Value;
        }

        return res.ToImmutableDictionary();
    }

    public static readonly ImmutableDictionary<int, string?> Equivalents = CompileMap(new Dictionary<string, string?>()
    {
        { "`´‘’", "'" },
        { "‐–—ー", "-" },
        { "“”„", "\"" },
        { "…", "..." },
        { "｜", "|" }
    });

    public static readonly ImmutableDictionary<int, string?> Accents = CompileMap(new Dictionary<string, string?>()
    {
        { "ÆǼ", "AE" },
        { "ǽæ", "ae" },
        { "œ", "oe" },
        { "ÀÁÂÃÄÅǺĀĂĄǍΑΆẢẠẦẪẨẬẰẮẴẲẶА", "A" },
        { "äàáâãåǻāăąǎªαάảạầấẫẩậằắẵẳặа", "a" },
        { "ÇĆĈĊČ", "C" },
        { "çćĉċč", "c" },
        { "ÐĎĐ", "D" },
        { "ðďđ", "d" },
        { "ÈÉÊËĒĔĖĘĚΕΈẼẺẸỀẾỄỂỆЕ", "E" },
        { "èéêëēĕėęěέεẽẻẹềếễểệе", "e" },
        { "ĜĞĠĢ", "G" },
        { "ĝğġģ", "g" },
        { "Ĥ", "H" },
        { "ĥħ", "h" },
        { "ÌÍÎÏĨĪĬǏĮİΗΉΊΙΪỈỊ", "I" },
        { "ìíîïĩīĭǐįıίιϊỉịї", "i" },
        { "Ĵ", "J" },
        { "ĵ", "j" },
        { "ĶΚК", "K" },
        { "ķκк", "k" },
        { "ĹĻĽĿŁ", "L" },
        { "ĺļľŀł", "l" },
        { "М", "M" },
        { "ÑŃŅŇΝ", "N" },
        { "ñńņňŉ", "n" },
        { "ÖÒÓÔÕŌŎǑŐƠØǾΟΌỎỌỒỐỖỔỘỜỚỠỞỢО", "O" },
        { "öòóôõōŏǒőơøǿºοόỏọồốỗổộờớỡởợо", "o" },
        { "ŔŖŘ", "R" },
        { "ŕŗř", "r" },
        { "ŚŜŞȘŠ", "S" },
        { "śŝşșš", "s" },
        { "ȚŢŤŦτТ", "T" },
        { "țţťŧт", "t" },
        { "ÜÙÚÛŪŬŮŰŲƯǓǕǗǙǛŨỦỤỪỨỮỬỰ", "U" },
        { "üùúûũūŭůűųưǔǖǘǚǜυύϋủụừứữửự", "u" },
        { "ÝŸŶΥΎΫỲỸỶỴ", "Y" },
        { "ýÿŷỳỹỷỵ", "y" },
        { "Ŵ", "W" },
        { "ŵ", "w" },
        { "ŹŻŽΖ", "Z" },
        { "źżž", "z" },
        { string.Join(null, Enumerable.Range(0x0300, 0x005F).Select(v => (char)v)), null }
    });
}
