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

    [return: NotNullIfNotNull(nameof(v))]
    static string? FixMetaString(string? v)
    {
        if (v is null) return null;
        int i = v.IndexOf('(');
        if (i is not -1 and not 0)
        {
            return v[..i].TrimEnd();
        }
        return v;
    }

    public static async Task FetchMetadata(TagLib.File file, MetaGuesser.Meta meta, Diff tagDiff, MusicBrainzClient musicBrainz, List<string>? issues, CancellationToken cancellationToken)
    {
        string title = FixMetaString(meta.Title);
        ImmutableArray<string> artists = [.. meta.Artists.Select(FixMetaString)!];

        Recording? recording = null;

        if (artists.Length > 1 && !string.IsNullOrEmpty(meta.RemixedBy))
        {
            artists = [.. artists.Where(v => !string.Equals(v, meta.RemixedBy, StringComparison.OrdinalIgnoreCase))];
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

        if (!string.IsNullOrEmpty(meta.RemixedBy))
        {
            if (queryBuilder.Length > 0) queryBuilder.Append(" AND ");
            queryBuilder.Append($"creditname:{meta.RemixedBy.Quote()}");
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

        file.Tag.Title = tagDiff.Modify("Title", file.Tag.Title, recording.Title);
        file.Tag.Performers = tagDiff.Modify("Performers", file.Tag.Performers, [.. (recording.Credits ?? []).Select(v => v.Name)]);

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
            //issues?.Add($"Recording {recording.Id} appeared in multiple releases (check https://musicbrainz.org/recording/{recording.Id} )");
        }
        else if (appearedInReleases.Count == 0)
        {
            issues?.Add($"Recording {recording.Id} didn't appear in any releases (check https://musicbrainz.org/recording/{recording.Id} )");
        }
        else
        {
            Release release = appearedInReleases[0];
            release = await musicBrainz.Releases.GetAsync(release.Id, "genres", "tags", "release-groups", "recordings");

            if (file.Tag.Pictures.Length == 0 || file.Tag.Pictures[0].Description != "MusicBrainz")
            {
                Uri url = new($"https://coverartarchive.org/release/{release.Id}/front-250.jpg", UriKind.Absolute);
                bool ok = await TagUtils.DownloadCoverImage(file, url, "MusicBrainz", TagLib.PictureType.FrontCover, tagDiff, cancellationToken);
                if (!ok)
                {
                    issues?.Add($"Couldn't download cover art (check {url} )");
                }
            }

            file.Tag.MusicBrainzReleaseStatus = tagDiff.Modify("MusicBrainzReleaseStatus", file.Tag.MusicBrainzReleaseStatus, release.Status);
            file.Tag.MusicBrainzReleaseCountry = tagDiff.Modify("MusicBrainzReleaseCountry", file.Tag.MusicBrainzReleaseCountry, release.Country);
            file.Tag.MusicBrainzReleaseId = tagDiff.Modify("MusicBrainzReleaseId", file.Tag.MusicBrainzReleaseId, release.Id);
            file.Tag.Genres = tagDiff.Modify("Genres", file.Tag.Genres, [.. (release.Genres ?? recording.Genres ?? []).Select(v => v.Name)]);

            if (release.Date is not null)
            {
                string[] v = release.Date.Split('-');
                if (v.Length >= 1 && uint.TryParse(v[0], out uint year))
                {
                    file.Tag.Year = tagDiff.Modify("Year", file.Tag.Year, year);
                }
                else
                {
                    issues?.Add($"Invalid release date format \"{release.Date}\" (check https://musicbrainz.org/release/{release.Id} )");
                }
            }

            ReleaseGroup? releaseGroup = release.ReleaseGroup;
            if (releaseGroup is not null)
            {
                file.Tag.MusicBrainzReleaseGroupId = tagDiff.Modify("MusicBrainzReleaseGroupId", file.Tag.MusicBrainzReleaseGroupId, releaseGroup.Id);

                if (releaseGroup.PrimaryType == "Album")
                {
                    file.Tag.Album = tagDiff.Modify("Album", file.Tag.Album, releaseGroup.Title);
                    file.Tag.AlbumArtists = tagDiff.Modify("AlbumArtists", file.Tag.AlbumArtists, [.. (releaseGroup.Credits ?? []).Select(v => v.Name)]);
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
                        file.Tag.Track = tagDiff.Modify("Track", file.Tag.Track, (uint)track.Position);
                        file.Tag.TrackCount = tagDiff.Modify("TrackCount", file.Tag.TrackCount, (uint)media.TrackCount);
                        file.Tag.MusicBrainzTrackId = tagDiff.Modify("MusicBrainzTrackId", file.Tag.MusicBrainzTrackId, track.Id);
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
