using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Logger;
using YoutubeExplode.Videos;
using YtPlaylist.SoundCloud;

namespace YtPlaylist;

static class SoundCloudUtils
{
    public static async Task<Track?> MatchTrack(MusicFile musicFile, Library library, SoundCloudClient soundCloudClient, AppArguments arguments, CancellationToken cancellationToken = default)
    {
        MusicMeta searchingMeta = musicFile.Meta;
        if (searchingMeta.Performers.Length == 0)
        {
            musicFile.TagsFile ??= TagLib.File.Create(musicFile.Path, TagLib.ReadStyle.PictureLazy);
            searchingMeta = MetaGuesser.Guess(musicFile.TagsFile.Tag);
        }

        if (searchingMeta.Performers.Length == 0)
        {
            Log.Warning($"Music file {musicFile.Meta} has no performers, skipping");
            return null;
        }

        if (string.IsNullOrWhiteSpace(searchingMeta.Title))
        {
            Log.Warning($"Music file {musicFile.Meta} has no title, skipping");
            return null;
        }

        ImmutableDictionary<int, string?> confusablesAccentsMap = Confusables.CombineMaps(await Confusables.Fetch(arguments), Confusables.Accents);
        ImmutableDictionary<int, string?> equivalentsAccentsMap = Confusables.CombineMaps(Confusables.Equivalents, Confusables.Accents);
        MetaStringEqualityComparer metaStringEqualityComparer = new(confusablesAccentsMap, StringComparison.InvariantCultureIgnoreCase);

        List<string> artistPermalinks = [];

        if (musicFile.Video is not null)
        {
            Regex soundcloudLinkRegex = new(@"https:\/\/(www.)?soundcloud.com\/([a-z0-9-]*)");
            foreach (string permalink in soundcloudLinkRegex.Matches(musicFile.Video.Description).Select(v => v.Groups[1].Value))
            {
                if (artistPermalinks.Any(v => v == permalink)) continue;

                artistPermalinks.Add(permalink);
            }
        }

        if (artistPermalinks.Count == 0)
        {
            foreach (Video? video in library.Musics.Where(v => v.Video is not null && musicFile.Video is not null && v.Video.Author.ChannelId.Value == musicFile.Video.Author.ChannelId.Value).Select(v => v.Video))
            {
                if (video is null) continue;

                Regex soundcloudLinkRegex = new(@"https:\/\/(www.)?soundcloud.com\/([a-z0-9-]*)");
                foreach (string permalink in soundcloudLinkRegex.Matches(video.Description).Select(v => v.Groups[2].Value))
                {
                    if (artistPermalinks.Any(v => v == permalink)) continue;

                    artistPermalinks.Add(permalink);
                }
            }
        }

        List<User> verifiedUsers = [];

        foreach (string permalink in artistPermalinks)
        {
            if (verifiedUsers.Any(v => v.Permalink == permalink)) continue;

            User? user = await soundCloudClient.GetUserFromPermalink(permalink, cancellationToken);
            if (user is null) continue;

            if (verifiedUsers.Any(v => v.Permalink == user.Permalink)) continue;

            IReadOnlyList<WebProfile> webProfiles = await soundCloudClient.GetUserWebProfiles(user.Id, cancellationToken);
            bool verifiedLink = false;
            foreach (WebProfile item in webProfiles)
            {
                if (item.Network != "youtube") continue;
                if (item.Username is null) continue;
                if (!item.Username.Equals(musicFile.PlaylistVideo?.Author.ChannelTitle, StringComparison.InvariantCultureIgnoreCase)) continue;

                verifiedLink = true;
                break;
            }

            if (!verifiedLink) continue;

            verifiedUsers.Add(user);
        }

        StringBuilder query = new();
        query.Append($"{string.Join(' ', searchingMeta.Performers)} ");
        if (!string.IsNullOrWhiteSpace(searchingMeta.Title)) query.Append(searchingMeta.Title);
        if (!string.IsNullOrWhiteSpace(searchingMeta.RemixedBy)) query.Append($" {searchingMeta.RemixedBy} Remix");

        TrackSearchResponse queryRes = await soundCloudClient.SearchTracks(
            new SearchRequestTrackFilter()
            {
                Query = query.ToString(),
                Limit = 10,
                Duration = (musicFile.PlaylistVideo?.Duration.HasValue ?? false) ? musicFile.PlaylistVideo.Duration.Value.TotalMinutes switch
                {
                    < 2 => DurationFilter.Short,
                    < 10 => DurationFilter.Medium,
                    < 30 => DurationFilter.Long,
                    _ => DurationFilter.Epic,
                } : DurationFilter.Any,
            },
            cancellationToken: cancellationToken);

        if (queryRes.Collection.Count == 0)
        {
            if (!arguments.IgnoreSoundCloudMatchWarnings) Log.Warning($"Absolute no track found for query \"{query}\" (check https://soundcloud.com/search/sounds?q={HttpUtility.UrlEncode(query.ToString())} )");
            return null;
        }

        Track? bestMatch = null;
        int bestMatchScore = 0;
        ImmutableArray<string> bestMatchIssues = [];
        foreach (Track item in queryRes.Collection)
        {
            if (item.Kind != "track")
            {
                Log.Warning($"Search result item \"{item.Title}\" is not a track, skipping");
                continue;
            }
            if (item.Id == default)
            {
                Log.Warning($"Track \"{item.Title}\" has no id, skipping");
                continue;
            }

            bool onlyVerifiedArtists = false;

            bool artistMatch = false;
            bool titleMatch = false;
            bool remixMatch = false;
            List<string> artistMatchIssues = [];
            List<string> titleMatchIssues = [];
            List<string> remixMatchIssues = [];

            bool MatchArtists(ImmutableArray<string> scArtists)
            {
                ImmutableArray<string> a = scArtists;
                ImmutableArray<string> b = searchingMeta.Performers;

                foreach (string suffix in new string[]
                {
                    "music",
                    "official",
                })
                {
                    a = [.. a.Select(v => v.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? v[..^suffix.Length].TrimEnd() : v)];
                    b = [.. b.Select(v => v.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? v[..^suffix.Length].TrimEnd() : v)];
                }

                int overlap = 0;
                foreach (string p in b)
                {
                    if (a.Contains(p, metaStringEqualityComparer))
                    {
                        overlap++;
                    }
                }

                if (overlap == 0)
                {
                    foreach (string? p in b.SelectMany(v => MetaGuesser.Split(v, " & ")))
                    {
                        if (a.Contains(p, metaStringEqualityComparer))
                        {
                            overlap++;
                        }
                    }
                }

                return overlap > 0; // FIXME
            }

            if (item.PublisherMetadata?.Artist is not null)
            {
                ImmutableArray<string> scArtists = MetaGuesser.ParseArtists(Confusables.Replace(item.PublisherMetadata.Artist, Confusables.Equivalents));

                artistMatch = MatchArtists(scArtists);

                if (!artistMatch)
                {
                    artistMatchIssues.Add($"Artist \"{string.Join(" & ", scArtists)}\" doesn't match with \"{string.Join(" & ", searchingMeta.Performers)}\"");
                }
            }
            else if (item.User is not null
                    && (searchingMeta.Performers.Contains(Confusables.Replace(item.User.Username, Confusables.Equivalents), metaStringEqualityComparer)
                    || searchingMeta.Performers.Contains(item.User.Permalink, metaStringEqualityComparer)))
            {
                if (onlyVerifiedArtists && (item.User.Verified ?? false))
                {
                    artistMatch = false;
                    artistMatchIssues.Add($"Artist {item.User.Username} isn't verified");
                }
                else
                {
                    artistMatch = true;
                }
            }
            else
            {
                artistMatch = false;
                artistMatchIssues.Add($"No reliable artist found for track");
            }

            if (item.PublisherMetadata?.ReleaseTitle is not null)
            {
                titleMatch = metaStringEqualityComparer.Equals(Confusables.Replace(item.PublisherMetadata.ReleaseTitle, Confusables.Equivalents), searchingMeta.Title);

                if (!titleMatch)
                {
                    titleMatchIssues.Add($"Title \"{item.PublisherMetadata.ReleaseTitle}\" doesn't match with \"{searchingMeta.Title}\"");
                }
            }
            else if (item.Title is not null)
            {
                titleMatch = metaStringEqualityComparer.Equals(Confusables.Replace(item.Title, Confusables.Equivalents), searchingMeta.Title);

                if (!titleMatch)
                {
                    titleMatchIssues.Add($"Title \"{item.Title}\" doesn't match with \"{searchingMeta.Title}\"");
                }
            }
            else
            {
                titleMatch = false;
                titleMatchIssues.Add($"No reliable title found for track");
            }

            MusicMeta meta;
            if (item.Title is not null)
            {
                meta = MetaGuesser.Guess(Confusables.Replace(item.Title, Confusables.Equivalents));

                if (!artistMatch)
                {
                    if (meta.Performers.IsDefaultOrEmpty)
                    {
                        artistMatchIssues.Add($"Couldn't extract artists from the title \"{item.Title}\"");
                    }
                    else
                    {
                        artistMatch = MatchArtists(meta.Performers);
                        if (!artistMatch)
                        {
                            artistMatchIssues.Add($"Artist \"{string.Join(" & ", meta.Performers)}\" doesn't match with \"{string.Join(" & ", searchingMeta.Performers)}\"");
                        }
                    }
                }

                if (!titleMatch)
                {
                    titleMatch = metaStringEqualityComparer.Equals(meta.Title, searchingMeta.Title);
                    if (!titleMatch)
                    {
                        titleMatchIssues.Add($"Title \"{meta.Title}\" doesn't match with \"{searchingMeta.Title}\"");
                    }
                }

                if (!searchingMeta.IsRemix)
                {
                    if (!remixMatch)
                    {
                        remixMatch = !meta.IsRemix;
                        if (!remixMatch)
                        {
                            titleMatchIssues.Add($"Remixer detected but searching for none");
                        }
                    }
                }
                else
                {
                    remixMatch = meta.IsRemix && meta.RemixedBy is not null && metaStringEqualityComparer.Equals(meta.RemixedBy, searchingMeta.RemixedBy);
                    if (!remixMatch)
                    {
                        if (meta.RemixedBy is null)
                        {
                            remixMatchIssues.Add($"Remixer not detected but searching for \"{searchingMeta.RemixedBy}\"");
                        }
                        else
                        {
                            remixMatchIssues.Add($"Remixer \"{meta.RemixedBy}\" doesn't match with \"{searchingMeta.RemixedBy}\"");
                        }
                    }
                }
            }

            int score = 10 + (artistMatch ? 1 : 0) + (titleMatch ? 1 : 0) + (remixMatch ? 1 : 0);

            if (!artistMatch || !titleMatch || !remixMatch)
            {
                static List<string> GetKeywords(string v)
                {
                    return [.. v
                        .SplitAll([' ', ',', '.', ':', '-', '~', '&', '|', '(', ')', '[', ']', '/', '·', '+', '"', '!'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Length > 1 && v.StartsWith('\'') && v.EndsWith('\'') ? v[1..^1] : v)
                    ];
                }

                List<string> trackKeywords = GetKeywords(Confusables.Replace(item.Title ?? string.Empty, equivalentsAccentsMap).ToLowerInvariant());
                List<string> queryKeywords = GetKeywords(Confusables.Replace($"{string.Join(' ', searchingMeta.Performers)} {searchingMeta.Title} {searchingMeta.RemixedBy}", equivalentsAccentsMap).ToLowerInvariant());
                ImmutableArray<string> commonDummies = [
                    "ft", "feat", "prod", "remix", "free", "x", "official", "audio", "video", "download", "feat_", "prod_", "by", "music", "w", "mp3", "original", "_", "'", "hq", "album", "sound", "release", "song", "visualiser", "hd", "released", "unreleased", "background", "copyright", "animation"
                ];
                ImmutableArray<string> genreDummies = [
                    "hardtechno", "industrial", "dubstep", "nightcore", "phonk", "techno", "metal", "trap", "hard", "cyberpunk", "dub", "rock", "ost", "soundtrack"
                ];
                int trackDummies = 0;
                int queryDummies = 0;

                foreach (string dummy in (IEnumerable<string>)[.. commonDummies, .. genreDummies])
                {
                    trackDummies += trackKeywords.RemoveAll(v => metaStringEqualityComparer.Equals(v, dummy));
                }

                foreach (string dummy in (IEnumerable<string>)[.. commonDummies, .. genreDummies])
                {
                    queryDummies += queryKeywords.RemoveAll(v => metaStringEqualityComparer.Equals(v, dummy));
                }

                for (int i = 0; i < queryKeywords.Count; i++)
                {
                    string keyword = queryKeywords[i];
                    int removed = trackKeywords.RemoveAll(v => v.Equals(keyword, StringComparison.InvariantCultureIgnoreCase));
                    if (removed > 0) queryKeywords.RemoveAt(i--);
                }

                if (queryKeywords.Count == 0 && trackKeywords.Count == 0)
                {
                    artistMatch = true;
                    titleMatch = true;
                    remixMatch = (item.Title ?? string.Empty).Contains("remix", StringComparison.InvariantCultureIgnoreCase) == !string.IsNullOrEmpty(searchingMeta.RemixedBy);

                    score = (artistMatch ? 1 : 0) + (titleMatch ? 1 : 0) + (remixMatch ? 1 : 0);
                }
            }

            if (artistMatch) artistMatchIssues.Clear();
            else if (artistMatchIssues.Count == 0) artistMatchIssues.Add($"Artists not matched");

            if (titleMatch) titleMatchIssues.Clear();
            else if (titleMatchIssues.Count == 0) titleMatchIssues.Add($"Title not matched");

            if (remixMatch) remixMatchIssues.Clear();
            else if (remixMatchIssues.Count == 0) remixMatchIssues.Add($"Remixer not matched");

            if (score > bestMatchScore || bestMatch is null)
            {
                bestMatch = item;
                bestMatchScore = score;
                bestMatchIssues = [.. artistMatchIssues, .. titleMatchIssues, .. remixMatchIssues];
            }

            if (!artistMatch)
            {
                continue;
            }

            if (!titleMatch)
            {
                continue;
            }

            if (!remixMatch)
            {
                continue;
            }

            return item;
        }

        if (!arguments.IgnoreSoundCloudMatchWarnings)
        {
            Log.Warning($"No track found for query \"{query}\" (check https://soundcloud.com/search/sounds?q={HttpUtility.UrlEncode(query.ToString())} )");

            if (bestMatchIssues.Length == 0) throw new UnreachableException();
            if (bestMatch is null) throw new UnreachableException();

            Log.WarningNoprefix($"Match issues for {bestMatch.Title}:");
            foreach (string issue in bestMatchIssues)
            {
                Log.WarningNoprefix($"  {issue}");
            }
        }

        return null;
    }
}
