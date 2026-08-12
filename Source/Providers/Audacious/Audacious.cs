using Logger;
using YtPlaylist.FFMPEG.Probe;

namespace YtPlaylist.Audacious;

static class AudaciousUtils
{
    public static async Task RegeneratePlaylists(Library library, AppArguments arguments, CancellationToken cancellationToken = default)
    {
        string? home = Environment.GetEnvironmentVariable("HOME");

        if (home is null)
        {
            Log.Error($"HOME variable not set");
            return;
        }

        string playlistsDirecotry = Path.Combine(home, ".config", "audacious", "playlists");

        if (!Directory.Exists(playlistsDirecotry))
        {
            Log.Error($"Audacious playlists directory doesn't exists");
            return;
        }

        List<AudaciousPlaylist> audaciousPlaylists = [];

        Log.MajorAction($"Reading playlists");
        using (ProgressBar progress = new() { MaxWidth = 70 })
        {
            foreach (string item in Directory.GetFiles(playlistsDirecotry, "*.audpl").WithProgress(progress))
            {
                using FileStream file = File.OpenRead(item);
                using StreamReader reader = new(file);
                AudaciousPlaylist audaciousPlaylist = new()
                {
                    Path = item,
                    Index = int.Parse(Path.GetFileNameWithoutExtension(item)),
                };
                audaciousPlaylist.ReadFrom(reader);
                audaciousPlaylists.Add(audaciousPlaylist);
            }
        }

        Log.MajorAction($"Deleting nonexistent playlists");
        for (int i = 0; i < audaciousPlaylists.Count; i++)
        {
            if (!library.Playlists.Any(v => v.Title == audaciousPlaylists[i].Title))
            {
                Log.MinorAction($"Deleting {Path.GetFileName(audaciousPlaylists[i].Path)}");
                File.Delete(audaciousPlaylists[i].Path);
                audaciousPlaylists.RemoveAt(i--);
            }
        }

        List<Change<string>> changes = [];

        Log.MajorAction($"Generating playlists");
        foreach (Playlist playlist in library.Playlists.OrderBy(v => v.Title).ToArray())
        {
            Log.MinorAction($"Generating playlist {playlist.Title}");

            AudaciousPlaylist? audaciousPlaylist = audaciousPlaylists.FirstOrDefault(v => v.Title == playlist.Title);
            if (audaciousPlaylist is null)
            {
                int uniqueIndex = Enumerable.Range(0, 1000).First(v => !audaciousPlaylists.Any(w => int.Parse(Path.GetFileNameWithoutExtension(w.Path)) == v));
                audaciousPlaylist = new()
                {
                    Path = Path.Combine(playlistsDirecotry, $"{uniqueIndex}.audpl"),
                    Title = playlist.Title,
                    Index = uniqueIndex,
                };
                audaciousPlaylists.Add(audaciousPlaylist);
            }

            for (int i = 0; i < audaciousPlaylist.Items.Count; i++)
            {
                if (!playlist.Musics.Any(v => v.Path == audaciousPlaylist.Items[i].Uri.LocalPath)
                    || audaciousPlaylist.Items.Take(i).Any(v => v.Uri.LocalPath == audaciousPlaylist.Items[i].Uri.LocalPath))
                {
                    changes.Add(new($"[{playlist.Title}] {audaciousPlaylist.Items[i].Artist} - {audaciousPlaylist.Items[i].Title}", ChangeType.Delete));
                    audaciousPlaylist.Items.RemoveAt(i--);
                }
            }

            using (ProgressBar progress = new() { MaxWidth = 70 })
            {
                foreach (MusicFile item in playlist.Musics.WithProgress(progress, v => v.ToString()))
                {
                    AudaciousPlaylistItem? audaciousPlaylistItem = audaciousPlaylist.Items.FirstOrDefault(v => v.Uri.LocalPath == item.Path);

                    if (audaciousPlaylistItem is null)
                    {
                        FFProbeResult? ffprobeRes = await FFProbe.Probe(item.Path, cancellationToken);
                        if (ffprobeRes is null) continue;

                        FFMPEG.Probe.Stream? stream = ffprobeRes.Streams?.FirstOrDefault(v => v.CodecType == "audio");

                        if (stream is null)
                        {
                            Log.Error($"No audio stream found");
                            continue;
                        }

                        if (stream.BitRate is null)
                        {
                            Log.Error($"BitRate is missing");
                            continue;
                        }

                        if (!int.TryParse(stream.BitRate, out int bitRate))
                        {
                            Log.Error($"Invalid BitRate");
                            continue;
                        }

                        if (!stream.Channels.HasValue)
                        {
                            Log.Error($"Channels is missing");
                            continue;
                        }

                        if (stream.CodecName is null)
                        {
                            Log.Error($"CodecName is missing");
                            continue;
                        }

                        string? codec = stream.CodecName switch
                        {
                            "mp3" => "MPEG-1 layer 3",
                            _ => null,
                        };

                        if (codec is null)
                        {
                            Log.Error($"Invalid CodecName");
                            continue;
                        }

                        if (stream.Duration is null)
                        {
                            Log.Error($"Duration is missing");
                            continue;
                        }

                        if (!double.TryParse(stream.Duration, out double duration))
                        {
                            Log.Error($"Invalid Duration");
                            continue;
                        }

                        if (stream.ChannelLayout is null)
                        {
                            Log.Error($"ChannelLayout is missing");
                            continue;
                        }

                        string? channelLayout = stream.ChannelLayout switch
                        {
                            "stereo" => "Stereo",
                            "mono" => "Mono",
                            _ => null,
                        };

                        if (channelLayout is null)
                        {
                            Log.Error($"Invalid ChannelLayout");
                            continue;
                        }

                        if (stream.SampleRate is null)
                        {
                            Log.Error($"SampleRate is missing");
                            continue;
                        }

                        if (!int.TryParse(stream.SampleRate, out int sampleRate))
                        {
                            Log.Error($"Invalid SampleRate");
                            continue;
                        }

                        audaciousPlaylist.Items.Add(audaciousPlaylistItem = new AudaciousPlaylistItem()
                        {
                            Uri = new Uri(item.Path, UriKind.Absolute),
                            FileCreated = ((DateTimeOffset)File.GetCreationTime(item.Path)).ToUnixTimeSeconds(),
                            FileModified = ((DateTimeOffset)File.GetLastWriteTime(item.Path)).ToUnixTimeSeconds(),

                            Artist = string.Empty,
                            Title = string.Empty,

                            Bitrate = bitRate / 1000,
                            Channels = stream.Channels.Value,
                            Codec = codec,
                            Length = (long)(duration * 1000),
                            Quality = $"{channelLayout}, {sampleRate} Hz",
                        });
                        changes.Add(new($"[{playlist.Title}] {audaciousPlaylistItem.Artist} - {audaciousPlaylistItem.Title}", ChangeType.Create));
                    }

                    Diff diff = new();

                    audaciousPlaylistItem.Artist = diff.Modify("Artist", audaciousPlaylistItem.Artist, item.Meta.GetArtistsText());
                    audaciousPlaylistItem.Title = diff.Modify("Title", audaciousPlaylistItem.Title, item.Meta.GetTitleText());
                    audaciousPlaylistItem.Album = diff.Modify("Album", audaciousPlaylistItem.Album, item.Meta.Album);
                    audaciousPlaylistItem.Year = diff.Modify("Year", audaciousPlaylistItem.Year, (int)(item.Meta.Year ?? default));
                    audaciousPlaylistItem.Copyright = diff.Modify("Copyright", audaciousPlaylistItem.Copyright, item.Meta.Copyright);

                    TagLib.File tag = item.TagsFile ??= TagLib.File.Create(item.Path, TagLib.ReadStyle.PictureLazy);

                    audaciousPlaylistItem.TrackNumber = diff.Modify("TrackNumber", audaciousPlaylistItem.TrackNumber, (int)tag.Tag.Track);
                    audaciousPlaylistItem.Genre = diff.Modify("Genre", audaciousPlaylistItem.Genre, tag.Tag.Genres ?? []);

                    if (diff.Changes.Count > 0)
                    {
                        changes.Add(new($"[{playlist.Title}] {audaciousPlaylistItem.Artist} - {audaciousPlaylistItem.Title}", ChangeType.Modify));
                    }
                }
            }
        }

        Log.MajorAction($"Writing playlists");
        using (ProgressBar progress = new() { MaxWidth = 70 })
        {
            foreach (AudaciousPlaylist audaciousPlaylist in audaciousPlaylists.WithProgress(progress, v => $"{v.Index}.audpl"))
            {
                if (!arguments.DryRun) audaciousPlaylist.SaveTo(Path.Combine(playlistsDirecotry, $"{audaciousPlaylist.Index}.audpl"));
            }
        }

        Log.MajorAction($"Writing playlist order");
        if (!arguments.DryRun) File.WriteAllText(Path.Combine(playlistsDirecotry, "order"), string.Join(' ', audaciousPlaylists.OrderBy(v => v.Title).Select((v) => v.Index)));

        Changes.Print(changes);
    }
}