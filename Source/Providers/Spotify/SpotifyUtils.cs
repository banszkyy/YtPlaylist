using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Logger;
using YoutubeExplode.Videos;
using YtPlaylist.Spotify;

namespace YtPlaylist;

static class SpotifyUtils
{
    public static async Task<MatchedSearchResultItem?> MatchTrack(MusicFile musicFile, Library library, SpotifyClient client, AppArguments arguments, CancellationToken cancellationToken = default)
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

        if (musicFile.Video is not null)
        {
            Regex soundcloudLinkRegex = new(@"https:\/\/(www.)?spotify.com\/");
            foreach (string permalink in soundcloudLinkRegex.Matches(musicFile.Video.Description).Select(v => v.Groups[1].Value))
            {
                Debugger.Break();
            }
        }

        foreach (Video? video in library.Musics.Where(v => v.Video is not null && musicFile.Video is not null && v.Video.Author.ChannelId.Value == musicFile.Video.Author.ChannelId.Value).Select(v => v.Video))
        {
            if (video is null) continue;

            Regex soundcloudLinkRegex = new(@"https:\/\/(www.)?spotify.com\/");
            foreach (string permalink in soundcloudLinkRegex.Matches(video.Description).Select(v => v.Groups[2].Value))
            {
                Debugger.Break();
            }
        }

        StringBuilder query = new();
        query.Append($"{string.Join(' ', searchingMeta.Performers)} ");
        if (!string.IsNullOrWhiteSpace(searchingMeta.Title)) query.Append(searchingMeta.Title);
        if (!string.IsNullOrWhiteSpace(searchingMeta.RemixedBy)) query.Append($" {searchingMeta.RemixedBy} Remix");

        ImmutableArray<MatchedSearchResultItem> queryRes = await client.SearchTracks(
            query.ToString(),
            offset: 0,
            limit: 10,
            cancellationToken: cancellationToken);

        if (queryRes.Length == 0)
        {
            if (!arguments.IgnoreSoundCloudMatchWarnings) Log.Warning($"Absolute no track found for query \"{query}\" (check https://open.spotify.com/search/{HttpUtility.UrlPathEncode(query.ToString())}/tracks )");
            return null;
        }

        MatchedSearchResultItem? bestMatch = null;
        int bestMatchScore = 0;
        ImmutableArray<string> bestMatchIssues = [];
        foreach (MatchedSearchResultItem item in queryRes)
        {
            if (item.Item.Data.Uri == null)
            {
                Log.Warning($"Track \"{item.Item.Data.Name}\" has no uri, skipping");
                continue;
            }

            if (!item.Item.Data.Uri.StartsWith("spotify:track:"))
            {
                Log.Warning($"Search result item \"{item.Item.Data.Name}\" is not a track, skipping");
                continue;
            }

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

            if (item.Item.Data.Artists is not null)
            {
                ImmutableArray<string> scArtists = [.. item.Item.Data.Artists.Items.Select(v => Confusables.Replace(v.Profile?.Name, Confusables.Equivalents))!];

                artistMatch = MatchArtists(scArtists);

                if (!artistMatch)
                {
                    artistMatchIssues.Add($"Artist \"{string.Join(" & ", scArtists)}\" doesn't match with \"{string.Join(" & ", searchingMeta.Performers)}\"");
                }
            }
            else
            {
                Debugger.Break();
                artistMatch = false;
                artistMatchIssues.Add($"No reliable artist found for track");
            }

            if (item.Item.Data.Name is not null)
            {
                titleMatch = metaStringEqualityComparer.Equals(Confusables.Replace(item.Item.Data.Name, Confusables.Equivalents), searchingMeta.Title);

                if (!titleMatch)
                {
                    artistMatchIssues.Add($"Title \"{item.Item.Data.Name}\" doesn't match with \"{searchingMeta.Title}\"");
                }
            }
            else
            {
                Debugger.Break();
                titleMatch = false;
                artistMatchIssues.Add($"No reliable title found for track");
            }

            remixMatch = true;
            if (musicFile.Meta.RemixedBy != null) Debugger.Break();

            int score = 10 + (artistMatch ? 1 : 0) + (titleMatch ? 1 : 0) + (remixMatch ? 1 : 0);

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
            Log.Warning($"No track found for query \"{query}\" (check https://open.spotify.com/search/{HttpUtility.UrlPathEncode(query.ToString())}/tracks )");

            if (bestMatchIssues.Length == 0) throw new UnreachableException();
            if (bestMatch is null) throw new UnreachableException();

            Log.WarningNoprefix($"Match issues for {bestMatch.Item.Data.Name}:");
            foreach (string issue in bestMatchIssues)
            {
                Log.WarningNoprefix($"  {issue}");
            }
        }

        return null;
    }
}
