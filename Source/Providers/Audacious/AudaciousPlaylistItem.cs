namespace YtPlaylist.Audacious;

sealed class AudaciousPlaylistItem
{
    public required Uri Uri { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public string? Album { get; set; }
    public int Year { get; set; }
    public int TrackNumber { get; set; }
    public string[]? Genre { get; set; }
    public string? Copyright { get; set; }
    public required long Length { get; set; }
    public required int Bitrate { get; set; }
    public required int Channels { get; set; }
    public required string Codec { get; set; }
    public required string Quality { get; set; }
    public required long FileCreated { get; set; }
    public required long FileModified { get; set; }

    public static AudaciousPlaylistItem CreateEmpty() => new()
    {
        Uri = null!,
        Title = string.Empty,
        Artist = string.Empty,
        Length = 0,
        Bitrate = 0,
        Channels = 0,
        Codec = string.Empty,
        Quality = string.Empty,
        FileCreated = 0,
        FileModified = 0,
    };

    static void WriteProperty(StreamWriter writer, string name, string value)
    {
        writer.Write(name);
        writer.Write('=');
        writer.Write(value);
        writer.WriteLine();
    }

    public void SaveTo(StreamWriter writer)
    {
        WriteProperty(writer, "uri", Uri.AbsoluteUri);
        WriteProperty(writer, "title", Uri.EscapeDataString(Title));
        WriteProperty(writer, "artist", Uri.EscapeDataString(Artist));
        if (!string.IsNullOrWhiteSpace(Album)) WriteProperty(writer, "album", Uri.EscapeDataString(Album));
        if (Year != 0) WriteProperty(writer, "year", Year.ToString());
        if (TrackNumber != 0) WriteProperty(writer, "track-number", TrackNumber.ToString());
        if (Genre is not null) WriteProperty(writer, "genre", Uri.EscapeDataString(string.Join(';', Genre)));
        if (Copyright is not null) WriteProperty(writer, "copyright", Uri.EscapeDataString(Copyright));
        WriteProperty(writer, "length", Length.ToString());
        WriteProperty(writer, "bitrate", Bitrate.ToString());
        WriteProperty(writer, "channels", Channels.ToString());
        WriteProperty(writer, "codec", Uri.EscapeDataString(Codec));
        WriteProperty(writer, "quality", Uri.EscapeDataString(Quality));
        WriteProperty(writer, "file-created", FileCreated.ToString());
        WriteProperty(writer, "file-modified", FileModified.ToString());
    }

    public void PopulateFrom(Dictionary<string, string> properties)
    {
        foreach ((string k, string v) in properties)
        {
            switch (k)
            {
                case "uri": Uri = new Uri(v, UriKind.Absolute); break;
                case "file-created": FileCreated = long.Parse(v); break;
                case "file-modified": FileModified = long.Parse(v); break;

                case "length": Length = long.Parse(v); break;
                case "bitrate": Bitrate = int.Parse(v); break;
                case "channels": Channels = int.Parse(v); break;
                case "codec": Codec = Uri.UnescapeDataString(v); break;
                case "quality": Quality = Uri.UnescapeDataString(v); break;

                case "title": Title = Uri.UnescapeDataString(v); break;
                case "artist": Artist = Uri.UnescapeDataString(v); break;
                case "album": Album = Uri.UnescapeDataString(v); break;
                case "year": Year = int.Parse(v); break;
                case "track-number": TrackNumber = int.Parse(v); break;
                case "genre": Genre = Uri.EscapeDataString(v).Split(';'); break;
                case "copyright": Copyright = Uri.EscapeDataString(v); break;
                case "lyrics": break; // Skip

                default: throw new NotImplementedException($"{k}={v}");
            }
        }
    }
}
