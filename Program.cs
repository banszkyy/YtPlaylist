using System.Collections.Immutable;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Logger;

namespace YtPlaylist;

static class Program
{
    readonly struct YouTubePlaylistItem(PlaylistItem item)
    {
        public string ResourceId => item.Id;
        public string? VideoId => item.ContentDetails?.VideoId;
    }

    readonly struct YouTubePlaylist
    {
        public static async IAsyncEnumerable<YouTubePlaylistItem> GetItems(YouTubeService youtube, string playlistId)
        {
            string? nextPage = null;

            do
            {
                PlaylistItemsResource.ListRequest req = youtube.PlaylistItems.List("contentDetails");
                req.PlaylistId = playlistId;
                req.MaxResults = 50;
                req.PageToken = nextPage;

                PlaylistItemListResponse res = await req.ExecuteAsync().ConfigureAwait(false);

                foreach (PlaylistItem? item in res.Items)
                {
                    yield return new YouTubePlaylistItem(item);
                }

                nextPage = res.NextPageToken;
            }
            while (nextPage is not null);
        }

        public static async Task DeleteItem(YouTubeService youtube, YouTubePlaylistItem item)
        {
            await youtube.PlaylistItems.Delete(item.ResourceId).ExecuteAsync().ConfigureAwait(false);
        }

        public static async Task<YouTubePlaylistItem> AddItem(YouTubeService youtube, string playlistId, string videoId)
        {
            return new YouTubePlaylistItem(await youtube.PlaylistItems.Insert(new PlaylistItem()
            {
                Snippet = new PlaylistItemSnippet()
                {
                    PlaylistId = playlistId,
                    ResourceId = new() { Kind = "youtube#video", VideoId = videoId }
                }
            }, "snippet").ExecuteAsync().ConfigureAwait(false));
        }
    }

    static int Main(string[] args)
    {
        //YouTubeService yt = await YoutubeServiceFactory.CreateAsync();
        //
        //ImmutableArray<string> all = [.. File.ReadAllLines("/home/bb/Projects/YtPlaylist/backup.txt")];
        ////await foreach (YouTubePlaylistItem item in YouTubePlaylist.GetItems(yt, "PL3pKDp-F7PPtqyA3Q_F8lpLohgbZnOAiU"))
        ////{
        ////    File.AppendAllLines("/home/bb/Projects/YtPlaylist/backup.txt", [item.VideoId ?? string.Empty]);
        ////}
        //
        //foreach (string item in all.Skip(193))
        //{
        //    await YouTubePlaylist.AddItem(yt, "PL3pKDp-F7PPsEeyNmtYYBhM6u6TY_tpx7", item).ConfigureAwait(false);
        //}
        //
        //return 0;

#if DEBUG
        args = (
            $"--playlist PL3pKDp-F7PPuo3MIneE9MX77zKcEiw-QZ " +
            $"--playlist PL3pKDp-F7PPu_1Sz9dMu3GLkZVAw70pL1 " +
            $"--playlist PL3pKDp-F7PPu785eiO43ccKgaOCLhpTBJ " +
            $"--playlist PL3pKDp-F7PPuI_BsyPZfXtNySJ5By-Yrb " +
            $"--playlist PL3pKDp-F7PPvdl7-_7m6iZ_KNdIH70abQ " +
            $"--playlist PL3pKDp-F7PPu0DSnRGuNUCttOO4ZjGA2T " +
            $"--playlist PL3pKDp-F7PPvEzNztKC-Auf-1TdPqjIIN " +
            $"--playlist PL3pKDp-F7PPvEi_vgUWlMjyAqT8uCu_KA " +
            $"--playlist PL3pKDp-F7PPtX38yvSUEDQb24g-EQe-gQ " +
            $"--playlist PL3pKDp-F7PPv1HlqI3VuTd1Hj7DzfmNCi " +
            $"--playlist PL3pKDp-F7PPueGCqGhQiNYE5RQCG5fwLC " +
            $"--playlist PL3pKDp-F7PPsMhUgKJwprtal_vP0aulsC " +
            $"--playlist PL3pKDp-F7PPu_gEzx1zeoY7zm0uQ6WOTV " +
            $"--playlist PL3pKDp-F7PPvsLhK3ufdyBuu73uL6I4WD " +
            $"--playlist PL3pKDp-F7PPuQ90i5efLdi9ZurVjNXXSz " +
            $"--playlist PL3pKDp-F7PPt340bvoRQ-VvPBOhlM8uCu " +
            $"--playlist PL3pKDp-F7PPsdm2p3bMOEw0kQA8FhE5kJ " +
            $"--playlist PL3pKDp-F7PPuRGcaVhxoyK2PzR-1xk4jw " +
            $"--playlist PL3pKDp-F7PPu9t2xlu6DI-5J-ztL1Ddi- " +
            $"--playlist PL3pKDp-F7PPvAKSEGEUpmrfj6fN4M8hRd " +
            $"--httpcache /home/bb/Projects/YtPlaylist/cache " +
            $"--output /d2/Music " +
            //$"--dry " +
            string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
#endif

        List<string> playlistIds = [];
        string? outputPath = null;
        bool useCache = true;
        bool dryRun = false;
        bool download = true;
        bool metadata = true;
        bool lyrics = true;
        string? httpCachePath = null;
        bool ignoreMetaWarnings = false;

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
                case "--nocache":
                    useCache = false;
                    break;
                case "--dry":
                    dryRun = true;
                    break;
                case "--nodownload":
                    download = false;
                    break;
                case "--nometadata":
                    metadata = false;
                    break;
                case "--nolyrics":
                    lyrics = false;
                    break;
                case "--ignoremetawarnings":
                    ignoreMetaWarnings = true;
                    break;
                case "--httpcache":
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
            Console.WriteLine("YtPlaylist <-p|--playlist Playlist Id> <-o|--output Output Directory>");
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
