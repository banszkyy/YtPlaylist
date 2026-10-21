using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsToml;
using CsToml.Values;
using Logger;

namespace YtPlaylist;

sealed partial class AppArguments
{
    public ImmutableArray<string> PlaylistIds { get; private set; } = [];
    public bool UseCache { get; private set; }
    [NotNull] public string? HttpCachePath { get; private set; }
    [NotNull] public TimeSpan? HttpCacheLifetime { get; private set; }
    public string? YouTubeCredentialsPath { get; private set; }
    public SoundCloud.SoundCloudCredentials? SoundCloudCredentials { get; private set; }
    public Spotify.SpotifyCredentials? SpotifyCredentials { get; private set; }
    public string? SoundCloudDebugPath { get; private set; }
    public string? SoundCloudCookiesPath { get; private set; }
    public string? SpotifyDebugPath { get; private set; }
    public string? SpotifyCookiesPath { get; private set; }
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
    public bool ExportM3U { get; private set; }
    public bool ExportPLS { get; private set; }
    public bool SyncSoundCloudPlaylists { get; private set; }
    public bool SyncSpotifyPlaylists { get; private set; }
    public bool IgnoreSoundCloudMatchWarnings { get; private set; }
    public bool IgnoreSpotifyMatchWarnings { get; private set; }
    public string? FixFile { get; private set; }
    public string? Har { get; private set; }
    public bool SaveIntermediateTags { get; private set; }
    public ImmutableArray<string> SoundCloudIgnore { get; private set; } = [];
    public ImmutableArray<string> SpotifyIgnore { get; private set; } = [];
    public ImmutableArray<string> YtDlpAdditionalArguments { get; private set; } = [];

    public static (AppArguments Arguments, bool Success) Read(string[] args)
    {
        AppArguments v = new();
        bool success = true;

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
                    {
                        if (v.SoundCloudCredentials is not null)
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

                        string x = args[++i];

                        if (!File.Exists(x))
                        {
                            Log.Error($"File \"{x}\" doesn't exists");
                            success = false;
                            continue;
                        }

                        v.SoundCloudCredentials = JsonSerializer.Deserialize<SoundCloud.SoundCloudCredentials>(File.ReadAllText(x)) ?? throw new JsonException();
                        break;
                    }
                case "--spotify-credentials":
                    {
                        if (v.SpotifyCredentials is not null)
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


                        string x = args[++i];

                        if (!File.Exists(x))
                        {
                            Log.Error($"File \"{x}\" doesn't exists");
                            success = false;
                            continue;
                        }

                        v.SpotifyCredentials = JsonSerializer.Deserialize<Spotify.SpotifyCredentials>(File.ReadAllText(x)) ?? throw new JsonException();
                        break;
                    }
                case "--soundcloud-debug":
                    if (v.SoundCloudDebugPath is not null)
                    {
                        Log.Error($"SoundCloud debug path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SoundCloudDebugPath = args[++i];
                    break;
                case "--spotify-debug":
                    if (v.SpotifyDebugPath is not null)
                    {
                        Log.Error($"Spotify debug path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SpotifyDebugPath = args[++i];
                    break;
                case "--soundcloud-cookies":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SpotifyCookiesPath = args[++i];
                    break;
                case "--spotify-cookies":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.SoundCloudCookiesPath = args[++i];
                    break;
                case "--cache-lifetime":
                    if (v.HttpCacheLifetime.HasValue)
                    {
                        Log.Error($"HTTP cache lifetime already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    string y = args[++i];

                    if (!ParseTimeSpan(y, out TimeSpan cacheLifetime))
                    {
                        success = false;
                        continue;
                    }
                    v.HttpCacheLifetime = cacheLifetime;
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
                case "--export-m3u":
                    if (v.ExportM3U) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.ExportM3U = true;
                    break;
                case "--export-pls":
                    if (v.ExportPLS) Log.Warning($"Argument \"{args[i]}\" already passed");
                    v.ExportPLS = true;
                    break;
                case "--har":
                    if (v.Har is not null)
                    {
                        Log.Error($"HAR path already defined");
                        success = false;
                        continue;
                    }

                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a path name after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    v.Har = args[++i];
                    break;
                case "-c" or "--config":
                    if (i + 1 == args.Length)
                    {
                        Log.Error($"Expected a filename after the argument {args[i]}");
                        success = false;
                        continue;
                    }

                    {
                        string c = args[++i];
                        if (!File.Exists(c))
                        {
                            Log.Error($"File \"{c}\" doesn't exists");
                            success = false;
                            continue;
                        }

                        using FileStream f = new(c, FileMode.Open, FileAccess.Read);
                        TomlDocument doc = CsTomlSerializer.Deserialize<TomlDocument>(f);
                        if (!ReadConfig(doc, v))
                        {
                            Log.Error($"Failed to read config \"{c}\"");
                            success = false;
                            continue;
                        }
                        break;
                    }
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
        v.HttpCacheLifetime ??= TimeSpan.MaxValue;

        return (v, success);
    }

    static bool ParseTimeSpan(string value, out TimeSpan timeSpan)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            timeSpan = TimeSpan.Zero;
            return false;
        }

        if (value == "i")
        {
            timeSpan = TimeSpan.MaxValue;
            return true;
        }

        if (value == "0")
        {
            timeSpan = TimeSpan.Zero;
            return true;
        }

        timeSpan = TimeSpan.Zero;
        foreach (string item in value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (item.Length <= 1)
            {
                Log.Error($"Invalid time segment \"{item}\"");
                return false;
            }

            if (!long.TryParse(item[..^1], out long x))
            {
                Log.Error($"Invalid time segment number \"{item[..^1]}\"");
                return false;
            }

            switch (item[^1])
            {
                case 's': timeSpan = timeSpan.Add(TimeSpan.FromSeconds(x)); break;
                case 'm': timeSpan = timeSpan.Add(TimeSpan.FromMinutes(x)); break;
                case 'h': timeSpan = timeSpan.Add(TimeSpan.FromHours(x)); break;
                case 'd': timeSpan = timeSpan.Add(TimeSpan.FromDays(x)); break;
                case 'w': timeSpan = timeSpan.Add(TimeSpan.FromDays(x * 7)); break;
                case 'M': timeSpan = timeSpan.Add(TimeSpan.FromDays(x * 30)); break;
                case 'y': timeSpan = timeSpan.Add(TimeSpan.FromDays(x * 365)); break;
                default: Log.Error($"Invalid time segment suffix \"{item[^1]}\""); return false;
            }
        }

        return true;
    }

    static bool ReadConfig(TomlDocument doc, AppArguments v)
    {
        bool success = true;

        if (doc.RootNode["playlists"].TryGetArray(out ReadOnlyCollection<TomlValue>? playlist))
        {
            foreach (TomlValue item in playlist)
            {
                if (!item.TryGetString(out string w))
                {
                    Log.Error($"Invalid playlist item {item}");
                    success = false;
                    continue;
                }

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
            }
        }

        if (doc.RootNode["output"].TryGetString(out string output))
        {
            v.OutputPath = output;
        }

        if (doc.RootNode["dry"].TryGetBool(out bool dryRun))
        {
            v.DryRun = dryRun;
        }

        if (doc.RootNode["check-duplicates"].TryGetBool(out bool checkDuplicates))
        {
            v.CheckDuplicates = checkDuplicates;
        }

        if (doc.RootNode["lyrics"].TryGetBool(out bool lyrics))
        {
            v.Lyrics = lyrics;
        }

        if (doc.RootNode["download"].TryGetBool(out bool download))
        {
            v.Download = download;
        }

        if (doc.RootNode["metadata"].TryGetBool(out bool metadata))
        {
            v.Metadata = metadata;
        }

        if (doc.RootNode["reset-meta"].TryGetBool(out bool resetMetadata))
        {
            v.RecreateMetadata = resetMetadata;
        }

        if (doc.RootNode["check-redundancy"].TryGetBool(out bool checkRedundancy))
        {
            v.CheckRedundancy = checkRedundancy;
        }

        if (doc.RootNode["save-intermediate-tags"].TryGetBool(out bool saveIntermediateTags))
        {
            v.SaveIntermediateTags = saveIntermediateTags;
        }

        if (doc.RootNode["fixfile"].TryGetString(out string? fixfile))
        {
            if (File.Exists(fixfile))
            {
                v.FixFile = fixfile;
            }
            else
            {
                Log.Warning($"File \"{fixfile}\" doesn't exists");
                v.FixFile = null;
            }
        }

        if (doc.RootNode["sync-ignore"].TryGetArray(out ReadOnlyCollection<TomlValue>? syncIgnore))
        {
            foreach (TomlValue item in syncIgnore)
            {
                if (!item.TryGetString(out string w))
                {
                    Log.Error($"Invalid sync ignore item {item}");
                    success = false;
                    continue;
                }

                v.SpotifyIgnore = v.SpotifyIgnore.Add(w);
                v.SoundCloudIgnore = v.SoundCloudIgnore.Add(w);
            }
        }

        if (doc.RootNode["log"].HasValue)
        {
            if (doc.RootNode["log"]["ignore-meta-warnings"].TryGetBool(out bool ignoreMetaWarnings))
            {
                v.IgnoreMetaWarnings = ignoreMetaWarnings;
            }

            if (doc.RootNode["log"]["har"].TryGetString(out string? har))
            {
                v.Har = har;
            }
        }

        if (doc.RootNode["cache"].HasValue)
        {
            if (doc.RootNode["cache"]["force"].TryGetBool(out bool force))
            {
                v.UseCache = force;
            }

            if (doc.RootNode["cache"]["path"].TryGetString(out string? path))
            {
                v.HttpCachePath = path;
            }

            if (doc.RootNode["cache"]["lifetime"].TryGetString(out string? lifetime))
            {
                if (!ParseTimeSpan(lifetime, out TimeSpan lifetimeV))
                {
                    success = false;
                }
                else
                {
                    v.HttpCacheLifetime = lifetimeV;
                }
            }
        }

        if (doc.RootNode["soundcloud"].HasValue)
        {
            if (doc.RootNode["soundcloud"]["sync"].TryGetBool(out bool sync))
            {
                v.SyncSoundCloudPlaylists = sync;
            }

            if (doc.RootNode["soundcloud"]["ignore-match-warnings"].TryGetBool(out bool ignoreMatchWarnings))
            {
                v.IgnoreSoundCloudMatchWarnings = ignoreMatchWarnings;
            }

            if (doc.RootNode["soundcloud"]["cookies"].TryGetString(out string? cookies))
            {
                if (!File.Exists(cookies))
                {
                    Log.Error($"File \"{cookies}\" doesn't exists");
                    success = false;
                }

                v.SoundCloudCookiesPath = cookies;
            }

            if (doc.RootNode["soundcloud"]["debug"].TryGetString(out string? debug))
            {
                v.SoundCloudDebugPath = debug;
            }

            if (doc.RootNode["soundcloud"]["credentials"].TryGetString(out string? credentials))
            {
                if (!File.Exists(credentials))
                {
                    Log.Warning($"File \"{credentials}\" doesn't exists");
                    success = false;
                }
                else
                {
                    v.SoundCloudCredentials = JsonSerializer.Deserialize<SoundCloud.SoundCloudCredentials>(File.ReadAllText(credentials)) ?? throw new JsonException();
                }
            }
            else if (doc.RootNode["soundcloud"]["credentials"].HasValue)
            {
                if (doc.RootNode["soundcloud"]["credentials"]["user-agent"].TryGetString(out string? userAgent)
                    && doc.RootNode["soundcloud"]["credentials"]["user-agent"].TryGetString(out string? jslp)
                    && doc.RootNode["soundcloud"]["credentials"]["user-agent"].TryGetString(out string? sessionId)
                    && doc.RootNode["soundcloud"]["credentials"]["user-agent"].TryGetString(out string? token))
                {
                    v.SoundCloudCredentials = new()
                    {
                        UserAgent = userAgent,
                        Jspl = jslp,
                        SessionId = sessionId,
                        Token = token,
                    };
                }
            }
        }

        if (doc.RootNode["spotify"].HasValue)
        {
            if (doc.RootNode["spotify"]["sync"].TryGetBool(out bool sync))
            {
                v.SyncSpotifyPlaylists = sync;
            }

            if (doc.RootNode["spotify"]["ignore-match-warnings"].TryGetBool(out bool ignoreMatchWarnings))
            {
                v.IgnoreSpotifyMatchWarnings = ignoreMatchWarnings;
            }

            if (doc.RootNode["spotify"]["cookies"].TryGetString(out string? cookies))
            {
                if (!File.Exists(cookies))
                {
                    Log.Error($"File \"{cookies}\" doesn't exists");
                    success = false;
                }

                v.SpotifyCookiesPath = cookies;
            }

            if (doc.RootNode["spotify"]["debug"].TryGetString(out string? debug))
            {
                v.SpotifyDebugPath = debug;
            }

            if (doc.RootNode["spotify"]["credentials"].TryGetString(out string? credentials))
            {
                if (!File.Exists(credentials))
                {
                    Log.Warning($"File \"{credentials}\" doesn't exists");
                    success = false;
                }
                else
                {
                    v.SpotifyCredentials = JsonSerializer.Deserialize<Spotify.SpotifyCredentials>(File.ReadAllText(credentials)) ?? throw new JsonException();
                }
            }
            else if (doc.RootNode["spotify"]["credentials"].HasValue)
            {
                if (doc.RootNode["spotify"]["credentials"]["user-agent"].TryGetString(out string? userAgent))
                {
                    v.SpotifyCredentials = new()
                    {
                        UserAgent = userAgent,
                    };
                }
            }
        }

        if (doc.RootNode["youtube"].HasValue)
        {
            if (doc.RootNode["youtube"]["credentials"].TryGetString(out string? credentials))
            {
                v.YouTubeCredentialsPath = credentials;
            }
        }

        if (doc.RootNode["audacious"].HasValue)
        {
            if (doc.RootNode["audacious"]["regenerate-playlists"].TryGetBool(out bool regeneratePlaylists))
            {
                v.RegenerateAudicousPlaylists = regeneratePlaylists;
            }
        }

        if (doc.RootNode["export"].HasValue)
        {
            if (doc.RootNode["export"]["m3u"].TryGetBool(out bool m3u))
            {
                v.ExportM3U = m3u;
            }

            if (doc.RootNode["export"]["pls"].TryGetBool(out bool pls))
            {
                v.ExportPLS = pls;
            }
        }

        if (doc.RootNode["ytdlp"].HasValue)
        {
            if (doc.RootNode["ytdlp"]["args"].TryGetArray(out ReadOnlyCollection<TomlValue>? args))
            {
                foreach (TomlValue item in args)
                {
                    if (!item.TryGetString(out string w))
                    {
                        Log.Error($"Invalid ytdlp args item {item}");
                        success = false;
                        continue;
                    }

                    v.YtDlpAdditionalArguments = v.YtDlpAdditionalArguments.Add(w);
                }
            }
        }

        return success;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9-_]+$")]
    static partial Regex playlistIdRegex { get; }

    [GeneratedRegex(@"youtube\.com.*list=([a-zA-Z0-9-_]+)")]
    static partial Regex playlistRegex { get; }
}
