using System.Collections.Immutable;
using Logger;

namespace YtPlaylist;

static class Program
{
    static int Main(string[] args)
    {
        List<string> playlistIds = [];
        string? outputPath = null;
        bool useCache = false;
        bool dryRun = false;
        bool download = true;
        bool metadata = true;
        bool lyrics = true;
        string? httpCachePath = null;
        string? youtubeCredentialsPath = null;
        string? soundcloudCredentialsPath = null;
        string? cookiesPath = null;
        bool ignoreMetaWarnings = false;
        bool recreateMetadata = false;
        bool checkRedundancy = false;
        bool checkDuplicates = false;
        bool regenerateAudicousPlaylists = false;
        bool syncSoundCloudPlaylists = false;
        bool ignoreSoundCloudMatchWarnings = false;
        bool saveIntermediateTags = false;
        List<string> soundcloudIgnore = [];
        List<string> additionalYtDlpArguments = [];

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p" or "--playlist":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a playlist id after the argument {args[i]}");
                        return 1;
                    }

                    playlistIds.Add(args[++i]);
                    break;
                case "-o" or "--output":
                    if (outputPath is not null)
                    {
                        Log.Error($"Output directory already defined");
                        return 1;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected an output directory after the argument {args[i]}");
                        return 1;
                    }

                    outputPath = args[++i];
                    break;
                case "--use-cache":
                    useCache = true;
                    break;
                case "--reset-meta":
                    recreateMetadata = true;
                    break;
                case "--dry":
                    dryRun = true;
                    break;
                case "--no-download":
                    download = false;
                    break;
                case "--no-metadata":
                    metadata = false;
                    break;
                case "--no-lyrics":
                    lyrics = false;
                    break;
                case "--ignore-meta-warnings":
                    ignoreMetaWarnings = true;
                    break;
                case "--http-cache":
                    if (httpCachePath is not null)
                    {
                        Log.Error($"HTTP cache path already defined");
                        return 1;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        return 1;
                    }

                    httpCachePath = args[++i];
                    break;
                case "--youtube-credentials":
                    if (youtubeCredentialsPath is not null)
                    {
                        Log.Error($"YouTube credentials path already defined");
                        return 1;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        return 1;
                    }

                    youtubeCredentialsPath = args[++i];
                    break;
                case "--soundcloud-credentials":
                    if (soundcloudCredentialsPath is not null)
                    {
                        Log.Error($"SoundCloud credentials path already defined");
                        return 1;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        return 1;
                    }

                    soundcloudCredentialsPath = args[++i];
                    break;
                case "--cookies":
                    if (cookiesPath is not null)
                    {
                        Log.Error($"Cookies path already defined");
                        return 1;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        return 1;
                    }

                    cookiesPath = args[++i];
                    break;
                case "--check-redundancy":
                    checkRedundancy = true;
                    break;
                case "--check-duplicates":
                    checkDuplicates = true;
                    break;
                case "--regenerate-audicous-playlists":
                    regenerateAudicousPlaylists = true;
                    break;
                case "--sync-soundcloud-playlists":
                    syncSoundCloudPlaylists = true;
                    break;
                case "--ignore-soundcloud-match-warnings":
                    ignoreSoundCloudMatchWarnings = true;
                    break;
                case "--soundcloud-ignore":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a value after the argument {args[i]}");
                        return 1;
                    }

                    soundcloudIgnore.AddRange(args[++i]);
                    break;
                case "--save-intermediate-tags":
                    saveIntermediateTags = true;
                    break;
                case "--ytdlp":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a value after the argument {args[i]}");
                        return 1;
                    }

                    additionalYtDlpArguments.AddRange(args[++i]);
                    break;
                default:
                    Log.Error($"Unexpected argument {args[i]}");
                    return 1;
            }
        }

        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine("YouTube Playlist Downloader");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("ytsync <-p|--playlist Playlist Id> <-o|--output Output Directory>");
            return 1;
        }

        if (playlistIds.Count == 0)
        {
            Log.Error($"No playlist specified");
            return 1;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Log.Error($"Output directory not specified");
            return 1;
        }

        if (!Directory.Exists(outputPath))
        {
            Log.Error($"Output directory doesn't exists {outputPath}");
            return 1;
        }

        CancellationTokenSource cancellationTokenSource = new();

        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        new App()
        {
            Arguments = new AppArguments()
            {
                PlaylistIds = [.. playlistIds],
                UseCache = useCache,
                DryRun = dryRun,
                Download = download,
                Metadata = metadata,
                Lyrics = lyrics,
                OutputPath = outputPath,
                HttpCachePath = httpCachePath ?? "./cache",
                IgnoreMetaWarnings = ignoreMetaWarnings,
                YouTubeCredentialsPath = youtubeCredentialsPath,
                SoundCloudCredentialsPath = soundcloudCredentialsPath,
                CookiesPath = cookiesPath,
                RecreateMetadata = recreateMetadata,
                CheckDuplicates = checkDuplicates,
                CheckRedundancy = checkRedundancy,
                RegenerateAudicousPlaylists = regenerateAudicousPlaylists,
                SyncSoundCloudPlaylists = syncSoundCloudPlaylists,
                SoundCloudIgnore = [.. soundcloudIgnore],
                IgnoreSoundCloudMatchWarnings = ignoreSoundCloudMatchWarnings,
                SaveIntermediateTags = saveIntermediateTags,
                YtDlpAdditionalArguments = [.. additionalYtDlpArguments],
            },
        }.Run(cancellationTokenSource.Token).ContinueWith(task =>
        {
            if (task.Exception is not null)
            {
                foreach (Exception item in task.Exception.Flatten().InnerExceptions)
                {
                    Log.Error(item);
                }
            }
        }).Wait();

        return 0;
    }
}
