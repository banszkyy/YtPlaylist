using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;

namespace YtPlaylist;

public sealed class YouTubeCache(string path)
{
    public void SavePlaylist(Playlist playlist)
    {
        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, $"p{playlist.Id.Value}");

        using FileStream file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        using BinaryWriter writer = new(file);

        writer.Write(playlist.Id.Value);
        writer.Write(playlist.Title);
        writer.Write(playlist.Description);

        writer.Write(playlist.Thumbnails.Count);
        foreach (Thumbnail item in playlist.Thumbnails)
        {
            writer.Write(item.Url);
            writer.Write(item.Resolution.Width);
            writer.Write(item.Resolution.Height);
        }

        writer.Write(playlist.Count);

        writer.Write(playlist.Author is not null);
        if (playlist.Author is not null)
        {
            writer.Write(playlist.Author.ChannelId);
            writer.Write(playlist.Author.ChannelTitle);
        }
    }

    public bool LoadPlaylist(string playlistId, [NotNullWhen(true)] out Playlist? playlist)
    {
        string filePath = Path.Combine(path, $"p{playlistId}");
        if (!File.Exists(filePath))
        {
            playlist = null;
            return false;
        }

        using FileStream file = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(file);

        string id = reader.ReadString();
        string title = reader.ReadString();
        string description = reader.ReadString();

        int thumbnailCount = reader.ReadInt32();
        List<Thumbnail> thumbnails = new(thumbnailCount);
        for (int i = 0; i < thumbnailCount; i++)
        {
            string _url = reader.ReadString();
            int w = reader.ReadInt32();
            int h = reader.ReadInt32();
            thumbnails.Add(new Thumbnail(_url, new Resolution(w, h)));
        }

        int? count = reader.ReadInt32Nullable();

        Author? author = null;
        if (reader.ReadBoolean())
        {
            string channelId = reader.ReadString();
            string channelTitle = reader.ReadString();
            author = new Author(channelId, channelTitle);
        }

        if (id != playlistId)
        {
            playlist = null;
            return false;
        }

        playlist = new Playlist(id, title, author, description, count, thumbnails);
        return true;
    }

    public void SavePlaylistItems(string playlistId, IReadOnlyCollection<PlaylistVideo> videos)
    {
        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, $"l{playlistId}");

        using FileStream file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        using BinaryWriter writer = new(file);

        writer.Write(videos.Count);
        foreach (PlaylistVideo v in videos)
        {
            writer.Write(v.Id);
            writer.Write(v.Title);
            writer.Write(v.Author.ChannelId);
            writer.Write(v.Author.ChannelTitle);
            writer.Write(v.Duration.HasValue);
            if (v.Duration.HasValue) writer.Write(v.Duration.Value.Ticks);
            writer.Write(v.Thumbnails.Count);
            foreach (Thumbnail item in v.Thumbnails)
            {
                writer.Write(item.Url);
                writer.Write(item.Resolution.Width);
                writer.Write(item.Resolution.Height);
            }
        }
    }

    public bool LoadPlaylistItems(string playlistId, out ImmutableArray<PlaylistVideo> videos)
    {
        string filePath = Path.Combine(path, $"l{playlistId}");
        if (!File.Exists(filePath))
        {
            videos = default;
            return false;
        }

        using FileStream file = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(file);

        int count = reader.ReadInt32();
        List<PlaylistVideo> result = new(count);
        for (int i = 0; i < count; i++)
        {
            string id = reader.ReadString();
            string title = reader.ReadString();
            string channelId = reader.ReadString();
            string channelTitle = reader.ReadString();
            TimeSpan? duration = null;
            if (reader.ReadBoolean())
            {
                duration = TimeSpan.FromTicks(reader.ReadInt64());
            }
            int thumbnailCount = reader.ReadInt32();
            List<Thumbnail> thumbnails = new(thumbnailCount);
            for (int j = 0; j < thumbnailCount; j++)
            {
                string url = reader.ReadString();
                int w = reader.ReadInt32();
                int h = reader.ReadInt32();
                thumbnails.Add(new Thumbnail(url, new Resolution(w, h)));
            }
            result.Add(new PlaylistVideo(playlistId, id, title, new Author(channelId, channelTitle), duration, thumbnails));
        }

        videos = [.. result];
        return true;
    }
}
