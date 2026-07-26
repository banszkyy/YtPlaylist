using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YtPlaylist;

public sealed class YouTubeCache(string path)
{
    public void SavePlaylist(YoutubeExplode.Playlists.Playlist playlist)
    {
        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, $"p{playlist.Id.Value}");

        using FileStream file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        using BinaryWriter writer = new(file);

        writer.Write(playlist.Id.Value);
        writer.Write(playlist.Title);
        writer.Write(playlist.Description);

        writer.WriteList(playlist.Thumbnails, v =>
        {
            writer.Write(v.Url);
            writer.Write(v.Resolution.Width);
            writer.Write(v.Resolution.Height);
        });

        writer.Write(playlist.Count);

        writer.Write(playlist.Author is not null);
        if (playlist.Author is not null)
        {
            writer.Write(playlist.Author.ChannelId);
            writer.Write(playlist.Author.ChannelTitle);
        }
    }

    public bool LoadPlaylist(string playlistId, [NotNullWhen(true)] out YoutubeExplode.Playlists.Playlist? playlist)
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

        List<Thumbnail> thumbnails = reader.ReadList(() =>
        {
            string _url = reader.ReadString();
            int w = reader.ReadInt32();
            int h = reader.ReadInt32();
            return new Thumbnail(_url, new Resolution(w, h));
        });

        int? count = reader.ReadNullableInt32();

        Author? author = reader.ReadNullable(() =>
        {
            string channelId = reader.ReadString();
            string channelTitle = reader.ReadString();
            return new Author(channelId, channelTitle);
        });

        if (id != playlistId)
        {
            playlist = null;
            return false;
        }

        playlist = new YoutubeExplode.Playlists.Playlist(id, title, author, description, count, thumbnails);
        return true;
    }

    public void SavePlaylistItems(string playlistId, IReadOnlyCollection<PlaylistVideo> videos)
    {
        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, $"l{playlistId}");

        using FileStream file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        using BinaryWriter writer = new(file);

        writer.WriteList(videos, v =>
        {
            writer.Write(v.Id);
            writer.Write(v.Title);
            writer.Write(v.Author.ChannelId);
            writer.Write(v.Author.ChannelTitle);
            writer.Write(v.Duration, writer.Write);
            writer.WriteList(v.Thumbnails, w =>
            {
                writer.Write(w.Url);
                writer.Write(w.Resolution.Width);
                writer.Write(w.Resolution.Height);
            });
        });
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

        List<PlaylistVideo> result = reader.ReadList(() =>
        {
            string id = reader.ReadString();
            string title = reader.ReadString();
            string channelId = reader.ReadString();
            string channelTitle = reader.ReadString();
            TimeSpan? duration = reader.ReadNullable(reader.ReadTimeSpan);
            List<Thumbnail> thumbnails = reader.ReadList(() =>
            {
                string url = reader.ReadString();
                int w = reader.ReadInt32();
                int h = reader.ReadInt32();
                return new Thumbnail(url, new Resolution(w, h));
            });
            return new PlaylistVideo(playlistId, id, title, new Author(channelId, channelTitle), duration, thumbnails);
        });

        videos = [.. result];
        return true;
    }

    public void SaveVideo(Video video)
    {
        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, $"v{video.Id.Value}");

        using FileStream file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        using BinaryWriter writer = new(file);

        writer.Write(video.Title);
        writer.Write(video.Author.ChannelId);
        writer.Write(video.Author.ChannelTitle);
        writer.Write(video.UploadDate);
        writer.Write(video.Description);
        writer.Write(video.Duration, writer.Write);
        writer.WriteList(video.Thumbnails, v =>
        {
            writer.Write(v.Url);
            writer.Write(v.Resolution.Width);
            writer.Write(v.Resolution.Height);
        });
        writer.WriteList(video.Keywords, writer.Write);
        writer.Write(video.Engagement.ViewCount);
        writer.Write(video.Engagement.LikeCount);
        writer.Write(video.Engagement.DislikeCount);
    }

    public bool LoadVideo(string videoId, [NotNullWhen(true)] out Video? video)
    {
        string filePath = Path.Combine(path, $"v{videoId}");
        if (!File.Exists(filePath))
        {
            video = default;
            return false;
        }

        using FileStream file = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(file);

        string title = reader.ReadString();
        string channelId = reader.ReadString();
        string channelTitle = reader.ReadString();
        DateTimeOffset uploadDate = reader.ReadDateTimeOffset();
        string description = reader.ReadString();
        TimeSpan? duration = reader.ReadNullable(reader.ReadTimeSpan);
        List<Thumbnail> thumbnails = reader.ReadList(() =>
        {
            string url = reader.ReadString();
            int w = reader.ReadInt32();
            int h = reader.ReadInt32();
            return new Thumbnail(url, new Resolution(w, h));
        });
        List<string> keywords = reader.ReadList(reader.ReadString);
        long viewCount = reader.ReadInt64();
        long likeCount = reader.ReadInt64();
        long dislikeCount = reader.ReadInt64();

        video = new Video(videoId, title, new Author(channelId, channelTitle), uploadDate, description, duration, thumbnails, keywords, new Engagement(viewCount, likeCount, dislikeCount));
        return true;
    }
}
