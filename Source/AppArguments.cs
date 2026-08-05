using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Logger;

namespace YtPlaylist;

sealed class AppArguments
{
    public ImmutableArray<string> PlaylistIds { get; private set; } = [];
    public bool UseCache { get; private set; }
    [NotNull] public string? HttpCachePath { get; private set; }
    public string? YouTubeCredentialsPath { get; private set; }
    public string? SoundCloudCredentialsPath { get; private set; }
    public string? SpotifyCredentialsPath { get; private set; }
    public string? CookiesPath { get; private set; }
    public bool DryRun { get; private set; }
    public bool Download { get; private set; } = true;
    public bool Metadata { get; private set; } = true;
    public bool Lyrics { get; private set; } = true;
    [NotNull] public string? OutputPath { get; private set; }
    public bool IgnoreMetaWarnings { get; private set; }
    public bool RecreateMetadata { get; private set; }
    public bool CheckRedundancy { get; private set; }
    public bool CheckDuplicates { get; private set; }
    public bool RegenerateAudicousPlaylists { get; private set; }
    public bool SyncSoundCloudPlaylists { get; private set; }
    public bool SyncSpotifyPlaylists { get; private set; }
    public bool IgnoreSoundCloudMatchWarnings { get; private set; }
    public bool IgnoreSpotifyMatchWarnings { get; private set; }
    public string? FixFile { get; private set; }
    public bool SaveIntermediateTags { get; private set; }
    public ImmutableArray<string> SoundCloudIgnore { get; private set; } = [];
    public ImmutableArray<string> SpotifyIgnore { get; private set; } = [];
    public ImmutableArray<string> YtDlpAdditionalArguments { get; private set; } = [];

    public static (AppArguments Arguments, bool Success) Read(string[] args)
    {
        AppArguments v = new();
        bool success = true;
        Regex playlistIdRegex = new(@"^[a-zA-Z0-9-_]+$");
        Regex playlistRegex = new(@"youtube\.com.*list=([a-zA-Z0-9-_]+)");

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("---")) continue;

            switch (args[i])
            {
                case "-p" or "--playlist":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a playlist id after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    string w = args[++i];

                    Match match = playlistRegex.Match(w);
                    if (match.Success)
                    {
                        w = match.Groups[1].Value;
                    }
                    else if (!playlistIdRegex.IsMatch(w))
                    {
                        Log.Warning($"Playlist \"{w}\" might be invalid");
                    }

                    v.PlaylistIds = v.PlaylistIds.Add(w);
                    break;
                case "-o" or "--output":
                    if (v.OutputPath is not null)
                    {
                        Log.Error($"Output directory already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected an output directory after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.OutputPath = args[++i];
                    break;
                case "--use-cache":
                    if (v.UseCache) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.UseCache = true;
                    break;
                case "--reset-meta":
                    if (v.RecreateMetadata) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.RecreateMetadata = true;
                    break;
                case "--dry":
                    if (v.DryRun) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.DryRun = true;
                    break;
                case "--no-download":
                    if (!v.Download) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.Download = false;
                    break;
                case "--no-metadata":
                    if (!v.Metadata) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.Metadata = false;
                    break;
                case "--no-lyrics":
                    if (!v.Lyrics) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.Lyrics = false;
                    break;
                case "--ignore-meta-warnings":
                    if (v.IgnoreMetaWarnings) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.IgnoreMetaWarnings = true;
                    break;
                case "--http-cache":
                    if (v.HttpCachePath is not null)
                    {
                        Log.Error($"HTTP cache path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.HttpCachePath = args[++i];
                    break;
                case "--youtube-credentials":
                    if (v.YouTubeCredentialsPath is not null)
                    {
                        Log.Error($"YouTube credentials path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.YouTubeCredentialsPath = args[++i];
                    break;
                case "--soundcloud-credentials":
                    if (v.SoundCloudCredentialsPath is not null)
                    {
                        Log.Error($"SoundCloud credentials path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SoundCloudCredentialsPath = args[++i];
                    break;
                case "--spotify-credentials":
                    if (v.SoundCloudCredentialsPath is not null)
                    {
                        Log.Error($"Spotify credentials path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SpotifyCredentialsPath = args[++i];
                    break;
                case "--cookies":
                    if (v.CookiesPath is not null)
                    {
                        Log.Error($"Cookies path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.CookiesPath = args[++i];
                    break;
                case "--check-redundancy":
                    if (v.CheckRedundancy) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.CheckRedundancy = true;
                    break;
                case "--check-duplicates":
                    if (v.CheckDuplicates) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.CheckDuplicates = true;
                    break;
                case "--regenerate-audicous-playlists":
                    if (v.RegenerateAudicousPlaylists) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.RegenerateAudicousPlaylists = true;
                    break;
                case "--sync-soundcloud-playlists":
                    if (v.SyncSoundCloudPlaylists) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.SyncSoundCloudPlaylists = true;
                    break;
                case "--sync-spotify-playlists":
                    if (v.SyncSpotifyPlaylists) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.SyncSpotifyPlaylists = true;
                    break;
                case "--ignore-match-warnings":
                    v.IgnoreSoundCloudMatchWarnings = true;
                    v.IgnoreSpotifyMatchWarnings = true;
                    break;
                case "--ignore-soundcloud-match-warnings":
                    if (v.IgnoreSoundCloudMatchWarnings) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.IgnoreSoundCloudMatchWarnings = true;
                    break;
                case "--ignore-spotify-match-warnings":
                    if (v.IgnoreSpotifyMatchWarnings) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.IgnoreSpotifyMatchWarnings = true;
                    break;
                case "--soundcloud-sync-ignore":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a value after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SoundCloudIgnore = v.SoundCloudIgnore.Add(args[++i]);
                    break;
                case "--spotify-sync-ignore":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a value after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SoundCloudIgnore = v.SoundCloudIgnore.Add(args[++i]);
                    break;
                case "--sync-ignore":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a value after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    w = args[++i];

                    v.SoundCloudIgnore = v.SoundCloudIgnore.Add(w);
                    v.SpotifyIgnore = v.SpotifyIgnore.Add(w);
                    break;
                case "--save-intermediate-tags":
                    if (v.SaveIntermediateTags) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.SaveIntermediateTags = true;
                    break;
                case "--ytdlp":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a value after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.YtDlpAdditionalArguments = v.YtDlpAdditionalArguments.Add(args[++i]);
                    break;
                case "--fixfile":
                    if (v.FixFile is not null)
                    {
                        Log.Error($"Fixfile already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a value after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.FixFile = args[++i];
                    break;
                default:
                    Log.Error($"Unexpected argument {args[i]}");
                    success = false;
                    continue;
            }
        }

        if (v.PlaylistIds.Length == 0)
        {
            Log.Error($"No playlist passed");
            success = false;
        }

        if (string.IsNullOrEmpty(v.OutputPath))
        {
            Log.Error($"Output directory not specified");
            success = false;
        }

        if (!Directory.Exists(v.OutputPath))
        {
            Log.Error($"Output directory \"{v.OutputPath}\" doesn't exists");
        }

        v.HttpCachePath ??= "./cache";

        return (v, success);
    }
}
