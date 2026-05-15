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

    static async Task<Artist?> LookupArtist(MusicBrainzClient musicBrainz, string artist, List<Warning> warnings, CancellationToken cancellationToken)
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
            if (cancellationToken.IsCancellationRequested) return null;
            Log.Error(ex);
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
            warnings.Add(new Warning($"Multiple artists found with name \"{artist}\""));
            //foreach (Artist item in candidates)
            //{
            //    MusicBrainzUtils.Print(item);
            //}

            return null;
        }

        return candidates.FirstOrDefault();
    }

    static async Task<QueryResult<Recording>?> LookupRecording(MusicBrainzClient musicBrainz, string artist, string recordingTitle, CancellationToken cancellationToken)
    {
        StringBuilder queryBuilder = new();

        queryBuilder.Append($"artistname:{artist.Quote()}");

        queryBuilder.Append($" AND recording:{recordingTitle.Quote()}");

        try
        {
            return await musicBrainz.Recordings.SearchAsync(queryBuilder.ToString());
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            Log.Error(ex);
            return null;
        }
    }

    static async Task<QueryResult<Recording>?> LookupRecording(MusicBrainzClient musicBrainz, Artist artist, string recordingTitle, CancellationToken cancellationToken)
    {
        StringBuilder queryBuilder = new();

        queryBuilder.Append($"arid:{artist.Id}");

        queryBuilder.Append($" AND recording:{recordingTitle.Quote()}");

        try
        {
            return await musicBrainz.Recordings.SearchAsync(queryBuilder.ToString());
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            Log.Error(ex);
            return null;
        }
    }

    static Recording? FilterRecordings(IReadOnlyList<Recording>? recordings, string artistName, string recordingTitle, string? remixedBy, List<Warning> warnings)
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
                warnings.Add(new Warning($"Multiple recordings found: {Ansi.Bold(artistName)} - {Ansi.Bold(recordingTitle)}"));
                //foreach ((Recording recording, bool isFirst) in bestRecordings.WithSeparators())
                //{
                //    if (!isFirst) Console.WriteLine("=================================");
                //    MusicBrainzUtils.Print(recording);
                //}
                return null;
            }

            if (bestScore != 100)
            {
                warnings.Add(new Warning($"Similar recording found: {Ansi.Bold(artistName)} - {Ansi.Bold(recordingTitle)} --> {Ansi.Bold(string.Join(" & ", bestRecordings[0].Credits.Select(v => v.Name)))} - {Ansi.Bold(bestRecordings[0].Title)}"));
                //foreach ((Recording recording, bool isFirst) in bestRecordings.WithSeparators())
                //{
                //   if (!isFirst) Console.WriteLine("=================================");
                //   MusicBrainzUtils.Print(recording);
                //}
                return null;
            }

            return bestRecordings[0];
        }
        else
        {
            if (recordings.Count > 1)
            {
                warnings.Add(new Warning($"Multiple recordings found: {Ansi.Bold(artistName)} - {Ansi.Bold(recordingTitle)}"));
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
        if (i != -1)
        {
            return v[..i].TrimEnd();
        }
        return v;
    }

    public static async Task FetchMetadata(TagLib.File file, Diff tagDiff, MusicBrainzClient musicBrainz, AppArguments appArguments, CancellationToken cancellationToken)
    {
        string? title = FixMetaString(file.Tag.Title);
        string[]? artists = [.. (file.Tag.Performers ?? []).Select(FixMetaString)!];

        if (string.IsNullOrEmpty(title) || artists.Length == 0)
        {
            Log.Warning($"Empty file metadata");
            return;
        }

        Recording? recording = null;

        foreach (string artist in artists)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Tag.RemixedBy is not null && file.Tag.RemixedBy.Contains(artist, StringComparison.InvariantCultureIgnoreCase)) continue;

            List<Warning> warnings = [];
            QueryResult<Recording>? recordings = await LookupRecording(musicBrainz, artist, file.Tag.Title, cancellationToken);
            recording = FilterRecordings(recordings?.Items, artist, file.Tag.Title, file.Tag.RemixedBy, warnings);

            if (recording is null)
            {
                warnings.Clear();

                Artist? artist_ = await LookupArtist(musicBrainz, artist, warnings, cancellationToken);

                if (artist_ is not null)
                {
                    recordings = await LookupRecording(musicBrainz, artist_, file.Tag.Title, cancellationToken);
                    recording = FilterRecordings(recordings?.Items, artist, file.Tag.Title, file.Tag.RemixedBy, warnings);
                }
            }

            if (recording is null)
            {
                if (!appArguments.IgnoreMetaWarnings)
                {
                    foreach (Warning warning in warnings)
                    {
                        Log.WarningNoprefix(warning.ToString());
                    }
                }
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

        file.Tag.Performers = tagDiff.Modify("Performers", file.Tag.Performers, recording.Credits is null ? [] : [.. recording.Credits.Select(v => v.Artist.Name)]);

        if (recording.Releases.IsNullOrEmpty())
        {
            Log.Warning($"No release found: {Ansi.Bold(string.Join(" & ", artists))} - {Ansi.Bold(title)}");

            file.Tag.MusicBrainzReleaseStatus = tagDiff.Modify("MusicBrainzReleaseStatus", file.Tag.MusicBrainzReleaseStatus, null);
            file.Tag.MusicBrainzReleaseCountry = tagDiff.Modify("MusicBrainzReleaseCountry", file.Tag.MusicBrainzReleaseCountry, null);
            file.Tag.MusicBrainzReleaseId = tagDiff.Modify("MusicBrainzReleaseId", file.Tag.MusicBrainzReleaseId, null);
        }
        else
        {
            Release release = GetCandidates(recording.Releases, CompareReleases).First();

            if (file.Tag.Pictures.Length == 0 || file.Tag.Pictures[0].Description != "MusicBrainz")
            {
                await TagUtils.DownloadCoverImage(file, CoverArtArchive.GetCoverArtUri(release.Id), "MusicBrainz", TagLib.PictureType.FrontCover, tagDiff, cancellationToken);
            }

            file.Tag.MusicBrainzReleaseStatus = tagDiff.Modify("MusicBrainzReleaseStatus", file.Tag.MusicBrainzReleaseStatus, release.Status);
            file.Tag.MusicBrainzReleaseCountry = tagDiff.Modify("MusicBrainzReleaseCountry", file.Tag.MusicBrainzReleaseCountry, release.Country);
            file.Tag.MusicBrainzReleaseId = tagDiff.Modify("MusicBrainzReleaseId", file.Tag.MusicBrainzReleaseId, release.Id);

            if (release.Date is not null)
            {
                string[] v = release.Date.Split('-');
                if (v.Length >= 1 && uint.TryParse(v[0], out uint year))
                {
                    file.Tag.Year = tagDiff.Modify("Year", file.Tag.Year, year);
                }
            }
            else
            {
                file.Tag.Year = tagDiff.Modify("Year", file.Tag.Year, default);
            }

            file.Tag.Genres = tagDiff.Modify("Genres", file.Tag.Genres, release.Genres is null ? [] : [.. release.Genres.Select(v => v.Name)]);

            if (release.ReleaseGroup is not null)
            {
                ReleaseGroup releaseGroup = release.ReleaseGroup;

                file.Tag.MusicBrainzReleaseGroupId = releaseGroup.Id;

                if (file.Tag.Album != releaseGroup.Title)
                {
                    file.Tag.Album = tagDiff.Modify("Album", file.Tag.Album, releaseGroup.Title);
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
                file.Tag.Album = tagDiff.Modify("Album", file.Tag.Album, null);
                file.Tag.AlbumArtists = tagDiff.Modify("AlbumArtists", file.Tag.AlbumArtists, []);
            }
        }
    }
}
