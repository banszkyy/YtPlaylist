namespace YtPlaylist.M3U;

public class M3UExporter
{
    public static void WriteTo(Playlist playlist, string filename)
    {
        using FileStream file = new(filename, FileMode.Create, FileAccess.Write);
        using StreamWriter writer = new(file);

        writer.WriteLine($"#EXTM3U");
        foreach (MusicFile music in playlist.Musics)
        {
            writer.WriteLine($"#EXTINF:{music.PlaylistVideo?.Duration?.TotalSeconds ?? -1},{music.Meta}");
            writer.WriteLine(Path.GetRelativePath(Path.GetDirectoryName(filename)!, music.Path));
        }
    }
}
