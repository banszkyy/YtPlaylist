using System.Diagnostics.CodeAnalysis;
using System.Text;
using Hqub.MusicBrainz;
using Hqub.MusicBrainz.Entities;
using Logger;

namespace YtPlaylist;

static class MusicBrainz
{
    readonly struct Warning(string message)
    {
        public readonly string Message = message;

        public override string ToString()
        {
            return Message;
        }
    }

    static async Task<Artist?> LookupArtist(MusicBrainzClient musicBrainz, string artist, List<string> issues, CancellationToken cancellationToken)
    {
        QueryResult<Artist>? onlineArtists = null;

        try
        {
            onlineArtists = await musicBrainz.Artists.SearchAsync(
                $"artist:{artist.Quote()} OR alias:{artist.Quote()}",
                2);
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested) Log.Error(ex);
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        List<Artist> candidates = GetCandidates(onlineArtists, (a, b) =>
        {
            if (a.Score > b.Score) return -1;
            if (a.Score < b.Score) return +1;

            if (a.Name == artist && b.Name != artist) return -1;
            if (a.Name != artist && b.Name == artist) return +1;

            bool fa = (a.Aliases ?? []).Any(v => string.Equals(v.Name, artist, StringComparison.OrdinalIgnoreCase));
            bool fb = (b.Aliases ?? []).Any(v => string.Equals(v.Name, artist, StringComparison.OrdinalIgnoreCase));
            if (fa && !fb) return -1;
            if (!fa && fb) return +1;

            return 0;
        });

        if (candidates.Count > 1)
        {
            issues.Add($"Multiple artists found with name \"{artist}\"");
            return null;
        }

        return candidates.FirstOrDefault();
    }

    static async Task<(QueryResult<Recording>? Result, string Query)> LookupRecording(MusicBrainzClient musicBrainz, string artist, string recordingTitle, string? remixedBy, CancellationToken cancellationToken)
    {
        StringBuilder queryBuilder = new();

        queryBuilder.Append($"artistname:{artist.Quote()}");

        queryBuilder.Append($" AND recording:{recordingTitle.Quote()}");

        if (!string.IsNullOrEmpty(remixedBy))
        {
            queryBuilder.Append($" AND creditname:{remixedBy.Quote()}");
        }

        string query = queryBuilder.ToString();

        try
        {
            return (await musicBrainz.Recordings.SearchAsync(query), query);
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested) Log.Error(ex);
            return (null, query);
        }
    }

    static async Task<(QueryResult<Recording>? Result, string Query)> LookupRecording(MusicBrainzClient musicBrainz, Artist artist, string recordingTitle, CancellationToken cancellationToken)
    {
        StringBuilder queryBuilder = new();

        queryBuilder.Append($"arid:{artist.Id}");

        queryBuilder.Append($" AND recording:{recordingTitle.Quote()}");

        string query = queryBuilder.ToString();

        try
        {
            return (await musicBrainz.Recordings.SearchAsync(queryBuilder.ToString()), query);
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested) Log.Error(ex);
            return (null, query);
        }
    }

    static Recording? FilterRecordings(IReadOnlyList<Recording>? recordings, string artistName, string recordingTitle, string? remixedBy, List<string> issues, string query)
    {
        if (recordings.IsNullOrEmpty())
        {
            return null;
        }

        if (recordings[0].Score > 0)
        {
            List<Recording> bestRecordings = [];
            int bestScore = 0;

            foreach (Recording recording in recordings)
            {
                if (bestRecordings.Count == 0)
                {
                    bestRecordings.Add(recording);
                    bestScore = recording.Score;
                }
                else if (recording.Score > bestScore)
                {
                    bestRecordings.Clear();
                    bestRecordings.Add(recording);
                    bestScore = recording.Score;
                }
                else if (recording.Score == bestScore)
                {
                    bestRecordings.Add(recording);
                }
            }

            if (bestRecordings.Count > 1)
            {
                issues.Add($"Multiple recordings found: {Ansi.Bold(artistName)} - {Ansi.Bold(recordingTitle)} (check https://musicbrainz.org/search?query={Uri.EscapeDataString(query)}&type=recording )");
                return null;
            }

            if (bestScore != 100)
            {
                issues.Add($"Similar recording found: {Ansi.Bold(artistName)} - {Ansi.Bold(recordingTitle)} --> {Ansi.Bold(string.Join(" & ", bestRecordings[0].Credits.Select(v => v.Name)))} - {Ansi.Bold(bestRecordings[0].Title)}(check https://musicbrainz.org/search?query={Uri.EscapeDataString(query)}&type=recording )");
                return null;
            }

            return bestRecordings[0];
        }
        else
        {
            if (recordings.Count > 1)
            {
                issues.Add($"Multiple recordings found: {Ansi.Bold(artistName)} - {Ansi.Bold(recordingTitle)} (check https://musicbrainz.org/search?query={Uri.EscapeDataString(query)}&type=recording )");
                return null;
            }

            if (recordings.Count == 0)
            {
                return null;
            }

            return recordings[0];
        }
    }

    static List<T> GetCandidates<T>(IEnumerable<T>? items, Comparison<T> comparison)
    {
        if (items is null || !items.Any()) return [];

        T? best = default;
        foreach (T item in items)
        {
            if (best is null || comparison(best, item) > 0)
            {
                best = item;
            }
        }

        List<T> candidates = [];
        foreach (T item in items)
        {
            if (comparison(best!, item) == 0)
            {
                candidates.Add(item);
            }
        }
        return candidates;
    }

    static int CompareReleases(Release a, Release b)
    {
        if (a.Status == "Official" && b.Status != "Official") return -1;
        if (a.Status != "Official" && b.Status == "Official") return +1;

        return 0;
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

    public static async Task FetchMetadata(TagLib.File file, Diff tagDiff, MusicBrainzClient musicBrainz, AppArguments appArguments, List<string>? issues, CancellationToken cancellationToken)
    {
        string? title = FixMetaString(file.Tag.Title);
        string[]? artists = [.. (file.Tag.Performers ?? []).Select(FixMetaString)!];

        if (string.IsNullOrEmpty(title) || artists.Length == 0)
        {
            issues?.Add($"Empty file metadata (title and artists are empty)");
            return;
        }

        Recording? recording = null;

        foreach (string artist in artists)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Tag.RemixedBy is not null && file.Tag.RemixedBy.Contains(artist, StringComparison.InvariantCultureIgnoreCase)) continue;

            List<string> searchIssues = [];
            (QueryResult<Recording>? recordings, string query) = await LookupRecording(musicBrainz, artist, file.Tag.Title, file.Tag.RemixedBy, cancellationToken);
            recording = FilterRecordings(recordings?.Items, artist, file.Tag.Title, file.Tag.RemixedBy, searchIssues, query);

            if (recording is null)
            {
                searchIssues.Clear();

                Artist? artist_ = await LookupArtist(musicBrainz, artist, searchIssues, cancellationToken);

                if (artist_ is not null)
                {
                    (recordings, query) = await LookupRecording(musicBrainz, artist_, file.Tag.Title, cancellationToken);
                    recording = FilterRecordings(recordings?.Items, artist, file.Tag.Title, file.Tag.RemixedBy, searchIssues, query);
                }
            }

            if (recording is null)
            {
                issues?.AddRange(searchIssues);
                continue;
            }

            break;
        }

        if (recording is null)
        {
            //Log.Warning($"No recording found for `{string.Join(" & ", artists)} - {title}`");
            return;
        }

        //MusicBrainzUtils.Print(recording);
        //Console.ReadKey();

        file.Tag.Title = tagDiff.Modify("Title", file.Tag.Title, recording.Title);

        file.Tag.Performers = tagDiff.Modify("Performers", file.Tag.Performers, [.. (recording.Credits ?? []).Select(v => v.Name)]);

        List<Release> appearedInReleases = recording.Releases ?? [];

        if (appearedInReleases.Any(v => v.Status == "Offical"))
        {
            appearedInReleases = [.. appearedInReleases.Where(v => v.Status == "Offical")];
        }
        file.Tag.MusicBrainzReleaseStatus = tagDiff.Modify("MusicBrainzReleaseStatus", file.Tag.MusicBrainzReleaseStatus, null);
        file.Tag.MusicBrainzReleaseCountry = tagDiff.Modify("MusicBrainzReleaseCountry", file.Tag.MusicBrainzReleaseCountry, null);
        file.Tag.MusicBrainzReleaseId = tagDiff.Modify("MusicBrainzReleaseId", file.Tag.MusicBrainzReleaseId, null);
        file.Tag.MusicBrainzReleaseGroupId = tagDiff.Modify("MusicBrainzReleaseGroupId", file.Tag.MusicBrainzReleaseGroupId, null);

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

            if (file.Tag.Pictures.Length == 0 || file.Tag.Pictures[0].Description != "MusicBrainz")
            {
                await TagUtils.DownloadCoverImage(file, CoverArtArchive.GetCoverArtUri(release.Id), "MusicBrainz", TagLib.PictureType.FrontCover, tagDiff, cancellationToken);
            }

            file.Tag.MusicBrainzReleaseStatus = tagDiff.Modify("MusicBrainzReleaseStatus", file.Tag.MusicBrainzReleaseStatus, release.Status);
            file.Tag.MusicBrainzReleaseCountry = tagDiff.Modify("MusicBrainzReleaseCountry", file.Tag.MusicBrainzReleaseCountry, release.Country);
            file.Tag.MusicBrainzReleaseId = tagDiff.Modify("MusicBrainzReleaseId", file.Tag.MusicBrainzReleaseId, release.Id);

            file.Tag.Year = tagDiff.Modify("Year", file.Tag.Year, default);

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

            file.Tag.Genres = tagDiff.Modify("Genres", file.Tag.Genres, release.Genres is null ? [] : [.. release.Genres.Select(v => v.Name)]);

            file.Tag.Album = tagDiff.Modify("Album", file.Tag.Album, null);
            file.Tag.AlbumArtists = tagDiff.Modify("AlbumArtists", file.Tag.AlbumArtists, []);
            file.Tag.MusicBrainzReleaseGroupId = tagDiff.Modify("MusicBrainzReleaseGroupId", file.Tag.MusicBrainzReleaseGroupId, null);

            if (release.ReleaseGroup is not null)
            {
                if (release.ReleaseGroup.PrimaryType == "Album")
                {
                    ReleaseGroup releaseGroup = release.ReleaseGroup;

                    file.Tag.MusicBrainzReleaseGroupId = releaseGroup.Id;

                    if (file.Tag.Album != releaseGroup.Title)
                    {
                        file.Tag.Album = tagDiff.Modify("Album", file.Tag.Album, releaseGroup.Title);
                        file.Tag.MusicBrainzReleaseGroupId = tagDiff.Modify("MusicBrainzReleaseGroupId", file.Tag.MusicBrainzReleaseGroupId, releaseGroup.Id);
                    }

                    if (releaseGroup.Credits is not null)
                    {
                        string[] albumArtists = [.. releaseGroup.Credits.Select(v => v.Artist.SortName)];
                        if (!(file.Tag.AlbumArtists ?? []).SequenceEqual(albumArtists))
                        {
                            file.Tag.AlbumArtists = tagDiff.Modify("AlbumArtists", file.Tag.AlbumArtists, albumArtists);
                        }
                    }
                }
                else
                {
                    issues?.Add($"Unknown release group type \"{release.ReleaseGroup.PrimaryType}\" (check https://musicbrainz.org/release/{release.Id} )");
                }
            }

            file.Tag.TrackCount = tagDiff.Modify("TrackCount", file.Tag.TrackCount, default);
            file.Tag.Track = tagDiff.Modify("Track", file.Tag.Track, default);
            file.Tag.MusicBrainzTrackId = tagDiff.Modify("MusicBrainzTrackId", file.Tag.MusicBrainzTrackId, default);

            if (release.Media is null || release.Media.Count == 0)
            {

            }
            else if (release.Media.Count == 1)
            {
                Medium media = release.Media[0];

                if (media.Tracks is not null)
                {
                    file.Tag.TrackCount = tagDiff.Modify("TrackCount", file.Tag.TrackCount, (uint)media.TrackCount);

                    foreach (Track track in media.Tracks)
                    {
                        if (track.Recording.Id == recording.Id)
                        {
                            file.Tag.Track = tagDiff.Modify("Track", file.Tag.Track, (uint)track.Position);
                            file.Tag.MusicBrainzTrackId = tagDiff.Modify("MusicBrainzTrackId", file.Tag.MusicBrainzTrackId, track.Id);
                            goto ok;
                        }
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
