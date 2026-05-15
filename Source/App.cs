using System.Threading.Channels;
using Hqub.MusicBrainz;
using Logger;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Common;
using System.Collections.Immutable;
using System.Text;

namespace YtPlaylist;

sealed class App
{
    public required AppArguments Arguments { get; init; }

    const int MaxRetries = 1;
    const int MaxConcurrency = 1;
    static readonly TimeSpan CacheTime = TimeSpan.FromDays(500);
    public const string UserAgent = "github.com/BBpezsgo";

    public async Task Run(CancellationToken cancellationToken = default)
    {
        TagLib.Id3v2.Tag.DefaultVersion = 3;
        TagLib.Id3v2.Tag.ForceDefaultVersion = true;

        List<Playlist> playlists = [];
        Dictionary<string, List<MusicFile>> playlistFiles = [];

        List<string> unexpectedMusicFiles = [];
        List<PlaylistVideo> online = [];

        YouTubeCache? youTubeCache = new(Arguments.HttpCachePath);
        youTubeCache = null;

        Log.Section($"Synchronizing playlists");

        using (YoutubeClient youtube = new())
        {
            Log.MajorAction($"Fetching playlists");

            using (ProgressBar progressBar = new() { MaxWidth = 40 })
            {
                foreach (string playlistId in Arguments.PlaylistIds.Distinct().ToArray().WithProgress(progressBar))
                {
                    if (youTubeCache is null || !youTubeCache.LoadPlaylist(playlistId, out Playlist? playlist))
                    {
                        try
                        {
                            playlist = await youtube.Playlists.GetAsync($"https://youtube.com/playlist?list={playlistId}", cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            Log.Error(ex);
                            continue;
                        }
                        youTubeCache?.SavePlaylist(playlist);
                    }

                    playlists.Add(playlist);
                }
            }

            Log.MajorAction($"Indexing");

            using (ProgressBar progressBar = new() { MaxWidth = 40 })
            {
                foreach (Playlist playlist in playlists.WithProgress(progressBar))
                {
                    string outputPath = Path.Combine(Arguments.OutputPath, playlist.Title);
                    List<MusicFile> localFiles = [];

                    if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

                    if (!Arguments.UseCache)
                    {
                        localFiles.Clear();
                        IndexFiles(localFiles, unexpectedMusicFiles, outputPath, playlist, cancellationToken);
                        //if (!Arguments.DryRun) WriteIndex(localFiles, outputPath);
                    }
                    else if (!File.Exists(Path.Combine(outputPath, ".cache")))
                    {
                        localFiles.Clear();
                        IndexFiles(localFiles, unexpectedMusicFiles, outputPath, playlist, cancellationToken);
                        if (!Arguments.DryRun) WriteIndex(localFiles, outputPath);
                    }
                    else
                    {
                        ReadIndex(localFiles, outputPath, playlist);
                    }

                    playlistFiles.Add(playlist.Id.Value, localFiles);

                    if (cancellationToken.IsCancellationRequested) return;
                }
            }

            Log.MajorAction($"Downloading music videos");

            using (ProgressBar progressBar = new() { MaxWidth = 40 })
            {
                foreach (Playlist playlist in playlists.WithProgress(progressBar))
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    string outputPath = Path.Combine(Arguments.OutputPath, playlist.Title);
                    List<MusicFile> localFiles = playlistFiles[playlist.Id.Value];

                    await DownloadPlaylist(playlist, youtube, youTubeCache, online, outputPath, localFiles, cancellationToken);
                }
            }
        }

        if (!Arguments.DryRun && Arguments.UseCache)
        {
            using (ProgressBar progressBar = new() { MaxWidth = 40 })
            {
                foreach (Playlist playlist in playlists.WithProgress(progressBar))
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    string outputPath = Path.Combine(Arguments.OutputPath, playlist.Title);
                    List<MusicFile> localFiles = playlistFiles[playlist.Id.Value];

                    WriteIndex(localFiles, outputPath);
                }
            }
        }

        {
            Dictionary<string, List<PlaylistVideo>> duplicates = [];
            for (int i = 0; i < online.Count; i++)
            {
                for (int j = i + 1; j < online.Count; j++)
                {
                    if (online[i].Id.Value == online[j].Id.Value)
                    {
                        if (!duplicates.TryGetValue(online[i].Id.Value, out List<PlaylistVideo>? dups))
                        {
                            dups = duplicates[online[i].Id.Value] = [online[i]];
                        }
                        dups.Add(online[j]);
                    }
                }
            }

            if (duplicates.Count > 0)
            {
                Log.Warning($"Duplicated music items found:");
                foreach (List<PlaylistVideo> items in duplicates.Values)
                {
                    Console.Write("    ");
                    Console.Write('[');
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (i > 0) Console.Write(", ");
                        Playlist? playlist = playlists.FirstOrDefault(v => v.Id.Value == items[i].PlaylistId.Value);
                        Console.Write(playlist?.Title ?? "?");
                    }
                    Console.Write(']');
                    Console.Write($" {items[0].Author} - {items[0].Title}");
                    Console.WriteLine();
                }
            }
        }

        if (unexpectedMusicFiles.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Unexpected music files:");
            Console.WriteLine();
            foreach (string file in unexpectedMusicFiles)
            {
                Console.Write("    ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("-");
                Console.ResetColor();
                Console.Write($" {Path.GetFileNameWithoutExtension(file)}");
                Console.WriteLine();
            }

            if (!Arguments.DryRun && Log.AskYesNo("Do you want to delete the files above?", true))
            {
                foreach (string file in unexpectedMusicFiles)
                {
                    File.Delete(file);
                    string lyricsFilename = Path.ChangeExtension(file, ".lrc");
                    if (File.Exists(lyricsFilename)) File.Delete(lyricsFilename);
                }
            }
        }

        List<MusicFile> deleteFiles = [.. playlistFiles.Values.SelectMany(v => v).Where(item => item.Video is null)];

        if (deleteFiles.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Deleted music files:");
            Console.WriteLine();
            foreach (MusicFile file in deleteFiles)
            {
                Console.Write("    ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("-");
                Console.ResetColor();
                Console.Write($" [{file.Playlist.Title}] {Path.GetFileNameWithoutExtension(file.Path)}");
                Console.WriteLine();
            }

            if (!Arguments.DryRun && Log.AskYesNo("Do you want to delete the files above?", true))
            {
                HashSet<string> modifiedPlaylists = [];
                Log.MinorAction("Deleting music files");
                using (ProgressBar progressBar = new() { MaxWidth = 40 })
                {
                    foreach (MusicFile file in deleteFiles.WithProgress(progressBar))
                    {
                        File.Delete(file.Path);
                        string lyricsFilename = Path.ChangeExtension(file.Path, ".lrc");
                        if (File.Exists(lyricsFilename)) File.Delete(lyricsFilename);

                        playlistFiles[file.Playlist.Id.Value].RemoveAll(v => v.Id == file.Id);
                        modifiedPlaylists.Add(file.Playlist.Id.Value);
                    }
                }

                Log.MinorAction("Writing index files");
                foreach (string playlistId in modifiedPlaylists)
                {
                    Playlist playlist = playlists.First(v => v.Id.Value == playlistId);
                    string outputPath = Path.Combine(Arguments.OutputPath, playlist.Title);
                    if (!Arguments.DryRun) WriteIndex(playlistFiles[playlistId], outputPath);
                }
            }
        }

        if (Arguments.Metadata)
        {
            Log.Section($"Fetching metadata");

            using MusicBrainzClient musicBrainz = new(new HttpClient(new SocketsHttpHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            })
            {
                DefaultRequestHeaders = { { "User-Agent", UserAgent } },
                BaseAddress = new Uri("https://musicbrainz.org/ws/2/"),
            })
            {
                Cache = new FileRequestCache(Arguments.HttpCachePath)
                {
                    Timeout = CacheTime,
                },
            };


            using (ProgressBar progressBar = new() { MaxWidth = 40 })
            {
                foreach (MusicFile musicFile in playlistFiles.Values.SelectMany(v => v).ToArray().WithProgress(progressBar))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    if (!File.Exists(musicFile.Path)) continue;

                    string outputPath = Path.Combine(Arguments.OutputPath, playlists.First(v => v.Id.Value == musicFile.Playlist.Id.Value).Title);

                    using TagLib.File file = TagLib.File.Create(musicFile.Path);

                    if (string.IsNullOrEmpty(file.Tag.MusicBrainzReleaseId))
                    {
                        string name = Path.GetFileNameWithoutExtension(musicFile.Path);

                        List<MetaGuesser.Warning> warnings = [];
                        MetaGuesser.Meta guessedMeta = MetaGuesser.Guess(name, warnings);

                        if (warnings.Count > 0 && !Arguments.IgnoreMetaWarnings)
                        {
                            Log.Warning($"Failed to guess meta for \"{name}\"");
                            StringBuilder arrowsBuilder = new();
                            arrowsBuilder.Append(' ', 26);
                            int i = 0;
                            foreach (int j in warnings.Select(v => v.Index).Where(v => v != 0).Distinct().Order())
                            {
                                int diff = j - i;
                                arrowsBuilder.Append(' ', diff);
                                i = j;
                                arrowsBuilder.Append('^');
                            }
                            string arrows = arrowsBuilder.ToString().TrimEnd();
                            if (arrows.Length > 0) Log.WarningNoprefix(arrows);
                            foreach (MetaGuesser.Warning warning in warnings)
                            {
                                Log.WarningNoprefix(warning.ToString());
                            }
                        }

                        Diff tagDiff = new();

                        file.Tag.Performers = tagDiff.Modify("Performers", file.Tag.Performers, guessedMeta.RemixedBy is not null ? [.. guessedMeta.Artists, guessedMeta.RemixedBy] : [.. guessedMeta.Artists]);
                        file.Tag.Title = tagDiff.Modify("Title", file.Tag.Title, guessedMeta.Title);
                        file.Tag.RemixedBy = tagDiff.Modify("RemixedBy", file.Tag.RemixedBy, guessedMeta.RemixedBy);

                        await MusicBrainz.FetchMetadata(file, tagDiff, musicBrainz, Arguments, cancellationToken);

                        if (tagDiff.Changes.Count > 0)
                        {
                            Log.None($"Meta tags changed for \"{name}\":");
                            tagDiff.Print();

                            if (!Arguments.DryRun) file.Save();
                        }
                    }
                }
            }
        }

        if (Arguments.Lyrics)
        {
            Log.Section($"Fetching lyrics");

            using LrcLib lrcLib = new(new(Arguments.HttpCachePath)
            {
                Timeout = CacheTime,
            });

            using (ProgressBar progressBar = new() { MaxWidth = 40 })
            {
                foreach (MusicFile musicFile in playlistFiles.Values.SelectMany(v => v).ToArray().WithProgress(progressBar))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    if (!File.Exists(musicFile.Path)) continue;
                    if (File.Exists(Path.ChangeExtension(musicFile.Path, ".lrc"))) continue;

                    string outputPath = Path.Combine(Arguments.OutputPath, playlists.First(v => v.Id.Value == musicFile.Playlist.Id.Value).Title);

                    using TagLib.File file = TagLib.File.Create(musicFile.Path);

                    if (string.IsNullOrEmpty(file.Tag.Title)
                        || file.Tag.Performers is null
                        || file.Tag.Performers.Length == 0)
                    { continue; }

                    try
                    {
                        LrcLib.LyricsResponse? lyrics = await lrcLib.FetchLyrics(file.Tag.FirstPerformer, file.Tag.Title, null, null, cancellationToken);
                        if (lyrics is null) continue;
                        if (lyrics.SyncedLyrics is null && lyrics.PlainLyrics is null) continue;

                        TagLib.Id3v2.SynchedText[]? synchedTexts = null;
                        string? unsyncedText = null;

                        if (lyrics.SyncedLyrics is not null)
                        {
                            string[] lines = lyrics.SyncedLyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            synchedTexts = new TagLib.Id3v2.SynchedText[lines.Length];
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string line = lines[i];

                                if (!line.StartsWith('['))
                                {
                                    Log.Error($"Invalid lyrics");
                                    goto skipFile;
                                }

                                int j = line.IndexOf(']');

                                if (j == -1)
                                {
                                    Log.Error($"Invalid lyrics");
                                    goto skipFile;
                                }

                                string time = line[1..j];
                                line = line[(j + 1)..].TrimStart();

                                string[] timeSegments = time.Split(':');
                                if (timeSegments.Length != 2)
                                {
                                    Log.Error($"Invalid lyrics");
                                    goto skipFile;
                                }

                                if (!int.TryParse(timeSegments[0], out int minute))
                                {
                                    Log.Error($"Invalid lyrics");
                                    goto skipFile;
                                }

                                if (!double.TryParse(timeSegments[1], out double second))
                                {
                                    Log.Error($"Invalid lyrics");
                                    goto skipFile;
                                }

                                synchedTexts[i] = new TagLib.Id3v2.SynchedText(
                                    (long)TimeSpan.FromSeconds(second + (minute * 60d)).TotalMilliseconds,
                                    line
                                );
                            }
                        }

                        if (lyrics.PlainLyrics is not null)
                        {
                            unsyncedText = lyrics.PlainLyrics;
                        }

                        if (unsyncedText is null && synchedTexts is not null)
                        {
                            unsyncedText = string.Join('\n', synchedTexts.Select(v => v.Text));
                        }

                        TagLib.Id3v2.Tag tag = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2, true);

                        if (synchedTexts is not null)
                        {
                            TagLib.Id3v2.SynchronisedLyricsFrame synchronisedLyricsFrame = new("LRCLib", "eng", TagLib.Id3v2.SynchedTextType.Lyrics)
                            {
                                Text = synchedTexts,
                                Format = TagLib.Id3v2.TimestampFormat.AbsoluteMilliseconds,
                            };
                            tag.ReplaceFrame(TagLib.Id3v2.SynchronisedLyricsFrame.Get(tag, synchronisedLyricsFrame.Description, synchronisedLyricsFrame.Language, synchronisedLyricsFrame.Type, true), synchronisedLyricsFrame);
                        }

                        if (unsyncedText is not null)
                        {
                            TagLib.Id3v2.UnsynchronisedLyricsFrame unsynchronisedLyricsFrame = new("LRCLib", "eng")
                            {
                                Text = unsyncedText,
                            };
                            tag.ReplaceFrame(TagLib.Id3v2.UnsynchronisedLyricsFrame.Get(tag, unsynchronisedLyricsFrame.Description, unsynchronisedLyricsFrame.Language, true), unsynchronisedLyricsFrame);
                        }

                        File.WriteAllText(Path.ChangeExtension(musicFile.Path, ".lrc"), lyrics.SyncedLyrics ?? unsyncedText);

                        file.Save();
                        Log.None($"Lyrics added ({lyrics.ArtistName} - {lyrics.TrackName} [{lyrics.AlbumName}] {TimeSpan.FromSeconds(lyrics.Duration)})");
                    }
                    catch (Exception ex)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        Log.Error(ex);
                        continue;
                    }

                skipFile:;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("Done");
    }

    #region Index

    static void ReadIndex(List<MusicFile> result, string path, Playlist playlist)
    {
        //Log.MinorAction("Reading index from file");

        using FileStream file = new(Path.Combine(path, ".cache"), FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(file);

        int n = reader.ReadInt32();
        for (int i = 0; i < n; i++)
        {
            string videoId = reader.ReadString();
            string relativeFilename = reader.ReadString();
            result.Add(new MusicFile(Path.GetFullPath(relativeFilename, path), videoId, playlist));
        }
    }

    static void WriteIndex(List<MusicFile> localFiles, string path)
    {
        //Log.MinorAction("Writing index to file");

        using FileStream file = new(Path.Combine(path, ".cache"), FileMode.OpenOrCreate, FileAccess.Write);
        using BinaryWriter writer = new(file);

        writer.Write(localFiles.Count);
        foreach (MusicFile musicFile in localFiles)
        {
            writer.Write(musicFile.Id);
            writer.Write(Path.GetRelativePath(path, musicFile.Path));
        }
    }

    static void IndexFiles(List<MusicFile> localFiles, List<string> unexpectedMusicFiles, string path, Playlist playlist, CancellationToken cancellationToken = default)
    {
        //Log.MinorAction("Indexing files");

        if (!Directory.Exists(path)) return;

        foreach (string filename in Directory.GetFiles(path, "*.mp3"))
        {
            if (cancellationToken.IsCancellationRequested) return;

            TagLib.File file = TagLib.File.Create(filename, TagLib.ReadStyle.PictureLazy);

            if (!string.IsNullOrWhiteSpace(file.Tag.Description))
            {
                localFiles.Add(new MusicFile(filename, file.Tag.Description, playlist));
            }
            else
            {
                unexpectedMusicFiles.Add(filename);
            }
        }
    }

    #endregion

    #region YouTube

    async Task DownloadPlaylist(Playlist playlist, YoutubeClient youtube, YouTubeCache? youTubeCache, List<PlaylistVideo> online, string path, List<MusicFile> localFiles, CancellationToken cancellationToken = default)
    {
        if (youTubeCache is not null && youTubeCache.LoadPlaylistItems(playlist.Id.Value, out ImmutableArray<PlaylistVideo> items))
        {
            foreach (PlaylistVideo item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await HandleVideo(youtube, playlist, item, path, localFiles, cancellationToken);
                online.Add(item);
            }
        }
        else
        {
            Channel<PlaylistVideo> channel = Channel.CreateUnbounded<PlaylistVideo>();

            Span<Task> tasks = new Task[1 + MaxConcurrency];

            tasks[0] = Task.Run(async () =>
            {
                List<PlaylistVideo> videos = [];
                await foreach (Batch<PlaylistVideo> batch in youtube.Playlists.GetVideoBatchesAsync(playlist.Url, cancellationToken))
                {
                    foreach (PlaylistVideo video in batch.Items)
                    {
                        online.Add(video);
                        videos.Add(video);
                        await channel.Writer.WriteAsync(video, cancellationToken);
                    }
                }
                youTubeCache?.SavePlaylistItems(playlist.Id.Value, videos);
                channel.Writer.Complete();
                if (cancellationToken.IsCancellationRequested) return;
            }, cancellationToken);

            for (int i = 0; i < MaxConcurrency; i++)
            {
                tasks[i + 1] = DownloadPlaylistJob(youtube, playlist, channel, path, localFiles, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }
    }

    async Task DownloadPlaylistJob(YoutubeClient youtube, Playlist playlist, Channel<PlaylistVideo> channel, string path, List<MusicFile> localFiles, CancellationToken cancellationToken = default)
    {
        await foreach (PlaylistVideo video in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;

            await HandleVideo(youtube, playlist, video, path, localFiles, cancellationToken);
        }
    }

    async Task HandleVideo(YoutubeClient youtube, Playlist playlist, PlaylistVideo video, string path, List<MusicFile> localFiles, CancellationToken cancellationToken = default)
    {
        MusicFile? musicFile = localFiles.FirstOrDefault(v => v.Id == video.Id.Value);
        if (musicFile is not null)
        {
            musicFile.Video = video;
            //Log.Debug($"File \"{Path.GetFileName(filename)}\" already exists, skipping (indexed)");
            return;
        }

        string artist = video.Author.ChannelTitle;
        string title = video.Title;

        if (title.StartsWith($"{artist} - ", StringComparison.InvariantCultureIgnoreCase))
        {
            title = title[(artist.Length + 3)..].TrimStart();
        }

        artist = artist.TrimEnd(" - Topic").TrimEnd();
        string[] artists = artist.Split('&', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        string filename = Path.Combine(path, $"{SanitizeFilename(artist)} - {SanitizeFilename(title)}.mp3");

        if (Arguments.Download)
        {
            if (File.Exists(filename))
            {
                Log.Debug($"File \"{Path.GetFileName(filename)}\" already exists, skipping download");
            }
            else
            {
                if (Arguments.DryRun)
                {
                    Log.MinorAction($"Should download \e[1m{video.Title}\e[22m");
                }
                else
                {
                    Log.MinorAction($"Downloading \e[1m{video.Title}\e[22m");

                    Exception? downloadException = await RunRetries(
                        (cancellationToken) => Task.Run(() => YtDlp.DownloadAudioData(filename, $"https://www.youtube.com/watch?v={video.Id}"), cancellationToken),
                        GenericHttpRetryFilter,
                        MaxRetries,
                        cancellationToken
                    );
                    switch (downloadException)
                    {
                        case HttpRequestException v:
                            Log.Error($"Failed to download \e[1m{video.Title}\e[22m: HTTP {(int)v.StatusCode!} ({v.StatusCode})");
                            return;
                        case not null:
                            Log.Error(downloadException);
                            return;
                    }
                }
            }
        }
        else if (!File.Exists(filename))
        {
            return;
        }

        localFiles.Add(new MusicFile(filename, video.Id, playlist) { Video = video });

        if (!Arguments.DryRun)
        {
            using TagLib.File file = TagLib.File.Create(filename);
            Diff diff = new();

            file.Tag.Description = diff.Modify("Description", file.Tag.Description, video.Id.Value);

            if (Arguments.Metadata)
            {
                if (file.Tag.Pictures.Length == 0)
                {
                    await TagUtils.DownloadCoverImage(file, new Uri(video.Thumbnails.OrderByDescending(v => v.Resolution.Area).First().Url, UriKind.Absolute), "YouTube", TagLib.PictureType.FrontCover, diff, cancellationToken);
                }

                file.Tag.Title = diff.Modify("Title", file.Tag.Title, title);
                file.Tag.Performers = diff.Modify("Performers", file.Tag.Performers, artists);
            }

            if (diff.Changes.Count > 0)
            {
                file.Save();
                Log.Debug($"Metadata updated");
                diff.Print();
            }
        }
    }

    static async Task<bool> GenericHttpRetryFilter(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is HttpRequestException httpRequestException
            && httpRequestException.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            await Task.Delay(1000, cancellationToken);
            return true;
        }
        return false;
    }

    static async Task<Exception?> RunRetries(Func<CancellationToken, Task> callback, Func<Exception, CancellationToken, Task<bool>> exceptionHandler, int retries, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (int retry = 1; retry <= retries; retry++)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            try
            {
                await callback.Invoke(cancellationToken);
                return null;
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) return null;
                if (!await exceptionHandler.Invoke(ex, cancellationToken)) return ex;
                lastException = ex;
                continue;
            }
        }

        return cancellationToken.IsCancellationRequested ? null : lastException;
    }

    static string SanitizeFilename(string filename)
    {
        char[] result = filename.ToCharArray();
        for (int i = 0; i < result.Length; i++)
        {
            ref char c = ref result[i];
            c = c switch
            {
                '/' or '\\' or '"' or '\'' => '_',
                '\n' or '\r' or '\t' => ' ',
                _ => c,
            };
            if (!char.IsAscii(c)
                && char.GetUnicodeCategory(c)
                    is not System.Globalization.UnicodeCategory.LowercaseLetter
                    and not System.Globalization.UnicodeCategory.UppercaseLetter
                    and not System.Globalization.UnicodeCategory.TitlecaseLetter
                    and not System.Globalization.UnicodeCategory.ModifierLetter
                    and not System.Globalization.UnicodeCategory.OtherLetter
                    and not System.Globalization.UnicodeCategory.LetterNumber)
            {
                c = '?';
            }
        }
        return new string(result);
    }

    #endregion
}
