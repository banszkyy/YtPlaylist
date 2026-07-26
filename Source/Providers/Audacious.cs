using FFMPEG.Probe;
using Logger;
using YtPlaylist;

static class Audacious
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
        using (ProgressBar progress = new() { MaxWidth = 20 })
        {
            foreach (string item in Directory.GetFiles(playlistsDirecotry, "*.audpl").WithProgress(progress))
            {
                using FileStream file = File.OpenRead(item);
                using StreamReader reader = new(file);
                AudaciousPlaylist audaciousPlaylist = new()
                {
                    Path = item,
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

        Log.MajorAction($"Generating playlists");
        foreach (Playlist? playlist in library.Playlists.OrderBy(v => v.Title).ToArray())
        {
            AudaciousPlaylist? audaciousPlaylist = audaciousPlaylists.FirstOrDefault(v => v.Title == playlist.Title);
            if (audaciousPlaylist is null)
            {
                audaciousPlaylist = new()
                {
                    Path = Path.Combine(playlistsDirecotry, $"{Enumerable.Range(0, 1000).First(v => !audaciousPlaylists.Any(w => int.Parse(Path.GetFileNameWithoutExtension(w.Path)) == v))}.audpl"),
                    Title = playlist.Title,
                };
                audaciousPlaylists.Add(audaciousPlaylist);
            }

            for (int i = 0; i < audaciousPlaylist.Items.Count; i++)
            {
                if (!playlist.Musics.Any(v => v.Path == audaciousPlaylist.Items[i].Uri.LocalPath))
                {
                    audaciousPlaylist.Items.RemoveAt(i--);
                }
            }

            using (ProgressBar progress = new() { MaxWidth = 20 })
            {
                foreach (MusicFile item in playlist.Musics.WithProgress(progress))
                {
                    AudaciousPlaylistItem? audaciousPlaylistItem = audaciousPlaylist.Items.FirstOrDefault(v => v.Uri.LocalPath == item.Path);

                    if (audaciousPlaylistItem is null)
                    {
                        Root? ffprobeRes = await FFProbe.Probe(item.Path, cancellationToken);
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

                        audaciousPlaylistItem = new AudaciousPlaylistItem()
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
                        };
                    }

                    TagLib.File tag = item.TagsFile ??= TagLib.File.Create(item.Path, TagLib.ReadStyle.PictureLazy);

                    audaciousPlaylistItem.Artist = string.Join(" & ", tag.Tag.Performers);
                    audaciousPlaylistItem.Title = tag.Tag.Title;
                    audaciousPlaylistItem.Album = tag.Tag.Album;
                    audaciousPlaylistItem.Year = (int)tag.Tag.Year;
                    audaciousPlaylistItem.TrackNumber = (int)tag.Tag.Track;
                    audaciousPlaylistItem.Genre = tag.Tag.Genres;

                    audaciousPlaylist.Items.Add(audaciousPlaylistItem);
                }
            }
        }

        Log.MajorAction($"Writing playlists");
        using (ProgressBar progress = new() { MaxWidth = 20 })
        {
            for (int i = 0; i < audaciousPlaylists.Count; i++)
            {
                progress.Report(i, audaciousPlaylists.Count);
                Log.MinorAction($"Writing {1000 + i}.audpl");
                AudaciousPlaylist audaciousPlaylist = audaciousPlaylists[i];
                if (!arguments.DryRun) audaciousPlaylist.SaveTo(Path.Combine(playlistsDirecotry, $"{1000 + i}.audpl"));
            }
        }

        Log.MajorAction($"Regenerating playlist order");
        {
            string orderFilename = Path.Combine(playlistsDirecotry, "order");
            List<int> order = File.Exists(orderFilename) ? [.. File.ReadAllText(orderFilename).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse)] : [];

            for (int i = 0; i < order.Count; i++)
            {
                int j = order[i] - 1000;
                if (j < 0 || j >= audaciousPlaylists.Count)
                {
                    Log.MinorAction($"Removing {order[i]}");
                    order.RemoveAt(i--);
                }
            }

            for (int i = 0; i < audaciousPlaylists.Count; i++)
            {
                if (!order.Contains(i + 1000))
                {
                    Log.MinorAction($"Adding {i + 1000}");
                    order.Add(i + 1000);
                }
            }

            Log.MinorAction($"Writing order");
            if (!arguments.DryRun) File.WriteAllText(orderFilename, string.Join(' ', order));
        }
    }
}