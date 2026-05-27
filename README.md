# YouTube Playlist Synchronizer

[![.Net 11.0](https://img.shields.io/badge/11.0-606060?style=flat-square&logo=dotnet&labelColor=512BD4)](#)

Downloads and synchronizes YouTube playlists as MP3 files so you can listen to them offline!!!

## Build Dependencies

- [BBpezsgo/MusicBrainz](https://github.com/BBpezsgo/MusicBrainz)
- [BBpezsgo/Logger](https://github.com/BBpezsgo/Logger)

## Runtime Dependencies

- `ffmpeg`
- `yt-dlp`
- `ffplay` (optional, only for previewing music)

## Arguments

- `-p|--playlist <id>` - The YouTube playlist id to download (you can specify more)
- `-o|--output <path>` - Output directory to sync the playlists to
- `--nodownload` - Don't download any new YouTube music videos
- `--nometadata` - Don't fetch song metadata
- `--nolyrics` - Don't fetch song lyrics
- `--httpcache <path>` - Directory to use as an HTTP cache for the API requests (MusicBrainz, LrcLib, YouTube)
- `--ignoremetawarnings` - Ignores all metadata warnings produced when it cannot parse the filename, or it cannot find the MusicBrainz record
- `--ytcredentials <path>` - Specifies where to save the credentials when interacting with the YouTube API
- `--usecache` - For testing only
- `--dry` - For testing only

> [!TIP]
> You can get the playlist id by navigating to the playlist on YouTube. For example:
>
> <https://music.youtube.com/playlist?list=PLCXNT9D5QsgZZrogN4KV__ImVNQWTmRjs>
>
> And copy the part after the "list=" parameter, in this example its "PLCXNT9D5QsgZZrogN4KV__ImVNQWTmRjs".

## Features

### Synchronizing

- Downloads all missing music files that are in your playlist
- Deletes all music files that you deleted from the playlist
- TODO: Move music files that you moved between your playlists

> [!NOTE]
> It will create subdirectories for each YouTube playlist.

> [!TIP]
> You can rename the MP3 files (together with the .lrc files), because the youtube video's id is embedded into the MP3 metadata.

> [!TIP]
> Until it doesn't find the MusicBrainz entry for the music, it tries to guess the artist and title from the MP3 file name.
> Because of this, you might have to fix filenames. The best filename for a music is `Artist - Title.mp3`.

### Metadata

- First, it fetches the YouTube video's metadata and sets the MP3 file's title, artist and album cover metadata.
- Second, it searches for the music *release* on [MusicBrainz](https://musicbrainz.org/), and sets all possible MP3 metadata tags possible.

### Lyrics

- It searches for the lyrics on [LrcLib](https://lrclib.net/), and embeds it into the MP3 metadata, and also into a .lrc file.

### Duplicate detection

- It warns you if you have the same music in multiple different playlists. If you specify `--ytcredentials`, you can also remove the duplicated music from the command line.

## Would you use this?

This program fulfills my needs, so no additional crazy features are planned, but if you want some you would actually use, tell me!
