namespace YtPlaylist.PLS;

public class PLSExporter
{
    public static void WriteTo(Playlist playlist, string filename)
    {
        using FileStream file = new(filename, FileMode.Create, FileAccess.Write);
        using StreamWriter writer = new(file);

        writer.WriteLine($"[playlist]");
        for (int i = 0; i < playlist.Musics.Count; i++)
        {
            MusicFile music = playlist.Musics[i];
            writer.WriteLine($"File{i + 1}={Path.GetRelativePath(Path.GetDirectoryName(filename)!, music.Path)}");
            writer.WriteLine($"Title{i + 1}={music.Meta}");
            if (music.PlaylistVideo is not null && music.PlaylistVideo.Duration.HasValue) writer.WriteLine($"Length{i + 1}={music.PlaylistVideo.Duration.Value.TotalSeconds}");
        }
        writer.WriteLine($"NumberOfEntries={playlist.Musics.Count}");
        writer.WriteLine($"Version=2");
    }
}
