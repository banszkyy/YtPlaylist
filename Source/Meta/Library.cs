namespace YtPlaylist;

public class Library : IDisposable
{
    public readonly List<Playlist> Playlists = [];
    public IEnumerable<MusicFile> Musics => Playlists.SelectMany(v => v.Musics).Distinct();

    public void Dispose()
    {
        foreach (MusicFile music in Musics)
        {
            music.Dispose();
        }
    }
}
