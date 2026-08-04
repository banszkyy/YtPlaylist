using Logger;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;

namespace YtPlaylist;

static class YouTube
{
    public static async Task FetchMetadata(MusicFile musicFile, YoutubeClient youtube, YouTubeCache? youtubeCache, CancellationToken cancellationToken = default)
    {
        if (musicFile.PlaylistVideo is null) return;

        musicFile.OpenTags();

        TagLib.File file = musicFile.TagsFile;
        Diff diff = musicFile.TagsDiff;

        MusicMeta meta = MetaGuesser.Guess(musicFile.PlaylistVideo);

        string channelName = musicFile.PlaylistVideo.Author.ChannelTitle;
        const string TopicSuffix = " - Topic";
        if (channelName.EndsWith(TopicSuffix, StringComparison.InvariantCulture))
        {
            channelName = channelName[..^TopicSuffix.Length].TrimEnd();
        }

        if (musicFile.Meta.Title == Path.GetFileNameWithoutExtension(musicFile.Path))
        {
            musicFile.Meta.Title = meta.Title;
            file.Tag.Title = diff.Modify("Title", file.Tag.Title, meta.GetTitleText());
        }

        if (musicFile.Meta.Performers.IsDefaultOrEmpty)
        {
            musicFile.Meta.Performers = meta.Performers;
            file.Tag.Performers = diff.Modify("Performers", file.Tag.Performers, [.. musicFile.Meta.Performers]);
        }

        if (string.IsNullOrEmpty(musicFile.Meta.RemixedBy))
        {
            musicFile.Meta.RemixedBy = meta.RemixedBy;
            file.Tag.RemixedBy = diff.Modify("RemixedBy", file.Tag.RemixedBy, musicFile.Meta.RemixedBy);
        }

        if (string.IsNullOrEmpty(musicFile.Meta.Featuring))
        {
            musicFile.Meta.Featuring = meta.Featuring;
        }

        Video? video2 = null;
        try
        {
            string url = $"https://www.youtube.com/watch?v={musicFile.Id}";
            if (youtubeCache is null || !youtubeCache.LoadVideo(musicFile.Id, out video2))
            {
                video2 = await youtube.Videos.GetAsync(url, cancellationToken);
                youtubeCache?.SaveVideo(video2);
            }
            musicFile.Video = video2;
        }
        catch (VideoUnavailableException ex)
        {
            Log.Error(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        if (video2 is not null)
        {
            meta = MetaGuesser.Guess(video2);

            musicFile.Meta.Copyright = meta.Copyright;
            file.Tag.Copyright = diff.Modify("Copyright", file.Tag.Copyright, meta.Copyright);

            if (string.IsNullOrEmpty(file.Tag.MusicBrainzReleaseId))
            {
                musicFile.Meta.Title = meta.Title;
                file.Tag.Title = diff.Modify("Title", file.Tag.Title, meta.GetTitleText());

                if (!meta.Performers.Any(v => musicFile.Meta.Performers.Contains(v, StringComparer.InvariantCultureIgnoreCase))
                    || meta.Performers.Length > musicFile.Meta.Performers.Length)
                {
                    musicFile.Meta.Performers = meta.Performers;
                    file.Tag.Performers = diff.Modify("Performers", file.Tag.Performers, [.. meta.Performers]);
                }

                if (meta.Year.HasValue)
                {
                    musicFile.Meta.Year = meta.Year.Value;
                    file.Tag.Year = diff.Modify("Year", file.Tag.Year, meta.Year.Value);
                }
            }

            if (string.IsNullOrEmpty(file.Tag.MusicBrainzReleaseGroupId))
            {
                musicFile.Meta.Album = meta.Album;
                file.Tag.Album = diff.Modify("Album", file.Tag.Album, meta.Album);
            }

            //MatchCollection linkMatches = LinkRegex.Matches(video2.Description);
            //List<Uri> uris = [];
            //foreach (Match item in linkMatches)
            //{
            //    if (!Uri.TryCreate(item.Value, UriKind.Absolute, out Uri? uri)) continue;
            //    uris.Add(uri);
            //}

            //Uri[] soundCloudUris = [.. uris.Where(v => v.Host == "soundcloud.com")];
            //if (soundCloudUris.Length == 1)
            //{
            //    Uri soundCloudUri = soundCloudUris[0];

            //    if (soundCloudUri.Segments.Length >= 2)
            //    {
            //        string artistSoundCloudPerma = soundCloudUri.Segments[1].TrimEnd('/');
            //    }

            //    if (soundCloudUri.Segments.Length >= 3)
            //    {
            //        Debugger.Break();
            //    }
            //}
        }

        if (file.Tag.Pictures.Length == 0)
        {
            await TagUtils.DownloadCoverImage(file, new Uri(musicFile.PlaylistVideo.Thumbnails.OrderByDescending(v => v.Resolution.Area).First().Url, UriKind.Absolute), "YouTube", TagLib.PictureType.FrontCover, diff, cancellationToken);
        }
    }
}
