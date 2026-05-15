using YoutubeExplode.Playlists;

namespace YtPlaylist;

public class MusicFile(string path, string id, Playlist playlist)
{
    public string Path { get; } = path;
    public string Id { get; } = id;
    public Playlist Playlist { get; } = playlist;
    public PlaylistVideo? Video { get; set; }
}