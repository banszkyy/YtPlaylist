using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;

namespace YtPlaylist;

static class Confusables
{
    public static async Task<ImmutableArray<KeyValuePair<string, string>>> Fetch(AppArguments arguments)
    {
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

        List<KeyValuePair<string, string>> res = [];

        foreach (string _item in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string item = _item;
            int i = item.IndexOf('#');
            if (i != -1) item = item[..i].TrimEnd();
            string[] cols = item.Split(';', StringSplitOptions.TrimEntries);
            if (cols.Length < 2) continue;

            string a = string.Concat(cols[0].Split(' ').Select(hex => char.ConvertFromUtf32(int.Parse(hex, NumberStyles.HexNumber))));
            string b = string.Concat(cols[1].Split(' ').Select(hex => char.ConvertFromUtf32(int.Parse(hex, NumberStyles.HexNumber))));

            res.Add(new(a, b));
        }

        return [.. res];
    }
}
