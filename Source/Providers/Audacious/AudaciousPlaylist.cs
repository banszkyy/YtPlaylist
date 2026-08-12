
namespace YtPlaylist.Audacious;

sealed class AudaciousPlaylist
{
    public required string Path { get; init; }
    public required int Index { get; init; }
    public string Title { get; set; } = string.Empty;
    public List<AudaciousPlaylistItem> Items { get; } = [];

    static void WriteProperty(StreamWriter writer, string name, string value)
    {
        writer.Write(name);
        writer.Write('=');
        writer.Write(value);
        writer.WriteLine();
    }

    static KeyValuePair<string, string> ReadProperty(StreamReader reader)
    {
        string line = reader.ReadLine() ?? throw new EndOfStreamException();
        int i = line.IndexOf('=');
        return new KeyValuePair<string, string>(line[..i], line[(i + 1)..]);
    }

    static string ReadProperty(StreamReader reader, string name)
    {
        KeyValuePair<string, string> property = ReadProperty(reader);
        if (property.Key != name) throw new FormatException($"Expected property \"{name}\", got \"{property.Key}\"");
        return property.Value;
    }

    public void ReadFrom(StreamReader reader)
    {
        Title = Uri.UnescapeDataString(ReadProperty(reader, "title"));

        Dictionary<string, string> properties = [];
        while (!reader.EndOfStream)
        {
            KeyValuePair<string, string> prop = ReadProperty(reader);

            if (prop.Key == "uri")
            {
                if (properties.Count > 0)
                {
                    AudaciousPlaylistItem item = AudaciousPlaylistItem.CreateEmpty();
                    item.PopulateFrom(properties);
                    Items.Add(item);
                    properties.Clear();
                }
            }

            properties[prop.Key] = prop.Value;
        }

        if (properties.Count > 0)
        {
            AudaciousPlaylistItem item = AudaciousPlaylistItem.CreateEmpty();
            item.PopulateFrom(properties);
            Items.Add(item);
            properties.Clear();
        }
    }

    public void SaveTo(StreamWriter writer)
    {
        WriteProperty(writer, "title", Uri.EscapeDataString(Title));

        foreach (AudaciousPlaylistItem item in Items)
        {
            item.SaveTo(writer);
        }
    }

    public void SaveTo(string filename)
    {
        using FileStream file = File.Open(filename, FileMode.Create, FileAccess.Write);
        using StreamWriter writer = new(file);
        SaveTo(writer);
    }
}
