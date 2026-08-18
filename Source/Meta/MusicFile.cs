using System.Diagnostics.CodeAnalysis;
using Logger;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YtPlaylist;

public class MusicFile(string path, string id, MusicMeta meta, Playlist playlist) : IDisposable
{
    public string Path = path;
    public readonly string Id = id;
    public readonly Playlist Playlist = playlist;
    public PlaylistVideo? PlaylistVideo;
    public Video? Video;
    public MusicMeta Meta = meta;
    public TagLib.File? TagsFile;
    public Diff? TagsDiff;

    [MemberNotNull(nameof(TagsFile))]
    [MemberNotNull(nameof(TagsDiff))]
    public void OpenTags(TagLib.ReadStyle propertiesStyle = TagLib.ReadStyle.PictureLazy)
    {
        TagsFile ??= TagLib.File.Create(Path, propertiesStyle);
        TagsDiff ??= new();
    }

    public bool SaveTags(bool dry = false)
    {
        if (TagsFile is null || TagsDiff is null) return false;
        if (TagsDiff.Changes.Count <= 0) return false;

        if (!dry) TagsFile.Save();
        Log.None($"Metadata changed for {Meta}:");
        TagsDiff.Print();
        TagsDiff.Clear();

        return true;
    }

    public static void Delete(MusicFile file)
    {
        file.Dispose();
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

    public static void Move(string source, string destination, bool @overwrite)
    {
        Log.MinorAction($"Moving file from {source} to {destination}");
        File.Move(source, destination, overwrite);
        string lyricsSource = System.IO.Path.ChangeExtension(source, ".lrc");
        string lyricsDestination = System.IO.Path.ChangeExtension(destination, ".lrc");
        if (File.Exists(lyricsSource))
        {
            Log.MinorAction($"Moving file from {lyricsSource} to {lyricsDestination}");
            File.Move(lyricsSource, lyricsDestination, overwrite);
        }
    }

    public override string ToString() => $"[{Playlist.Title}] {System.IO.Path.GetFileNameWithoutExtension(Path)}";

    public void Dispose()
    {
        TagsFile?.Dispose();
        TagsFile = null;
        TagsDiff = null;
    }
}
