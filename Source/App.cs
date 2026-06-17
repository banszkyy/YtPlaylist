using System.Threading.Channels;
using Hqub.MusicBrainz;
using Logger;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Common;
using System.Collections.Immutable;
using System.Text;
using Google.Apis.YouTube.v3;
using System.Diagnostics;
using Quickenshtein;

namespace YtPlaylist;

sealed class App
{
    public required AppArguments Arguments { get; init; }

    const int MaxRetries = 1;
    const int MaxConcurrency = 1;
    static readonly TimeSpan CacheTime = TimeSpan.FromDays(500);
    public const string UserAgent = "github.com/banszkyy";
    ImmutableArray<KeyValuePair<string, string>> _confusables = [];

    public async Task Run(CancellationToken cancellationToken = default)
    {
        TagLib.Id3v2.Tag.DefaultVersion = 3;
        TagLib.Id3v2.Tag.ForceDefaultVersion = true;

        List<Playlist> playlists = [];
        Dictionary<string, List<MusicFile>> playlistFiles = [];

        List<string> unexpectedMusicFiles = [];
        List<PlaylistVideo> online = [];

        Dictionary<string, TagLib.Tag> tagCache = [];

        YouTubeCache? youTubeCache = new(Arguments.HttpCachePath);

        _confusables = await Confusables.Fetch(Arguments);

        Log.Section($"Synchronizing playlists");

        using (YoutubeClient youtube = new())
        {
            Log.MajorAction($"Fetching playlists");

            using (ProgressBar progressBar = new() { MaxWidth = 40 })
            {
                foreach (string playlistId in Arguments.PlaylistIds.Distinct().ToArray().WithProgress(progressBar))
                {
                    if (!Arguments.UseCache || youTubeCache is null || !youTubeCache.LoadPlaylist(playlistId, out Playlist? playlist))
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

                    if (Directory.Exists(outputPath))
                    {
                        foreach (string filename in Directory.GetFiles(outputPath, "*.mp3"))
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            TagLib.File file = TagLib.File.Create(filename, TagLib.ReadStyle.PictureLazy);
                            string? videoId = file.Tag.Description;

                            if (!string.IsNullOrWhiteSpace(videoId))
                            {
                                localFiles.Add(new MusicFile(filename, videoId, playlist));

                                if (Arguments.RecreateMetadata)
                                {
                                    Diff tagDiff = new();

                                    file.Tag.Album = tagDiff.Modify("Album", file.Tag.Album, default);
                                    file.Tag.AlbumArtists = tagDiff.Modify("AlbumArtists", file.Tag.AlbumArtists, []);
                                    file.Tag.BeatsPerMinute = tagDiff.Modify("BeatsPerMinute", file.Tag.BeatsPerMinute, default);
                                    file.Tag.Composers = tagDiff.Modify("Composers", file.Tag.Composers, []);
                                    file.Tag.Conductor = tagDiff.Modify("Conductor", file.Tag.Conductor, default);
                                    file.Tag.Copyright = tagDiff.Modify("Copyright", file.Tag.Copyright, default);
                                    file.Tag.Disc = tagDiff.Modify("Disc", file.Tag.Disc, default);
                                    file.Tag.DiscCount = tagDiff.Modify("DiscCount", file.Tag.DiscCount, default);
                                    file.Tag.Genres = tagDiff.Modify("Genres", file.Tag.Genres, []);
                                    file.Tag.Grouping = tagDiff.Modify("Grouping", file.Tag.Grouping, default);
                                    file.Tag.ISRC = tagDiff.Modify("ISRC", file.Tag.ISRC, default);
                                    file.Tag.MusicBrainzArtistId = tagDiff.Modify("MusicBrainzArtistId", file.Tag.MusicBrainzArtistId, default);
                                    file.Tag.MusicBrainzDiscId = tagDiff.Modify("MusicBrainzDiscId", file.Tag.MusicBrainzDiscId, default);
                                    file.Tag.MusicBrainzReleaseArtistId = tagDiff.Modify("MusicBrainzReleaseArtistId", file.Tag.MusicBrainzReleaseArtistId, default);
                                    file.Tag.MusicBrainzReleaseCountry = tagDiff.Modify("MusicBrainzReleaseCountry", file.Tag.MusicBrainzReleaseCountry, default);
                                    file.Tag.MusicBrainzReleaseGroupId = tagDiff.Modify("MusicBrainzReleaseGroupId", file.Tag.MusicBrainzReleaseGroupId, default);
                                    file.Tag.MusicBrainzReleaseId = tagDiff.Modify("MusicBrainzReleaseId", file.Tag.MusicBrainzReleaseId, default);
                                    file.Tag.MusicBrainzReleaseStatus = tagDiff.Modify("MusicBrainzReleaseStatus", file.Tag.MusicBrainzReleaseStatus, default);
                                    file.Tag.MusicBrainzReleaseType = tagDiff.Modify("MusicBrainzReleaseType", file.Tag.MusicBrainzReleaseType, default);
                                    file.Tag.MusicBrainzTrackId = tagDiff.Modify("MusicBrainzTrackId", file.Tag.MusicBrainzTrackId, default);
                                    file.Tag.Performers = tagDiff.Modify("Performers", file.Tag.Performers, []);
                                    file.Tag.Pictures = tagDiff.Modify("Pictures", file.Tag.Pictures, []);
                                    file.Tag.Publisher = tagDiff.Modify("Publisher", file.Tag.Publisher, default);
                                    file.Tag.RemixedBy = tagDiff.Modify("RemixedBy", file.Tag.RemixedBy, default);
                                    file.Tag.Title = tagDiff.Modify("Title", file.Tag.Title, default);
                                    file.Tag.TrackCount = tagDiff.Modify("TrackCount", file.Tag.TrackCount, default);
                                    file.Tag.Year = tagDiff.Modify("Year", file.Tag.Year, default);

                                    if (tagDiff.Changes.Count > 0)
                                    {
                                        Log.None($"Meta tags changed for \"{Path.GetFileName(filename)}\":");
                                        tagDiff.Print();

                                        if (!Arguments.DryRun) file.Save();
                                    }
                                }

                            }
                            else
                            {
                                unexpectedMusicFiles.Add(filename);
                            }
                        }
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

                    await DownloadPlaylist(playlist, youtube, youTubeCache, online, outputPath, playlistFiles[playlist.Id.Value], cancellationToken);
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

            if (!Arguments.DryRun && await Log.AskYesNoAsync("Do you want to delete the files above?", true, cancellationToken))
            {
                foreach (string file in unexpectedMusicFiles)
                {
                    MusicFile.Delete(file);
                }

                unexpectedMusicFiles.Clear();
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

                if (string.IsNullOrWhiteSpace(Arguments.YouTubeCredentialsPath))
                {
                    Log.Warning($"Cannot interact with the YouTube API: Credentials path not specified");
                }
                else if (await Log.AskYesNoAsync("Do you want to remove duplicated music videos?", false, cancellationToken))
                {
                    Log.MinorAction("Logging in");
                    YouTubeService yt = await YoutubeServiceFactory.CreateAsync(Arguments.YouTubeCredentialsPath, Path.Combine(Arguments.HttpCachePath, "token_cache"), cancellationToken);

                    foreach (List<PlaylistVideo> items in duplicates.Values)
                    {
                        ImmutableArray<Playlist> w = items.Select(w => playlists.FirstOrDefault(v => v.Id.Value == w.PlaylistId.Value)).Where(v => v is not null).ToImmutableArray()!;
                        if (w.IsEmpty) continue;

                        Log.None();
                        Log.None($"Video: {Ansi.Bold($"{items[0].Author.ChannelTitle} - {items[0].Title}")} ({items[0].Id}) (check https://www.youtube.com/watch?v={items[0].Id} )");

                        string? path = playlistFiles.Values.SelectMany(v => v).FirstOrDefault(v => v.Id == items[0].Id)?.Path;
                        if (path is null)
                        {
                            Log.Warning($"Cannot preview music: Music file not found");
                        }
                        else if (await Log.AskYesNoAsync($"Preview music?", false, cancellationToken))
                        {
                            try
                            {
                                Process? process = Process.Start(new ProcessStartInfo()
                                {
                                    FileName = "ffplay",
                                    Arguments = $"\"{path}\"",
                                    UseShellExecute = true,
                                });
                                if (process is null)
                                {
                                    Log.Error($"Failed to start ffplay");
                                }
                                else
                                {
                                    await process.WaitForExitAsync(cancellationToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                        }
                        Log.None();

                        for (int i = 0; i < w.Length; i++)
                        {
                            Console.Write("    ");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(i);
                            Console.ResetColor();
                            Console.Write(" - ");
                            Console.Write(w[i].Title);
                            Console.WriteLine();
                        }

                        int index = await Log.AskInputAsync($"Where do you want to keep it? ({0} - {w.Length - 1})", bool (string input, out int result) =>
                        {
                            if (!int.TryParse(input, out result))
                            {
                                Log.Error($"Invalid input");
                                return false;
                            }

                            if (result < 0 || result >= w.Length)
                            {
                                Log.Error($"Input is not in the range [{0}, {w.Length - 1}]");
                                return false;
                            }

                            return true;
                        }, cancellationToken);

                        for (int i = 0; i < w.Length; i++)
                        {
                            if (i == index) continue;

                            try
                            {
                                Playlist playlist = w[i];
                                PlaylistVideo video = items[0];

                                PlaylistItemsResource.ListRequest listRequest = yt.PlaylistItems.List("id,snippet");
                                listRequest.PlaylistId = playlist.Id;
                                listRequest.VideoId = video.Id;
                                listRequest.MaxResults = 1;

                                Log.Debug($"Searching for item id in {video.Title} ({playlist.Id})");
                                Google.Apis.YouTube.v3.Data.PlaylistItemListResponse listResponse = await listRequest.ExecuteAsync(cancellationToken);
                                Google.Apis.YouTube.v3.Data.PlaylistItem? item = listResponse.Items?.FirstOrDefault();

                                if (item == null)
                                {
                                    Log.Error($"Video {items[0].Author.ChannelTitle} - {items[i].Title} ({items[i].Id}) not found in playlist {playlist.Title} ({playlist.Id}).");
                                    continue;
                                }

                                Log.Debug($"Deleting item from {video.Title} ({playlist.Id})");
                                PlaylistItemsResource.DeleteRequest deleteRequest = yt.PlaylistItems.Delete(item.Id);
                                await deleteRequest.ExecuteAsync(cancellationToken);

                                foreach (MusicFile file in playlistFiles[playlist.Id].Where(v => v.Id == video.Id))
                                {
                                    MusicFile.Delete(file);
                                }
                                playlistFiles[playlist.Id].RemoveAll(v => v.Id == video.Id);
                                online.Remove(video);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                        }
                    }
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

            if (!Arguments.DryRun && await Log.AskYesNoAsync("Do you want to delete the files above?", true, cancellationToken))
            {
                Log.MinorAction("Deleting music files");
                using (ProgressBar progressBar = new() { MaxWidth = 40 })
                {
                    foreach (MusicFile file in deleteFiles.WithProgress(progressBar))
                    {
                        MusicFile.Delete(file);

                        playlistFiles[file.Playlist.Id.Value].RemoveAll(v => v.Id == file.Id);
                        if (file.Video is not null) online.Remove(file.Video);
                    }
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

                    string name = Path.GetFileNameWithoutExtension(musicFile.Path);
                    string? originalFilename = musicFile.Video is not null ? GetFileNameWithoutExtension(musicFile.Video) : null;

                    //if (musicFile.Video is not null
                    //    && originalFilename is not null
                    //    && !originalFilename.Equals(name, StringComparison.Ordinal))
                    //{
                    //    string originalPath = Path.Combine(Path.GetDirectoryName(musicFile.Path)!, originalFilename + ".mp3");
                    //    if (!File.Exists(originalPath))
                    //    {
                    //        if (Arguments.DryRun)
                    //        {
                    //            GetFileNameWithoutExtension(musicFile.Video);
                    //            Log.MinorAction($"Would rename \"{name}\" to \"{originalFilename}\"");
                    //            name = originalFilename;
                    //        }
                    //        else
                    //        {
                    //            Log.MinorAction($"Renamed \"{name}\" to \"{originalFilename}\"");
                    //            File.Move(musicFile.Path, originalPath);
                    //            name = originalFilename;
                    //            musicFile.Path = originalPath;
                    //        }
                    //    }
                    //}

                    using TagLib.File file = TagLib.File.Create(musicFile.Path);
                    tagCache[musicFile.Path] = file.Tag;

                    if (string.IsNullOrEmpty(file.Tag.MusicBrainzReleaseId))
                    {
                        List<MetaGuesser.Warning> warnings = [];
                        MetaGuesser.Meta guessedMeta;

                        if (musicFile.Video is not null)
                        {
                            guessedMeta = MetaGuesser.Guess(musicFile.Video, warnings);
                        }
                        else
                        {
                            guessedMeta = MetaGuesser.Guess(name, warnings);
                        }

                        if (warnings.Count > 0 && !Arguments.IgnoreMetaWarnings)
                        {
                            Log.Warning($"Meta issues for \"{Path.GetFileNameWithoutExtension(musicFile.Path)}\":");
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

                        //file.Tag.Performers = tagDiff.Modify("Performers", file.Tag.Performers, guessedMeta.RemixedBy is not null ? [.. guessedMeta.Artists, guessedMeta.RemixedBy] : [.. guessedMeta.Artists]);
                        //file.Tag.Title = tagDiff.Modify("Title", file.Tag.Title, guessedMeta.GetTitleText());
                        //file.Tag.RemixedBy = tagDiff.Modify("RemixedBy", file.Tag.RemixedBy, guessedMeta.RemixedBy);

                        List<string>? issues = Arguments.IgnoreMetaWarnings ? null : [];

                        await MusicBrainz.FetchMetadata(file, guessedMeta, tagDiff, musicBrainz, Arguments, issues, cancellationToken);

                        if (issues is not null && issues.Count > 0)
                        {
                            Log.Warning($"MusicBrainz issues for {guessedMeta}:");
                            foreach (string issue in issues)
                            {
                                Log.WarningNoprefix(issue);
                            }
                        }

                        if (tagDiff.Changes.Count > 0)
                        {
                            Log.None($"Meta tags changed for {guessedMeta}:");
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

                    string lyricsPath = Path.ChangeExtension(musicFile.Path, ".lrc");

                    if (File.Exists(lyricsPath)) continue;

                    using TagLib.File file = TagLib.File.Create(musicFile.Path);
                    tagCache[musicFile.Path] = file.Tag;

                    string outputPath = Path.Combine(Arguments.OutputPath, playlists.First(v => v.Id.Value == musicFile.Playlist.Id.Value).Title);

                    if (string.IsNullOrEmpty(file.Tag.Title)
                        || file.Tag.Performers is null
                        || file.Tag.Performers.Length == 0)
                    { continue; }

                    try
                    {
                        LrcLib.LyricsResponse? lyrics = await lrcLib.FetchLyrics(file.Tag.FirstPerformer, file.Tag.Title, null, null, cancellationToken);
                        if (lyrics is null) continue;
                        if (lyrics.SyncedLyrics is null && lyrics.PlainLyrics is null) continue;

                        TimeSpan lyricsDuration = TimeSpan.FromSeconds(lyrics.Duration);
                        TimeSpan? videoDuration = musicFile.Video?.Duration;

                        if (videoDuration.HasValue)
                        {
                            if (Math.Abs(lyricsDuration.TotalMilliseconds - videoDuration.Value.TotalMilliseconds) > 1000)
                            {
                                Log.Warning($"Duration mismatches: Video is {videoDuration.Value} Lyrics is {lyricsDuration}");
                            }
                        }

                        TagLib.Id3v2.SynchedText[]? synchedTexts = null;
                        string? unsyncedText = null;

                        if (lyrics.SyncedLyrics is not null)
                        {
                            synchedTexts = Lyrics.Parse(lyrics.SyncedLyrics);
                            if (synchedTexts is null) continue;
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

                        File.WriteAllText(lyricsPath, lyrics.SyncedLyrics ?? unsyncedText);

                        file.Save();
                        Log.None($"Lyrics added ({lyrics.ArtistName} - {lyrics.TrackName} [{lyrics.AlbumName}] {lyricsDuration})");
                    }
                    catch (Exception ex)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        Log.Error(ex);
                        continue;
                    }
                }
            }
        }

        if (false)
        {
            Log.Section("Checking redundancy");

            static int GetSimilarityDistance(string a, string b)
            {
                if (string.Equals(a, b)) return 0;
                if (string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase)) return 1;
                return 2 + Levenshtein.GetDistance(a.ToLowerInvariant(), b.ToLowerInvariant());
            }

            static ImmutableArray<ImmutableArray<string>> GetSimilarStrings(IEnumerable<string> strings, int distance)
            {
                HashSet<string> singlePerformers = [];
                List<HashSet<string>> similarPerformers = [];

                foreach (string performer in strings)
                {
                    int closestPerformerD = int.MaxValue;
                    string? closestPerformer = null;

                    foreach (string singlePerformer in singlePerformers)
                    {
                        int d = GetSimilarityDistance(performer, singlePerformer);
                        if (d <= closestPerformerD && d > 0)
                        {
                            closestPerformer = singlePerformer;
                            closestPerformerD = d;
                        }
                    }

                    if (closestPerformer is not null && closestPerformerD < distance)
                    {
                        singlePerformers.Remove(closestPerformer);

                        int bestGroupI = -1;
                        int bestGroupD = int.MaxValue;
                        for (int i = 0; i < similarPerformers.Count; i++)
                        {
                            int closest = int.MaxValue;
                            foreach (string v in similarPerformers[i])
                            {
                                closest = Math.Min(closest, GetSimilarityDistance(performer, v));
                                closest = Math.Min(closest, GetSimilarityDistance(closestPerformer, v));
                            }

                            if (closest < bestGroupD)
                            {
                                bestGroupI = i;
                                bestGroupD = closest;
                            }
                        }

                        if (bestGroupD < distance)
                        {
                            similarPerformers[bestGroupI].Add(performer);
                            similarPerformers[bestGroupI].Add(closestPerformer);
                        }
                        else
                        {
                            similarPerformers.Add([performer, closestPerformer]);
                        }
                    }
                    else
                    {
                        int bestGroupI = -1;
                        int bestGroupD = int.MaxValue;
                        for (int i = 0; i < similarPerformers.Count; i++)
                        {
                            int closest = int.MaxValue;
                            foreach (string v in similarPerformers[i])
                            {
                                closest = Math.Min(closest, GetSimilarityDistance(performer, v));
                            }

                            if (closest < bestGroupD)
                            {
                                bestGroupI = i;
                                bestGroupD = closest;
                            }
                        }

                        if (bestGroupD < distance)
                        {
                            similarPerformers[bestGroupI].Add(performer);
                        }
                        else
                        {
                            singlePerformers.Add(performer);
                        }
                    }
                }

                return [.. similarPerformers.Select(v => v.ToImmutableArray())];
            }

            ImmutableArray<ImmutableArray<string>> similarPerformers = GetSimilarStrings(playlistFiles.SelectMany(v => v.Value).Select(v => tagCache.TryGetValue(v.Path, out TagLib.Tag? tag) ? tag : null).SelectMany(v => v?.Performers ?? []), 4);

            if (similarPerformers.Length > 0)
            {
                Log.Warning($"Similar artists found:");
                foreach (ImmutableArray<string> v in similarPerformers)
                {
                    foreach (string w in v)
                    {
                        Log.WarningNoprefix(w);
                    }
                    Log.None();
                }
            }
        }

        {
            Log.Section($"Checking duplicates");

            List<ImmutableArray<MusicFile>> duplicates = [];
            ImmutableArray<MusicFile> all = [.. playlistFiles.Values.SelectMany(v => v)];

            using (ProgressBar progress = new() { MaxWidth = 40 })
            {
                for (int i = 0; i < all.Length; i++)
                {
                    progress.Report(i, all.Length);

                    MusicFile a = all[i];
                    if (duplicates.SelectMany(v => v).Any(v => v == a)) continue;
                    List<MusicFile> dups = [];

                    for (int j = i + 1; j < all.Length; j++)
                    {
                        MusicFile b = all[j];

                        if (a.Video is not null && b.Video is not null
                            && MetaGuesser.Guess(a.Video) == MetaGuesser.Guess(b.Video))
                        {
                            goto dupFound;
                        }
                        else if (tagCache.TryGetValue(a.Path, out TagLib.Tag? aTag) && tagCache.TryGetValue(b.Path, out TagLib.Tag? bTag)
                            && aTag.Performers.SequenceEqual(bTag.Performers)
                            && aTag.Title == bTag.Title
                            && aTag.RemixedBy == bTag.RemixedBy)
                        {
                            goto dupFound;
                        }
                        else if (MetaGuesser.Guess(Path.GetFileNameWithoutExtension(a.Path)) == MetaGuesser.Guess(Path.GetFileNameWithoutExtension(b.Path)))
                        {
                            goto dupFound;
                        }

                        continue;
                    dupFound:

                        dups.Add(b);
                    }

                    if (dups.Count > 0)
                    {
                        dups.Insert(0, all[i]);
                        duplicates.Add([.. dups]);
                    }
                }
            }

            if (duplicates.Count > 0)
            {
                Log.Warning($"Possible duplicated music items found:");
                foreach (ImmutableArray<MusicFile> items in duplicates)
                {
                    foreach (MusicFile item in items)
                    {
                        Console.Write("    ");
                        Console.Write('[');
                        Console.Write(item.Playlist.Title);
                        Console.Write(']');
                        if (item.Video is not null)
                        {
                            Console.Write($" {item.Video.Author.ChannelTitle} - {item.Video.Title}");
                        }
                        else
                        {
                            Console.Write($" {Path.GetFileNameWithoutExtension(item.Path)}");
                        }
                        Console.WriteLine();
                    }
                }

                if (string.IsNullOrWhiteSpace(Arguments.YouTubeCredentialsPath))
                {
                    Log.Warning($"Cannot interact with the YouTube API: Credentials path not specified");
                }
                else if (await Log.AskYesNoAsync("Do you want to remove duplicated music videos?", false, cancellationToken))
                {
                    Log.MinorAction("Logging in");
                    YouTubeService yt = await YoutubeServiceFactory.CreateAsync(Arguments.YouTubeCredentialsPath, Path.Combine(Arguments.HttpCachePath, "token_cache"), cancellationToken);

                    foreach (ImmutableArray<MusicFile> _items in duplicates)
                    {
                        ImmutableArray<MusicFile> items = [.. _items.Where(v => v.Video is not null)];
                        if (items.IsEmpty)
                        {
                            Log.Warning($"Something went wrong meow");
                            continue;
                        }

                        for (int i = 0; i < items.Length; i++)
                        {
                            MusicFile item = items[i];
                            Console.Write("    ");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(i + 1);
                            Console.ResetColor();
                            Console.Write(" - ");
                            Console.Write($"[{item.Playlist.Title}] {item.Video!.Author} - {item.Video.Title} (check https://www.youtube.com/watch?v={item.Video.Id} )");
                            Console.WriteLine();
                        }

                        int index = await Log.AskInputAsync($"Which one do you want to keep? ({1} - {items.Length} or 0 to skip)", bool (string input, out int result) =>
                        {
                            if (!int.TryParse(input, out result))
                            {
                                Log.Error($"Invalid input");
                                return false;
                            }

                            if (result < 0 || result > items.Length)
                            {
                                Log.Error($"Input is not in the range [{0}, {items.Length}]");
                                return false;
                            }

                            return true;
                        }, cancellationToken);

                        if (index == 0) continue;
                        index--;

                        for (int i = 0; i < items.Length; i++)
                        {
                            if (i == index) continue;
                            PlaylistVideo video = items[i].Video!;

                            try
                            {
                                PlaylistItemsResource.ListRequest listRequest = yt.PlaylistItems.List("id,snippet");
                                listRequest.PlaylistId = video.PlaylistId;
                                listRequest.VideoId = video.Id;
                                listRequest.MaxResults = 1;

                                Log.Debug($"Searching for item id in {video.Title} ({video.Id})");
                                Google.Apis.YouTube.v3.Data.PlaylistItemListResponse listResponse = await listRequest.ExecuteAsync(cancellationToken);
                                Google.Apis.YouTube.v3.Data.PlaylistItem? item = listResponse.Items?.FirstOrDefault();

                                if (item == null)
                                {
                                    Log.Error($"Video {video.Author.ChannelTitle} - {video.Title} ({video.Id}) not found in playlist {video.Title} ({video.Id}).");
                                    continue;
                                }

                                Log.Debug($"Deleting item from {video.Title} ({video.Id})");
                                PlaylistItemsResource.DeleteRequest deleteRequest = yt.PlaylistItems.Delete(item.Id);
                                await deleteRequest.ExecuteAsync(cancellationToken);

                                foreach (MusicFile file in playlistFiles[video.PlaylistId].Where(v => v.Id == video.Id))
                                {
                                    MusicFile.Delete(file);
                                }
                                playlistFiles[video.PlaylistId].RemoveAll(v => v.Id == video.Id);
                                online.Remove(video);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("Done");
    }

    #region YouTube

    async Task DownloadPlaylist(Playlist playlist, YoutubeClient youtube, YouTubeCache? youTubeCache, List<PlaylistVideo> online, string path, List<MusicFile> localFiles, CancellationToken cancellationToken = default)
    {
        Channel<PlaylistVideo> channel = Channel.CreateUnbounded<PlaylistVideo>();

        Span<Task> tasks = new Task[1 + MaxConcurrency];

        if (Arguments.UseCache && youTubeCache is not null && youTubeCache.LoadPlaylistItems(playlist.Id.Value, out ImmutableArray<PlaylistVideo> items))
        {
            tasks[0] = Task.Run(async () =>
            {
                foreach (PlaylistVideo video in items)
                {
                    online.Add(video);
                    await channel.Writer.WriteAsync(video, cancellationToken);
                }
                channel.Writer.Complete();
                if (cancellationToken.IsCancellationRequested) return;
            }, cancellationToken);
        }
        else
        {
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
        }

        for (int i = 0; i < MaxConcurrency; i++)
        {
            tasks[i + 1] = DownloadPlaylistJob(youtube, playlist, channel, path, localFiles, cancellationToken);
        }

        await Task.WhenAll(tasks);
    }

    async Task DownloadPlaylistJob(YoutubeClient youtube, Playlist playlist, Channel<PlaylistVideo> channel, string path, List<MusicFile> localFiles, CancellationToken cancellationToken = default)
    {
        await foreach (PlaylistVideo video in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;

            await HandleVideo(youtube, playlist, video, path, localFiles, cancellationToken);
        }
    }

    string GetFileNameWithoutExtension(PlaylistVideo video)
    {
        MetaGuesser.Meta meta = MetaGuesser.Guess(video, []);
        return SanitizeFilename($"{meta.GetArtistsText()} - {meta.GetTitleText()}");
    }

    async Task HandleVideo(YoutubeClient youtube, Playlist playlist, PlaylistVideo video, string path, List<MusicFile> localFiles, CancellationToken cancellationToken = default)
    {
        MusicFile? musicFile = localFiles.FirstOrDefault(v => v.Id == video.Id.Value);
        if (musicFile is not null)
        {
            musicFile.Video = video;
            goto meta;
        }

        string filename = Path.Combine(path, $"{GetFileNameWithoutExtension(video)}.mp3");

        if (File.Exists(filename))
        {
            localFiles.Add(musicFile = new MusicFile(filename, video.Id, playlist) { Video = video });
        }

        if (Arguments.Download)
        {
            if (musicFile is not null)
            {
                Log.Debug($"File \"{Path.GetFileName(filename)}\" already exists, skipping entirely");
                return;
            }
            else
            {
                if (Arguments.DryRun)
                {
                    Log.MinorAction($"Should download {Ansi.Bold(video.Author.ChannelTitle)} - {Ansi.Bold(video.Title)}");
                }
                else
                {
                    Log.MinorAction($"Downloading {Ansi.Bold(video.Author.ChannelTitle)} - {Ansi.Bold(video.Title)}");

                    Exception? downloadException = await RunRetries(
                        (cancellationToken) => Task.Run(() => YtDlp.DownloadAudioData(filename, $"https://www.youtube.com/watch?v={video.Id}"), cancellationToken),
                        GenericHttpRetryFilter,
                        MaxRetries,
                        cancellationToken
                    );
                    switch (downloadException)
                    {
                        case HttpRequestException v:
                            Log.Error($"Failed to download {Ansi.Bold(video.Author.ChannelTitle)} - {Ansi.Bold(video.Title)}: HTTP {(int)v.StatusCode!} ({v.StatusCode})");
                            return;
                        case not null:
                            Log.Error(downloadException);
                            return;
                    }

                    localFiles.Add(musicFile = new MusicFile(filename, video.Id, playlist) { Video = video });
                }
            }
        }
        else if (musicFile is null)
        {
            return;
        }

    meta:

        if (!Arguments.DryRun && musicFile is not null)
        {
            using TagLib.File file = TagLib.File.Create(musicFile.Path);
            Diff diff = new();

            file.Tag.Description = diff.Modify("Description", file.Tag.Description, video.Id.Value);

            if (Arguments.Metadata)
            {
                if (file.Tag.Pictures.Length == 0)
                {
                    await TagUtils.DownloadCoverImage(file, new Uri(video.Thumbnails.OrderByDescending(v => v.Resolution.Area).First().Url, UriKind.Absolute), "YouTube", TagLib.PictureType.FrontCover, diff, cancellationToken);
                }

                MetaGuesser.Meta meta = MetaGuesser.Guess(video);

                if (string.IsNullOrEmpty(file.Tag.Title)) file.Tag.Title = diff.Modify("Title", file.Tag.Title, meta.GetTitleText());
                if (file.Tag.Performers.IsNullOrEmpty()) file.Tag.Performers = diff.Modify("Performers", file.Tag.Performers, [.. meta.Artists]);
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

    string SanitizeFilename(string filename)
    {
        static bool IsOk(char c)
        {
            return char.IsAscii(c)
                || char.GetUnicodeCategory(c)
                is System.Globalization.UnicodeCategory.LowercaseLetter
                or System.Globalization.UnicodeCategory.UppercaseLetter
                or System.Globalization.UnicodeCategory.TitlecaseLetter
                or System.Globalization.UnicodeCategory.ModifierLetter
                or System.Globalization.UnicodeCategory.OtherLetter
                or System.Globalization.UnicodeCategory.LetterNumber;
        }

        foreach ((string a, string b) in _confusables)
        {
            if (a.All(IsOk)) continue;
            filename = filename.Replace(a, b);
        }

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

            if (!IsOk(c))
            {
                c = '?';
            }
        }
        return new string(result);
    }

    #endregion
}
