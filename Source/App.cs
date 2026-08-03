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
using System.Net;
using HttpCache;
using YtPlaylist.SoundCloud;
using System.Text.Json;

namespace YtPlaylist;

sealed class App
{
    public required AppArguments Arguments { get; init; }

    const int MaxRetries = 1;
    const int MaxConcurrency = 1;
    static readonly TimeSpan CacheTime = TimeSpan.FromDays(500);
    static ImmutableDictionary<int, string?> _confusables = [];
    public const string UserAgent = "github.com/banszkyy";
    readonly List<Change<MusicFile>> Changes = [];

    public async Task Run(CancellationToken cancellationToken = default)
    {
        TagLib.Id3v2.Tag.DefaultVersion = 3;
        TagLib.Id3v2.Tag.ForceDefaultVersion = true;

        using Library library = new();

        List<YoutubeExplode.Playlists.Playlist> playlists = [];

        List<string> unexpectedMusicFiles = [];
        List<PlaylistVideo> online = [];

        ImmutableArray<NetscapeCookieFile.Cookie> cookies = [];

        if (!string.IsNullOrWhiteSpace(Arguments.CookiesPath))
        {
            if (!File.Exists(Arguments.CookiesPath))
            {
                Log.Error($"Specified cookies path doesn't exists {Arguments.CookiesPath}");
            }
            else
            {
                cookies = NetscapeCookieFile.Parse(File.ReadAllText(Arguments.CookiesPath));
            }
        }

        YouTubeCache? youTubeCache = new(Path.Combine(Arguments.HttpCachePath, "YouTube"));

        _confusables = await Confusables.Fetch(Arguments);

        Log.Section($"Synchronizing playlists");

        using (YoutubeClient youtube = new())
        {
            Log.MajorAction($"Fetching playlists metadata");

            using (ProgressBar progressBar = new() { MaxWidth = 70 })
            {
                foreach (string playlistId in Arguments.PlaylistIds.Distinct().ToArray().WithProgress(progressBar, v => v))
                {
                    if (!Arguments.UseCache || youTubeCache is null || !youTubeCache.LoadPlaylist(playlistId, out YoutubeExplode.Playlists.Playlist? playlist))
                    {
                        try
                        {
                            playlist = await youtube.Playlists.GetAsync($"https://youtube.com/playlist?list={playlistId}", cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            Log.Error($"Failed to fetch playlist {playlistId}");
                            Log.Error(ex);
                            continue;
                        }
                        youTubeCache?.SavePlaylist(playlist);
                    }

                    playlists.Add(playlist);
                }
            }

            Log.MajorAction($"Indexing local files");

            using (ProgressBar progressBar = new() { MaxWidth = 70 })
            {
                foreach (YoutubeExplode.Playlists.Playlist playlist in playlists.WithProgress(progressBar, v => v.Title))
                {
                    string outputPath = Path.Combine(Arguments.OutputPath, playlist.Title);

                    Playlist libraryPlaylist = new(playlist.Title, outputPath, playlist);
                    library.Playlists.Add(libraryPlaylist);

                    if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

                    if (Directory.Exists(outputPath))
                    {
                        foreach (string filename in Directory.GetFiles(outputPath, "*.mp3"))
                        {
                            if (cancellationToken.IsCancellationRequested) return;

                            MusicFile musicFile;

                            {
                                TagLib.File tagsFile = TagLib.File.Create(filename, TagLib.ReadStyle.PictureLazy);

                                musicFile = new MusicFile(filename, tagsFile.Tag.Description, new MusicMeta([], Path.GetFileNameWithoutExtension(filename)), libraryPlaylist)
                                {
                                    TagsFile = tagsFile,
                                    TagsDiff = new Diff(),
                                };
                            }

                            if (!string.IsNullOrWhiteSpace(musicFile.TagsFile.Tag.Description))
                            {
                                if (Arguments.RecreateMetadata)
                                {
                                    musicFile.TagsFile.Tag.Album = musicFile.TagsDiff.Modify("Album", musicFile.TagsFile.Tag.Album, default);
                                    musicFile.TagsFile.Tag.AlbumArtists = musicFile.TagsDiff.Modify("AlbumArtists", musicFile.TagsFile.Tag.AlbumArtists, []);
                                    musicFile.TagsFile.Tag.BeatsPerMinute = musicFile.TagsDiff.Modify("BeatsPerMinute", musicFile.TagsFile.Tag.BeatsPerMinute, default);
                                    musicFile.TagsFile.Tag.Composers = musicFile.TagsDiff.Modify("Composers", musicFile.TagsFile.Tag.Composers, []);
                                    musicFile.TagsFile.Tag.Conductor = musicFile.TagsDiff.Modify("Conductor", musicFile.TagsFile.Tag.Conductor, default);
                                    musicFile.TagsFile.Tag.Copyright = musicFile.TagsDiff.Modify("Copyright", musicFile.TagsFile.Tag.Copyright, default);
                                    musicFile.TagsFile.Tag.Disc = musicFile.TagsDiff.Modify("Disc", musicFile.TagsFile.Tag.Disc, default);
                                    musicFile.TagsFile.Tag.DiscCount = musicFile.TagsDiff.Modify("DiscCount", musicFile.TagsFile.Tag.DiscCount, default);
                                    musicFile.TagsFile.Tag.Genres = musicFile.TagsDiff.Modify("Genres", musicFile.TagsFile.Tag.Genres, []);
                                    musicFile.TagsFile.Tag.Grouping = musicFile.TagsDiff.Modify("Grouping", musicFile.TagsFile.Tag.Grouping, default);
                                    musicFile.TagsFile.Tag.ISRC = musicFile.TagsDiff.Modify("ISRC", musicFile.TagsFile.Tag.ISRC, default);
                                    musicFile.TagsFile.Tag.MusicBrainzArtistId = musicFile.TagsDiff.Modify("MusicBrainzArtistId", musicFile.TagsFile.Tag.MusicBrainzArtistId, default);
                                    musicFile.TagsFile.Tag.MusicBrainzDiscId = musicFile.TagsDiff.Modify("MusicBrainzDiscId", musicFile.TagsFile.Tag.MusicBrainzDiscId, default);
                                    musicFile.TagsFile.Tag.MusicBrainzReleaseArtistId = musicFile.TagsDiff.Modify("MusicBrainzReleaseArtistId", musicFile.TagsFile.Tag.MusicBrainzReleaseArtistId, default);
                                    musicFile.TagsFile.Tag.MusicBrainzReleaseCountry = musicFile.TagsDiff.Modify("MusicBrainzReleaseCountry", musicFile.TagsFile.Tag.MusicBrainzReleaseCountry, default);
                                    musicFile.TagsFile.Tag.MusicBrainzReleaseGroupId = musicFile.TagsDiff.Modify("MusicBrainzReleaseGroupId", musicFile.TagsFile.Tag.MusicBrainzReleaseGroupId, default);
                                    musicFile.TagsFile.Tag.MusicBrainzReleaseId = musicFile.TagsDiff.Modify("MusicBrainzReleaseId", musicFile.TagsFile.Tag.MusicBrainzReleaseId, default);
                                    musicFile.TagsFile.Tag.MusicBrainzReleaseStatus = musicFile.TagsDiff.Modify("MusicBrainzReleaseStatus", musicFile.TagsFile.Tag.MusicBrainzReleaseStatus, default);
                                    musicFile.TagsFile.Tag.MusicBrainzReleaseType = musicFile.TagsDiff.Modify("MusicBrainzReleaseType", musicFile.TagsFile.Tag.MusicBrainzReleaseType, default);
                                    musicFile.TagsFile.Tag.MusicBrainzTrackId = musicFile.TagsDiff.Modify("MusicBrainzTrackId", musicFile.TagsFile.Tag.MusicBrainzTrackId, default);
                                    musicFile.TagsFile.Tag.Performers = musicFile.TagsDiff.Modify("Performers", musicFile.TagsFile.Tag.Performers, []);
                                    musicFile.TagsFile.Tag.Pictures = musicFile.TagsDiff.Modify("Pictures", musicFile.TagsFile.Tag.Pictures, []);
                                    musicFile.TagsFile.Tag.Publisher = musicFile.TagsDiff.Modify("Publisher", musicFile.TagsFile.Tag.Publisher, default);
                                    musicFile.TagsFile.Tag.RemixedBy = musicFile.TagsDiff.Modify("RemixedBy", musicFile.TagsFile.Tag.RemixedBy, default);
                                    musicFile.TagsFile.Tag.Title = musicFile.TagsDiff.Modify("Title", musicFile.TagsFile.Tag.Title, default);
                                    musicFile.TagsFile.Tag.TrackCount = musicFile.TagsDiff.Modify("TrackCount", musicFile.TagsFile.Tag.TrackCount, default);
                                    musicFile.TagsFile.Tag.Year = musicFile.TagsDiff.Modify("Year", musicFile.TagsFile.Tag.Year, default);

                                    if (Arguments.SaveIntermediateTags && musicFile.SaveTags(Arguments.DryRun))
                                    {
                                        Changes.Add(new(musicFile, ChangeType.Modify));
                                    }
                                }

                                libraryPlaylist.Musics.Add(musicFile);
                            }
                            else
                            {
                                musicFile.Dispose();
                                unexpectedMusicFiles.Add(filename);
                            }
                        }
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                }
            }

            Log.MajorAction($"Fetching & downloading playlist contents");

            using (ProgressBar progressBar = new() { MaxWidth = 70 })
            {
                foreach (Playlist playlist in library.Playlists.WithProgress(progressBar, v => v.Title))
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    string outputPath = Path.Combine(Arguments.OutputPath, playlist.Title);

                    await DownloadPlaylist(youtube, playlist, youTubeCache, online, outputPath, cancellationToken);
                }
            }
        }

        for (int i = 0; i < unexpectedMusicFiles.Count; i++)
        {
            if (library.Musics.Any(v => v.Path == unexpectedMusicFiles[i]))
            {
                unexpectedMusicFiles.RemoveAt(i--);
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
                        YoutubeExplode.Playlists.Playlist? playlist = playlists.FirstOrDefault(v => v.Id.Value == items[i].PlaylistId.Value);
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
                else if (!File.Exists(Arguments.YouTubeCredentialsPath))
                {
                    Log.Warning($"Cannot interact with the YouTube API: Specified credentials file doesn't exists");
                }
                else if (await Log.AskYesNoAsync("Do you want to remove duplicated music videos?", false, cancellationToken))
                {
                    Log.MinorAction("Logging in");
                    YouTubeService yt = await YoutubeServiceFactory.CreateAsync(Arguments.YouTubeCredentialsPath, Path.Combine(Arguments.HttpCachePath, "token_cache"), cancellationToken);

                    foreach (List<PlaylistVideo> items in duplicates.Values)
                    {
                        ImmutableArray<YoutubeExplode.Playlists.Playlist> w = items.Select<PlaylistVideo, YoutubeExplode.Playlists.Playlist?>(w => playlists.FirstOrDefault(v => v.Id.Value == w.PlaylistId.Value)).Where<YoutubeExplode.Playlists.Playlist?>(v => v is not null).ToImmutableArray();
                        if (w.IsEmpty) continue;

                        Log.None();
                        Log.None($"Video: {Ansi.Bold($"{items[0].Author.ChannelTitle} - {items[0].Title}")} ({items[0].Id}) (check https://www.youtube.com/watch?v={items[0].Id} )");

                        string? path = library.Musics.FirstOrDefault(v => v.Id == items[0].Id)?.Path;
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
                                YoutubeExplode.Playlists.Playlist playlist = w[i];
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

                                Playlist libraryPlaylist = library.Playlists.First(v => v.YouTubePlaylist.Id.Value == playlist.Id.Value);

                                foreach (MusicFile file in libraryPlaylist.Musics.Where(v => v.Id == video.Id))
                                {
                                    MusicFile.Delete(file);
                                    Changes.Add(new(file, ChangeType.Delete));
                                }
                                libraryPlaylist.Musics.RemoveAll(v => v.Id == video.Id);
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

        List<MusicFile> deleteFiles = [.. library.Musics.Where(item => item.PlaylistVideo is null)];

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
                using (ProgressBar progressBar = new() { MaxWidth = 70 })
                {
                    foreach (MusicFile file in deleteFiles.WithProgress(progressBar, v => v.ToString()))
                    {
                        MusicFile.Delete(file);
                        Changes.Add(new(file, ChangeType.Delete));

                        file.Playlist.Musics.RemoveAll(v => v.Id == file.Id);
                        if (file.PlaylistVideo is not null) online.Remove(file.PlaylistVideo);
                    }
                }
            }
        }

        if (Arguments.Metadata)
        {
            Log.Section($"Fetching metadata");

            using MusicBrainzClient musicBrainz = new(new HttpClient(new SocketsHttpHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            })
            {
                DefaultRequestHeaders = { { "User-Agent", UserAgent } },
                BaseAddress = new Uri("https://musicbrainz.org/ws/2/"),
            })
            {
                Cache = new FileRequestCache(Path.Combine(Arguments.HttpCachePath, "MusicBrainz"))
                {
                    Timeout = CacheTime,
                },
            };

            using (ProgressBar progressBar = new() { MaxWidth = 70 })
            {
                foreach (MusicFile musicFile in library.Musics.ToArray().WithProgress(progressBar, v => v.ToString()))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    if (!File.Exists(musicFile.Path)) continue;

                    string outputPath = Path.Combine(Arguments.OutputPath, musicFile.Playlist.Title);

                    string name = Path.GetFileNameWithoutExtension(musicFile.Path);
                    string? originalFilename = musicFile.PlaylistVideo is not null ? GetFileNameWithoutExtension(musicFile.PlaylistVideo) : null;

                    //if (musicFile.Video is not null
                    //    && originalFilename is not null
                    //    && !originalFilename.Equals(name, StringComparison.Ordinal))
                    //{
                    //    string originalPath = Path.Combine(Path.GetDirectoryName(musicFile.Path), originalFilename + ".mp3");
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

                    musicFile.TagsFile ??= TagLib.File.Create(musicFile.Path, TagLib.ReadStyle.PictureLazy);

                    if (string.IsNullOrEmpty(musicFile.TagsFile.Tag.MusicBrainzReleaseId))
                    {
                        List<MetaGuesser.Warning>? warnings = Arguments.IgnoreMetaWarnings ? null : [];

                        if (musicFile.PlaylistVideo is not null)
                        {
                            musicFile.Meta = MetaGuesser.Guess(musicFile.PlaylistVideo, warnings);
                        }
                        else
                        {
                            musicFile.Meta = MetaGuesser.Guess(name, warnings);
                        }

                        if (warnings is not null && warnings.Count > 0)
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

                        //musicFile.TagsFile.Tag.Performers = tagDiff.Modify("Performers", musicFile.TagsFile.Tag.Performers, guessedMeta.RemixedBy is not null ? [.. guessedMeta.Artists, guessedMeta.RemixedBy] : [.. guessedMeta.Artists]);
                        //musicFile.TagsFile.Tag.Title = tagDiff.Modify("Title", musicFile.TagsFile.Tag.Title, guessedMeta.GetTitleText());
                        //musicFile.TagsFile.Tag.RemixedBy = tagDiff.Modify("RemixedBy", musicFile.TagsFile.Tag.RemixedBy, guessedMeta.RemixedBy);

                        List<string>? issues = Arguments.IgnoreMetaWarnings ? null : [];

                        await MusicBrainz.FetchMetadata(musicFile, musicBrainz, issues, cancellationToken);

                        if (issues is not null && issues.Count > 0)
                        {
                            Log.Warning($"MusicBrainz issues for {musicFile.Meta}:");
                            foreach (string issue in issues)
                            {
                                Log.WarningNoprefix(issue);
                            }
                        }

                        if (Arguments.SaveIntermediateTags && musicFile.SaveTags(Arguments.DryRun))
                        {
                            Changes.Add(new(musicFile, ChangeType.Modify));
                        }
                    }
                }
            }
        }

        if (Arguments.FixFile is not null)
        {
            Log.Section($"Fixing");

            List<(string PlaylistName, string MusicName)> musicFix = [];

            if (File.Exists(Arguments.FixFile))
            {
                Log.MinorAction($"Parsing fixfile");
                var text = await File.ReadAllTextAsync(Arguments.FixFile, cancellationToken);
                var lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    int j = line.IndexOf(' ');
                    if (j == -1)
                    {
                        Log.Warning($"Invalid fixfile line at {i + 1}");
                        continue;
                    }
                    var playlistName = line[..j];
                    var musicName = line[j..].TrimStart();
                    musicFix.Add((playlistName.ToLowerInvariant(), musicName.ToLowerInvariant()));
                }
            }
            else
            {
                Log.Error($"File {Arguments.FixFile} doesn't exists");
            }

            if (musicFix.Count > 0)
            {
                List<(Playlist Playlist, MusicFile File)> _musicFix = new();

                Log.MinorAction($"Perparing musicfix");
                foreach ((string PlaylistName, string MusicName) item in musicFix)
                {
                    Playlist? playlist = null;
                    int playlistV = int.MaxValue;

                    foreach (Playlist w in library.Playlists)
                    {
                        int k = Levenshtein.GetDistance(item.PlaylistName, w.Title.ToLowerInvariant());
                        if (k == 0)
                        {
                            playlist = w;
                            playlistV = 0;
                            break;
                        }
                        else if (k < playlistV)
                        {
                            playlist = w;
                            playlistV = k;
                        }
                    }

                    if (playlist is null || playlistV > 3)
                    {
                        Log.Warning($"Playlist \"{item.PlaylistName}\" not found");
                        continue;
                    }

                    List<(MusicFile Candidate, int Badness)> candidates = [];
                    int fileV = int.MaxValue;

                    foreach (MusicFile w in library.Musics)
                    {
                        int k = int.MaxValue;
                        foreach (string a in new string[]
                        {
                            w.Meta.Title.ToLowerInvariant(),
                            $"{string.Join(' ', w.Meta.Performers)} {w.Meta.Title}".ToLowerInvariant(),
                            Path.GetFileNameWithoutExtension(w.Path).ToLowerInvariant()
                        })
                        {
                            string b = string.Join(' ', new string(w.Meta.Title.ToLowerInvariant().Where(v => char.IsAsciiLetterOrDigit(v) || v == ' ').ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries));

                            k = Math.Min(k, Levenshtein.GetDistance(item.MusicName, a));
                            k = Math.Min(k, Levenshtein.GetDistance(item.MusicName, b));
                        }

                        if (k <= fileV)
                        {
                            candidates.RemoveAll(v => v.Badness > k + 1);
                            candidates.Add((w, k));
                            fileV = k;
                        }
                    }

                    if (candidates.Count == 0)
                    {
                        Log.Warning($"Music \"{item.MusicName}\" not found");
                        continue;
                    }

                    if (candidates.Count > 1)
                    {
                        Log.Warning($"Music \"{item.MusicName}\" is too ambigious");
                        Log.WarningNoprefix($"Candidates:");
                        candidates.Reverse();
                        foreach ((MusicFile candidate, int badness) in candidates)
                        {
                            Log.WarningNoprefix($"d:{badness} {candidate.Meta}");
                        }
                        continue;
                    }

                    (MusicFile? file, fileV) = candidates[0];

                    if (fileV > 3)
                    {
                        Log.Warning($"Music \"{item.MusicName}\" doesn't match with \"{file.Meta}\"");
                        continue;
                    }

                    if (file.Playlist == playlist)
                    {
                        Log.Warning($"Music \"{file.Meta}\" is already in \"{playlist.Title}\"");
                        continue;
                    }

                    _musicFix.Add((playlist, file));
                }

                if (_musicFix.Count == 0)
                { }
                else if (string.IsNullOrWhiteSpace(Arguments.YouTubeCredentialsPath))
                {
                    Log.Warning($"Cannot interact with the YouTube API: Credentials path not specified");
                }
                else if (!File.Exists(Arguments.YouTubeCredentialsPath))
                {
                    Log.Warning($"Cannot interact with the YouTube API: Specified credentials file doesn't exists");
                }
                else
                {
                    Log.MinorAction("Logging in");
                    YouTubeService yt = await YoutubeServiceFactory.CreateAsync(Arguments.YouTubeCredentialsPath, Path.Combine(Arguments.HttpCachePath, "token_cache"), cancellationToken);

                    foreach ((Playlist Playlist, MusicFile File) item in _musicFix)
                    {
                        try
                        {
                            YoutubeExplode.Playlists.Playlist playlist = item.File.Playlist.YouTubePlaylist;
                            PlaylistVideo video = item.File.PlaylistVideo ?? throw new NullReferenceException();

                            Log.MinorAction($"Moving music {item.File.Meta} from \"{item.File.Playlist.Title}\" to \"{item.Playlist.Title}\"");

                            if (await YouTubeUtils.RemoveFromPlaylist(yt, playlist.Id, video.Id, cancellationToken))
                            {
                                PlaylistItemsResource.InsertRequest res = yt.PlaylistItems.Insert(new()
                                {
                                    Snippet = new()
                                    {
                                        PlaylistId = item.Playlist.YouTubePlaylist.Id,
                                        ResourceId = new()
                                        {
                                            Kind = "youtube#video",
                                            VideoId = video.Id,
                                        }
                                    }
                                }, new(["snippet"]));

                                item.File.SaveTags();
                                item.File.Dispose();

                                string destination = Path.Combine(item.Playlist.Path, Path.GetFileName(item.File.Path));
                                MusicFile.Move(item.File.Path, destination, true);

                                item.File.Playlist.Musics.RemoveAll(v => v.Id == video.Id);
                                Changes.Add(new(item.File, ChangeType.Delete));

                                MusicFile newFile = new(destination, item.File.Id, item.File.Meta, item.Playlist);
                                item.Playlist.Musics.Add(newFile);
                                Changes.Add(new(newFile, ChangeType.Create));
                            }
                            else
                            {
                                Log.Error($"Video {video.Author.ChannelTitle} - {video.Title} ({video.Id}) not found in playlist {playlist.Title} ({playlist.Id}).");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                    }
                }
            }
        }

        if (Arguments.Lyrics)
        {
            Log.Section($"Fetching lyrics");

            using LrcLib lrcLib = new(new FileRequestCache(Path.Combine(Arguments.HttpCachePath, "LrcLib"))
            {
                Timeout = CacheTime,
            });

            using (ProgressBar progressBar = new() { MaxWidth = 70 })
            {
                foreach (MusicFile musicFile in library.Musics.ToArray().WithProgress(progressBar, v => v.ToString()))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    if (!File.Exists(musicFile.Path)) continue;

                    string lyricsPath = Path.ChangeExtension(musicFile.Path, ".lrc");

                    if (File.Exists(lyricsPath)) continue;

                    musicFile.TagsFile ??= TagLib.File.Create(musicFile.Path, TagLib.ReadStyle.PictureLazy);

                    string outputPath = Path.Combine(Arguments.OutputPath, musicFile.Playlist.Title);

                    if (string.IsNullOrEmpty(musicFile.Meta.Title) || musicFile.Meta.Performers.IsDefaultOrEmpty)
                    { continue; }

                    try
                    {
                        LrcLib.LyricsResponse? lyrics = await lrcLib.FetchLyrics(musicFile.Meta.Performers[0], musicFile.Meta.Title, null, null, cancellationToken);
                        if (lyrics is null) continue;
                        if (lyrics.SyncedLyrics is null && lyrics.PlainLyrics is null) continue;

                        TimeSpan lyricsDuration = TimeSpan.FromSeconds(lyrics.Duration);
                        TimeSpan? videoDuration = musicFile.PlaylistVideo?.Duration;

                        if (videoDuration.HasValue)
                        {
                            if (Math.Abs(lyricsDuration.TotalMilliseconds - videoDuration.Value.TotalMilliseconds) > 1000)
                            {
                                Log.Warning($"Lyrics issue with {musicFile.Meta}:");
                                Log.WarningNoprefix($"Duration mismatches: Video is {videoDuration.Value} Lyrics is {lyricsDuration}");
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

                        TagLib.Id3v2.Tag tag = (TagLib.Id3v2.Tag)musicFile.TagsFile.GetTag(TagLib.TagTypes.Id3v2, true);

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

                        musicFile.TagsFile.Save();
                        Log.None($"Lyrics added ({lyrics.ArtistName} - {lyrics.TrackName} [{lyrics.AlbumName}] {lyricsDuration})");

                        Changes.Add(new(musicFile, ChangeType.Modify));
                    }
                    catch (Exception ex)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        Log.Error($"Failed to download lyrics for {musicFile.Meta}");
                        Log.Error(ex);
                        continue;
                    }
                }
            }
        }

        {
            Log.Section($"Checking lyrics");

            foreach (YoutubeExplode.Playlists.Playlist playlist in playlists)
            {
                string outputPath = Path.Combine(Arguments.OutputPath, playlist.Title);

                if (!Directory.Exists(outputPath)) continue; ;

                foreach (string filename in Directory.GetFiles(outputPath, "*.lrc"))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    string musicPath = Path.ChangeExtension(filename, ".mp3");
                    if (File.Exists(musicPath)) continue;
                    if (Arguments.DryRun)
                    {
                        Log.MinorAction($"Would delete {filename}");
                    }
                    else
                    {
                        Log.MinorAction($"Deleting {filename}");
                        File.Delete(filename);
                    }
                }
            }
        }

        if (Arguments.CheckRedundancy)
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

            ImmutableArray<ImmutableArray<string>> similarPerformers = GetSimilarStrings(library.Musics.SelectMany(v => v.Meta.Performers), 4);

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

        if (Arguments.CheckDuplicates)
        {
            Log.Section($"Checking duplicates");

            List<ImmutableArray<MusicFile>> duplicates = [];
            ImmutableArray<MusicFile> all = [.. library.Musics];

            using (ProgressBar progress = new() { MaxWidth = 70 })
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

                        if (a.PlaylistVideo is not null && b.PlaylistVideo is not null
                            && MetaGuesser.Guess(a.PlaylistVideo) == MetaGuesser.Guess(b.PlaylistVideo))
                        {
                            goto dupFound;
                        }
                        else if (a.Meta.Performers.SequenceEqual(b.Meta.Performers)
                              && a.Meta.Title == b.Meta.Title
                              && a.Meta.RemixedBy == b.Meta.RemixedBy)
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
                        Console.Write(' ');
                        if (item.PlaylistVideo is not null)
                        {
                            Console.Write($"{item.PlaylistVideo.Author.ChannelTitle} - {item.PlaylistVideo.Title}");
                        }
                        else
                        {
                            Console.Write($"{Path.GetFileNameWithoutExtension(item.Path)}");
                        }
                        Console.WriteLine();
                    }
                }

                if (string.IsNullOrWhiteSpace(Arguments.YouTubeCredentialsPath))
                {
                    Log.Warning($"Cannot interact with the YouTube API: Credentials path not specified");
                }
                else if (!File.Exists(Arguments.YouTubeCredentialsPath))
                {
                    Log.Warning($"Cannot interact with the YouTube API: Specified credentials file doesn't exists");
                }
                else if (await Log.AskYesNoAsync("Do you want to remove duplicated music videos?", false, cancellationToken))
                {
                    Log.MinorAction("Logging in");
                    YouTubeService yt = await YoutubeServiceFactory.CreateAsync(Arguments.YouTubeCredentialsPath, Path.Combine(Arguments.HttpCachePath, "token_cache"), cancellationToken);

                    foreach (ImmutableArray<MusicFile> _items in duplicates)
                    {
                        ImmutableArray<MusicFile> items = [.. _items.Where(v => v.PlaylistVideo is not null)];
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
                            Console.Write($"[{item.Playlist.Title}] {item.PlaylistVideo.Author} - {item.PlaylistVideo.Title} (check https://www.youtube.com/watch?v={item.PlaylistVideo.Id} )");
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
                            MusicFile v = items[i];

                            try
                            {
                                PlaylistItemsResource.ListRequest listRequest = yt.PlaylistItems.List("id,snippet");
                                listRequest.PlaylistId = v.PlaylistVideo.PlaylistId;
                                listRequest.VideoId = v.PlaylistVideo.Id;
                                listRequest.MaxResults = 1;

                                Log.Debug($"Searching for item id in {v.PlaylistVideo.Title} ({v.PlaylistVideo.Id})");
                                Google.Apis.YouTube.v3.Data.PlaylistItemListResponse listResponse = await listRequest.ExecuteAsync(cancellationToken);
                                Google.Apis.YouTube.v3.Data.PlaylistItem? item = listResponse.Items?.FirstOrDefault();

                                if (item == null)
                                {
                                    Log.Error($"Video {v.PlaylistVideo.Author.ChannelTitle} - {v.PlaylistVideo.Title} ({v.PlaylistVideo.Id}) not found in playlist {v.PlaylistVideo.Title} ({v.PlaylistVideo.Id}).");
                                    continue;
                                }

                                Log.Debug($"Deleting item from {v.PlaylistVideo.Title} ({v.PlaylistVideo.Id})");
                                PlaylistItemsResource.DeleteRequest deleteRequest = yt.PlaylistItems.Delete(item.Id);
                                await deleteRequest.ExecuteAsync(cancellationToken);

                                foreach (MusicFile file in v.Playlist.Musics.Where(v => v.Id == v.PlaylistVideo.Id))
                                {
                                    MusicFile.Delete(file);
                                    Changes.Add(new(file, ChangeType.Delete));
                                }
                                v.Playlist.Musics.RemoveAll(v => v.Id == v.PlaylistVideo.Id);
                                online.Remove(v.PlaylistVideo);
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

        if (Arguments.RegenerateAudicousPlaylists)
        {
            Log.Section($"Regenerating Audacious playlist files");

            await Audacious.RegeneratePlaylists(library, Arguments, cancellationToken);
        }

        if (!Arguments.SyncSoundCloudPlaylists || string.IsNullOrEmpty(Arguments.SoundCloudCredentialsPath))
        {
        }
        else if (!File.Exists(Arguments.SoundCloudCredentialsPath))
        {
            Log.Section($"Synchronizing SoundCloud playlists");
            Log.Warning($"Specified SoundCloud credentials file doesn't exists");
        }
        else
        {
            SoundCloudCredentials? credentials = JsonSerializer.Deserialize<SoundCloudCredentials>(File.ReadAllText(Arguments.SoundCloudCredentialsPath));
            Log.Section($"Synchronizing SoundCloud playlists");

            List<Change<(Playlist Playlist, Track Track)>> changes = [];

            try
            {
                using SoundCloudClient soundCloudClient = new(credentials, cookies, new FileRequestCache(Path.Combine(Arguments.HttpCachePath, "SoundCloud"))
                {
                    Timeout = CacheTime,
                });

                Log.MinorAction($"Initializing SoundCloud client");
                await soundCloudClient.Initialize(cancellationToken);

                Log.MinorAction($"Fetching user information");
                Me me = await soundCloudClient.GetMe(cancellationToken);

                List<(Playlist Playlist, int TotalMatches)> statisticsPerPlaylist = [];
                int totalSearches = 0;
                int totalMatches = 0;

                Log.MinorAction($"Fetching existing playlists");
                ImmutableArray<SoundCloud.Playlist> existingPlaylists = [.. await soundCloudClient.GetPlaylists(me.Id, cancellationToken).ToArrayAsync(cancellationToken)];

                foreach (Playlist playlistContent in library.Playlists)
                {
                    if (Arguments.SoundCloudIgnore.Contains(playlistContent.YouTubePlaylist.Id)
                        || Arguments.SoundCloudIgnore.Contains(playlistContent.Title, StringComparer.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    Log.MinorAction($"Generating playlist {playlistContent.Title}");

                    List<Track> tracks = [];
                    foreach (MusicFile musicFile in playlistContent.Musics)
                    {
                        Track? track = await SoundCloudUtils.MatchTrack(musicFile, library, soundCloudClient, Arguments, cancellationToken);

                        if (track is not null)
                        {
                            if (tracks.Any(v => v.Id == track.Id))
                            {
                                Log.Warning($"Skipping adding track {track.Title} multiple times");
                                continue;
                            }
                            tracks.Add(track);
                        }
                    }

                    SoundCloud.Playlist? existingScPlaylist = existingPlaylists.FirstOrDefault(v => v.Title == playlistContent.Title);
                    totalSearches += playlistContent.Musics.Count;
                    totalMatches += tracks.Count;
                    statisticsPerPlaylist.Add((playlistContent, tracks.Count));
                    if (existingScPlaylist is null)
                    {
                        foreach (Track track in tracks)
                        {
                            changes.Add(new((playlistContent, track), ChangeType.Create));
                        }

                        if (tracks.Count == 0)
                        {
                            Log.Warning($"Skipping creating playlist {playlistContent.Title} because it would be empty");
                        }
                        else
                        {
                            Log.MinorAction($"Creating playlist {playlistContent.Title}");
                            if (!Arguments.DryRun)
                            {
                                await soundCloudClient.CreatePlaylist(new()
                                {
                                    Permalink = string.Empty,
                                    Title = playlistContent.Title,
                                    Description = $"{tracks.Count * 100 / playlistContent.Musics.Count}% ({tracks.Count}/{playlistContent.Musics.Count})",
                                    Tracks = [.. tracks.Select(v => v.Id)],
                                    Sharing = "private",
                                }, cancellationToken);
                            }
                        }
                    }
                    else
                    {
                        foreach (Track track in tracks)
                        {
                            if (existingScPlaylist.Tracks.Any(v => v.Id == track.Id)) continue;
                            changes.Add(new((playlistContent, track), ChangeType.Create));
                        }

                        foreach (Track track in existingScPlaylist.Tracks)
                        {
                            if (tracks.Any(v => v.Id == track.Id)) continue;
                            changes.Add(new((playlistContent, track), ChangeType.Delete));
                        }

                        if (existingScPlaylist.Tracks.Select(v => v.Id).SequenceEqual(tracks.Select(v => v.Id)))
                        {
                            Log.MinorAction($"Playlist {playlistContent.Title} wasn't modified");
                        }
                        else
                        {
                            Log.MinorAction($"Updating playlist {playlistContent.Title}");
                            if (!Arguments.DryRun)
                            {
                                await soundCloudClient.UpdatePlaylist(existingScPlaylist.Id, new()
                                {
                                    Permalink = existingScPlaylist.Permalink,
                                    Title = playlistContent.Title,
                                    Description = $"{tracks.Count * 100 / playlistContent.Musics.Count}% ({tracks.Count}/{playlistContent.Musics.Count})",
                                    Tracks = [.. tracks.Select(v => v.Id)],
                                    Sharing = "private",
                                    ArtworkUrl = existingScPlaylist.ArtworkUrl,
                                    Genre = existingScPlaylist.Genre ?? string.Empty,
                                    ReleaseDate = existingScPlaylist.ReleaseDate,
                                    TagList = existingScPlaylist.TagList ?? string.Empty,
                                }, cancellationToken);
                            }
                        }
                    }
                }

                int margin = statisticsPerPlaylist.Max(v => v.Playlist.Title.Length);
                foreach ((Playlist Playlist, int TotalMatches) item in statisticsPerPlaylist)
                {
                    Console.Write('[');
                    Console.Write(item.Playlist.Title);
                    Console.Write(']');
                    Console.Write(' ');
                    Console.Write(new string(' ', margin - item.Playlist.Title.Length));
                    Log.None($"{item.TotalMatches * 100 / item.Playlist.Musics.Count,3}% ({item.TotalMatches}/{item.Playlist.Musics.Count})");
                }
                Log.None($"{totalMatches * 100 / totalSearches}% ({totalMatches}/{totalSearches})");
            }
            catch (SoundCloudException ex)
            {
                Log.Error($"Failed to sync SoundCloud playlists");
                Log.Error(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to sync SoundCloud playlists");
                Log.Error(ex);
            }

            if (changes.Count > 0)
            {
                Console.WriteLine();
            }

            YtPlaylist.Changes.Print(changes, v =>
            {
                Console.Write('[');
                Console.Write(v.Playlist.Title);
                Console.Write(']');
                Console.Write(' ');
                if (!string.IsNullOrWhiteSpace(v.Track.Title))
                {
                    Console.Write(v.Track.Title);
                }
                else if (!string.IsNullOrWhiteSpace(v.Track.PermalinkUrl))
                {
                    Console.Write(v.Track.PermalinkUrl);
                }
                else
                {
                    Console.Write($"<{v.Track.Id}>");
                }
                Console.WriteLine();
            });
        }

        Log.Section($"Saving meta tags");

        foreach (MusicFile musicFile in library.Musics)
        {
            if (musicFile.SaveTags(Arguments.DryRun))
            {
                Changes.Add(new(musicFile, ChangeType.Modify));
            }
        }

        Log.Section($"Done");

        if (Changes.Count > 0)
        {
            Console.WriteLine();
        }

        YtPlaylist.Changes.Print(Changes, static v =>
        {
            Console.Write($"[{v.Playlist.Title}] ");
            if (v.PlaylistVideo is null)
            {
                Console.WriteLine(Path.GetFileNameWithoutExtension(v.Path));
            }
            else
            {
                Console.WriteLine($"{v.PlaylistVideo.Author.ChannelTitle} - {v.PlaylistVideo.Title}");
            }
        });

        Console.WriteLine();
    }

    #region YouTube

    async Task DownloadPlaylist(YoutubeClient youtube, Playlist playlist, YouTubeCache? youTubeCache, List<PlaylistVideo> online, string path, CancellationToken cancellationToken = default)
    {
        Channel<PlaylistVideo> channel = Channel.CreateUnbounded<PlaylistVideo>();

        Span<Task> tasks = new Task[1 + MaxConcurrency];

        if (Arguments.UseCache && youTubeCache is not null && youTubeCache.LoadPlaylistItems(playlist.YouTubePlaylist.Id.Value, out ImmutableArray<PlaylistVideo> items))
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
                await foreach (Batch<PlaylistVideo> batch in youtube.Playlists.GetVideoBatchesAsync(playlist.YouTubePlaylist.Url, cancellationToken))
                {
                    foreach (PlaylistVideo video in batch.Items)
                    {
                        online.Add(video);
                        videos.Add(video);
                        await channel.Writer.WriteAsync(video, cancellationToken);
                    }
                }
                youTubeCache?.SavePlaylistItems(playlist.YouTubePlaylist.Id.Value, videos);
                channel.Writer.Complete();
                if (cancellationToken.IsCancellationRequested) return;
            }, cancellationToken);
        }

        for (int i = 0; i < MaxConcurrency; i++)
        {
            tasks[i + 1] = DownloadPlaylistJob(youtube, playlist, youTubeCache, channel, path, cancellationToken);
        }

        await Task.WhenAll(tasks);
    }

    async Task DownloadPlaylistJob(YoutubeClient youtube, Playlist playlist, YouTubeCache? youTubeCache, Channel<PlaylistVideo> channel, string path, CancellationToken cancellationToken = default)
    {
        await foreach (PlaylistVideo video in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;

            await HandleVideo(youtube, playlist, youTubeCache, video, path, cancellationToken);
        }
    }

    string GetFileNameWithoutExtension(PlaylistVideo video)
    {
        MusicMeta meta = MetaGuesser.Guess(video, []);
        return SanitizeFilename($"{meta.GetArtistsText()} - {meta.GetTitleText()}");
    }

    async Task HandleVideo(YoutubeClient youtube, Playlist playlist, YouTubeCache? youtubeCache, PlaylistVideo video, string path, CancellationToken cancellationToken = default)
    {
        MusicFile? musicFile = playlist.Musics.FirstOrDefault(v => v.Id == video.Id.Value);
        if (musicFile is not null)
        {
            musicFile.PlaylistVideo = video;
            goto meta;
        }

        string filename = Path.Combine(path, $"{GetFileNameWithoutExtension(video)}.mp3");

        if (File.Exists(filename))
        {
            playlist.Musics.Add(musicFile = new MusicFile(filename, video.Id, new MusicMeta([], Path.GetFileNameWithoutExtension(filename)), playlist)
            {
                PlaylistVideo = video,
            });
        }

        if (Arguments.Download)
        {
            if (musicFile is not null)
            {
                Log.Debug($"File \"{Path.GetFileName(filename)}\" already exists, skipping entirely");
                return;
            }

            if (Arguments.DryRun)
            {
                Log.MinorAction($"Should download {Ansi.Bold(video.Author.ChannelTitle)} - {Ansi.Bold(video.Title)}");
            }
            else
            {
                Log.MinorAction($"Downloading {Ansi.Bold(video.Author.ChannelTitle)} - {Ansi.Bold(video.Title)}");

                try
                {
                    await RunRetries(
                        (cancellationToken) => Task.Run(() => YtDlp.DownloadAudioData(filename, $"https://www.youtube.com/watch?v={video.Id}", Arguments.YtDlpAdditionalArguments), cancellationToken),
                        GenericHttpRetryFilter,
                        MaxRetries,
                        cancellationToken
                    );
                }
                catch (HttpRequestException ex)
                {
                    Log.Error($"Failed to download {Ansi.Bold(video.Author.ChannelTitle)} - {Ansi.Bold(video.Title)}: HTTP {(int)ex.StatusCode} ({ex.StatusCode})");
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                playlist.Musics.Add(musicFile = new MusicFile(filename, video.Id, new MusicMeta([], Path.GetFileNameWithoutExtension(filename)), playlist)
                {
                    PlaylistVideo = video,
                });

                Changes.Add(new(musicFile, ChangeType.Create));
            }
        }
        else if (musicFile is null)
        {
            return;
        }

    meta:

        if (musicFile is not null)
        {
            musicFile.OpenTags();

            musicFile.TagsFile.Tag.Description = musicFile.TagsDiff.Modify("Description", musicFile.TagsFile.Tag.Description, video.Id.Value);

            if (Arguments.Metadata)
            {
                await YouTube.FetchMetadata(musicFile, youtube, youtubeCache, cancellationToken);
            }

            if (Arguments.SaveIntermediateTags && musicFile.SaveTags(Arguments.DryRun))
            {
                Changes.Add(new(musicFile, ChangeType.Modify));
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

    static async Task RunRetries(Func<CancellationToken, Task> callback, Func<Exception, CancellationToken, Task<bool>> exceptionHandler, int retries, CancellationToken cancellationToken = default)
    {
        int retry = 0;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested) return;
            try
            {
                await callback.Invoke(cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (!await exceptionHandler.Invoke(ex, cancellationToken)) throw;
                if (retry++ >= retries) throw;
                continue;
            }
        }
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

        filename = Confusables.Replace(filename, _confusables);

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
