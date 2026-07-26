namespace YtPlaylist;

public class Playlist(string title, YoutubeExplode.Playlists.Playlist youtubePlaylist, List<MusicFile>? musics = null)
{
    public readonly string Title = title;
    public readonly List<MusicFile> Musics = musics ?? [];
    public readonly YoutubeExplode.Playlists.Playlist YouTubePlaylist = youtubePlaylist;
}
