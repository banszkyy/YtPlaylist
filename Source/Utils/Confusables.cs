using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace YtPlaylist;

partial class Confusables
{
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
}
