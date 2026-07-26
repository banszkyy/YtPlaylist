using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Hqub.MusicBrainz;
using Hqub.MusicBrainz.Entities;
using Logger;

namespace YtPlaylist;

static class MusicBrainz
{
    static ImmutableArray<T> GetBests<T>(IEnumerable<T>? queryResult, Func<T, int> score)
    {
        if (queryResult is null) return [];

        List<T> best = [];
        int bestScore = 0;

        foreach (T item in queryResult)
        {
            int v = score(item);
            if (best.Count == 0)
            {
                best.Add(item);
                bestScore = v;
            }
            else if (v > bestScore)
            {
                best.Clear();
                best.Add(item);
                bestScore = v;
            }
            else if (v == bestScore)
            {
                best.Add(item);
            }
        }

        return [.. best];
    }

    static readonly ImmutableDictionary<int, string?> InvalidQueryCharacters = Confusables.CompileMap(new Dictionary<string, string?>()
    {
        { "[]<>{}", null }
    });

    [return: NotNullIfNotNull(nameof(v))]
    static string? FixMetaString(string? v)
    {
        if (v is null) return null;
        int i = v.IndexOf('(');
        if (i is not -1 and not 0)
        {
            v = v[..i].TrimEnd();
        }
        v = Confusables.Replace(v, InvalidQueryCharacters);
        return v;
    }

    public static async Task FetchMetadata(MusicFile musicFile, MusicBrainzClient musicBrainz, List<string>? issues, CancellationToken cancellationToken)
    {
        string? title = FixMetaString(musicFile.Meta.Title);
        ImmutableArray<string> artists = [.. musicFile.Meta.Performers.Select(FixMetaString)!];

        Recording? recording = null;

        if (artists.Length > 1 && !string.IsNullOrEmpty(musicFile.Meta.RemixedBy))
        {
            artists = [.. artists.Where(v => !string.Equals(v, musicFile.Meta.RemixedBy, StringComparison.OrdinalIgnoreCase))];
        }

        StringBuilder queryBuilder = new();

        if (artists.Length > 1)
        {
            queryBuilder.Append($"({string.Join(" OR ", artists.Select(v => $"artistname:{v.Quote()}"))})");
        }
        else if (artists.Length == 1)
        {
            queryBuilder.Append($"artistname:{artists[0].Quote()}");
        }

        if (queryBuilder.Length > 0) queryBuilder.Append(" AND ");
        queryBuilder.Append($"recording:{title.Quote()}");

        if (!string.IsNullOrEmpty(musicFile.Meta.RemixedBy))
        {
            if (queryBuilder.Length > 0) queryBuilder.Append(" AND ");
            queryBuilder.Append($"creditname:{musicFile.Meta.RemixedBy.Quote()}");
        }

        QueryResult<Recording> res;
        try
        {
            res = await musicBrainz.Recordings.SearchAsync(queryBuilder.ToString());
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested) Log.Error(ex);
            return;
        }

        if (res.IsNullOrEmpty())
        {
            issues?.Add($"No recordings found (check https://musicbrainz.org/search?query={Uri.EscapeDataString(queryBuilder.ToString())}&type=recording&limit={25}&method=advanced )");
            return;
        }

        Debug.Assert(res.Items[0].Score > 0);

        ImmutableArray<Recording> bests = GetBests(res, v => v.Score);

        if (bests.Length > 1)
        {
            issues?.Add($"Multiple recordings found (check https://musicbrainz.org/search?query={Uri.EscapeDataString(queryBuilder.ToString())}&type=recording&limit={25}&method=advanced )");
            return;
        }

        if (bests[0].Score != 100)
        {
            issues?.Add($"Similar recording found (check https://musicbrainz.org/search?query={Uri.EscapeDataString(queryBuilder.ToString())}&type=recording&limit={25}&method=advanced )");
            return;
        }

        recording = res.Items[0];

        if (!string.Equals(musicFile.Meta.Title, recording.Title, StringComparison.InvariantCultureIgnoreCase))
        {
            issues?.Add($"Recording title doesn't match with \"{musicFile.Meta.Title}\" (check https://musicbrainz.org/search?query={Uri.EscapeDataString(queryBuilder.ToString())}&type=recording&limit={25}&method=advanced )");
        }

        musicFile.Meta.Performers = recording.Credits.IsNullOrEmpty() ? musicFile.Meta.Performers : [.. recording.Credits.Select(v => Confusables.Replace(v.Name, Confusables.Equivalents))];
        musicFile.Meta.Title = string.IsNullOrEmpty(recording.Title) ? musicFile.Meta.Title : Confusables.Replace(recording.Title, Confusables.Equivalents);
        musicFile.Meta.Genres = recording.Genres.IsNullOrEmpty() ? musicFile.Meta.Genres : [.. recording.Genres.Select(v => v.Name) ?? []];

        musicFile.OpenTags();

        TagLib.File tagsFile = musicFile.TagsFile;
        Diff tagsDiff = musicFile.TagsDiff;

        tagsFile.Tag.Title = tagsDiff.Modify("Title", tagsFile.Tag.Title, musicFile.Meta.Title);
        tagsFile.Tag.Performers = tagsDiff.Modify("Performers", tagsFile.Tag.Performers, [.. musicFile.Meta.Performers]);
        tagsFile.Tag.Genres = tagsDiff.Modify("Genres", tagsFile.Tag.Genres, [.. musicFile.Meta.Genres]);

        List<Release> appearedInReleases = recording.Releases ?? [];

        if (appearedInReleases.Any(v => v.Status == "Official"))
        {
            appearedInReleases = [.. appearedInReleases.Where(v => v.Status == "Official")];
        }

        //file.Tag.MusicBrainzReleaseStatus = tagDiff.Modify("MusicBrainzReleaseStatus", file.Tag.MusicBrainzReleaseStatus, null);
        //file.Tag.MusicBrainzReleaseCountry = tagDiff.Modify("MusicBrainzReleaseCountry", file.Tag.MusicBrainzReleaseCountry, null);
        //file.Tag.MusicBrainzReleaseId = tagDiff.Modify("MusicBrainzReleaseId", file.Tag.MusicBrainzReleaseId, null);
        //file.Tag.MusicBrainzReleaseGroupId = tagDiff.Modify("MusicBrainzReleaseGroupId", file.Tag.MusicBrainzReleaseGroupId, null);
        //file.Tag.MusicBrainzTrackId = tagDiff.Modify("MusicBrainzTrackId", file.Tag.MusicBrainzTrackId, null);
        //file.Tag.MusicBrainzReleaseGroupId = tagDiff.Modify("MusicBrainzReleaseGroupId", file.Tag.MusicBrainzReleaseGroupId, null);

        if (appearedInReleases.Count > 1)
        {
            issues?.Add($"Recording {recording.Id} appeared in multiple releases (check https://musicbrainz.org/recording/{recording.Id} )");
        }
        else if (appearedInReleases.Count == 0)
        {
            issues?.Add($"Recording {recording.Id} didn't appear in any releases (check https://musicbrainz.org/recording/{recording.Id} )");
        }
        else
        {
            Release release = appearedInReleases[0];
            release = await musicBrainz.Releases.GetAsync(release.Id, "genres", "tags", "release-groups", "recordings");

            if (tagsFile.Tag.Pictures.Length == 0 || tagsFile.Tag.Pictures[0].Description != "MusicBrainz")
            {
                Uri url = new($"https://coverartarchive.org/release/{release.Id}/front-250.jpg", UriKind.Absolute);
                bool ok = await TagUtils.DownloadCoverImage(tagsFile, url, "MusicBrainz", TagLib.PictureType.FrontCover, tagsDiff, cancellationToken);
                if (!ok)
                {
                    issues?.Add($"Couldn't download cover art (check {url} )");
                }
            }

            if ((release.Genres ?? []).Count > 0) musicFile.Meta.Genres = [.. (release.Genres ?? []).Select(v => Confusables.Replace(v.Name, Confusables.Equivalents))];

            tagsFile.Tag.MusicBrainzReleaseStatus = tagsDiff.Modify("MusicBrainzReleaseStatus", tagsFile.Tag.MusicBrainzReleaseStatus, release.Status);
            tagsFile.Tag.MusicBrainzReleaseCountry = tagsDiff.Modify("MusicBrainzReleaseCountry", tagsFile.Tag.MusicBrainzReleaseCountry, release.Country);
            tagsFile.Tag.MusicBrainzReleaseId = tagsDiff.Modify("MusicBrainzReleaseId", tagsFile.Tag.MusicBrainzReleaseId, release.Id);
            tagsFile.Tag.Genres = tagsDiff.Modify("Genres", tagsFile.Tag.Genres, [.. musicFile.Meta.Genres]);

            if (release.Date is not null)
            {
                string[] v = release.Date.Split('-');
                if (v.Length >= 1 && uint.TryParse(v[0], out uint year))
                {
                    musicFile.Meta.Year = year;

                    tagsFile.Tag.Year = tagsDiff.Modify("Year", tagsFile.Tag.Year, year);
                }
                else
                {
                    issues?.Add($"Invalid release date format \"{release.Date}\" (check https://musicbrainz.org/release/{release.Id} )");
                }
            }

            ReleaseGroup? releaseGroup = release.ReleaseGroup;
            if (releaseGroup is not null)
            {
                tagsFile.Tag.MusicBrainzReleaseGroupId = tagsDiff.Modify("MusicBrainzReleaseGroupId", tagsFile.Tag.MusicBrainzReleaseGroupId, releaseGroup.Id);

                if (musicFile.Meta.Genres.IsDefaultOrEmpty && (releaseGroup.Genres ?? []).Count > 0) musicFile.Meta.Genres = [.. (releaseGroup.Genres ?? []).Select(v => v.Name)];

                tagsFile.Tag.Genres = tagsDiff.Modify("Genres", tagsFile.Tag.Genres, [.. musicFile.Meta.Genres]);

                if (releaseGroup.PrimaryType == "Album")
                {
                    musicFile.Meta.Album = Confusables.Replace(releaseGroup.Title, Confusables.Equivalents);
                    musicFile.Meta.AlbumArtists = [.. (releaseGroup.Credits ?? []).Select(v => Confusables.Replace(v.Name, Confusables.Equivalents))];

                    tagsFile.Tag.Album = tagsDiff.Modify("Album", tagsFile.Tag.Album, musicFile.Meta.Album);
                    tagsFile.Tag.AlbumArtists = tagsDiff.Modify("AlbumArtists", tagsFile.Tag.AlbumArtists, [.. musicFile.Meta.AlbumArtists]);
                }
            }

            if (release.Media is null || release.Media.Count == 0)
            {

            }
            else if (release.Media.Count == 1)
            {
                Medium media = release.Media[0];

                if (media.Tracks is not null)
                {
                    foreach (Track track in media.Tracks)
                    {
                        if (track.Recording.Id != recording.Id) continue;
                        tagsFile.Tag.Track = tagsDiff.Modify("Track", tagsFile.Tag.Track, (uint)track.Position);
                        tagsFile.Tag.TrackCount = tagsDiff.Modify("TrackCount", tagsFile.Tag.TrackCount, (uint)media.TrackCount);
                        tagsFile.Tag.MusicBrainzTrackId = tagsDiff.Modify("MusicBrainzTrackId", tagsFile.Tag.MusicBrainzTrackId, track.Id);
                        goto ok;
                    }

                    issues?.Add($"Track of recording {recording.Id} not found in the media (check https://musicbrainz.org/release/{release.Id} )");
                ok:;
                }
            }
            else
            {
                issues?.Add($"Release has multiple medias (check https://musicbrainz.org/release/{release.Id} )");
            }
        }
    }
}
