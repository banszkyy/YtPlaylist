using Logger;
using YoutubeExplode.Playlists;

namespace YtPlaylist;

public class MusicFile(string path, string id, Playlist playlist)
{
    public string Path { get; set; } = path;
    public string Id { get; } = id;
    public Playlist Playlist { get; } = playlist;
    public PlaylistVideo? Video { get; set; }

    public static void Delete(MusicFile file)
    {
        Delete(file.Path);
    }

    public static void Delete(string file)
    {
        Log.MinorAction($"Deleting file {file}");
        File.Delete(file);
        string lyricsFilename = System.IO.Path.ChangeExtension(file, ".lrc");
        if (File.Exists(lyricsFilename))
        {
            Log.MinorAction($"Deleting file {lyricsFilename}");
            File.Delete(lyricsFilename);
        }
    }

    public override string ToString()
    {
        return System.IO.Path.GetFileNameWithoutExtension(Path);
    }
}