# YouTube Playlist Synchronizer

[![.Net 11.0](https://img.shields.io/badge/11.0-606060?style=flat-square&logo=dotnet&labelColor=512BD4)](#)

Downloads and synchronizes YouTube playlists as MP3 files so you can listen to them offline!!! (it also syncs it to SoundCloud :p)

## Features

### Synchronizing

- Downloads all missing music files that are in your playlist
- Deletes all music files that you deleted from the playlist
- TODO: Move music files that you moved between your playlists

> [!NOTE]
> It will create subdirectories for each YouTube playlist.

> [!TIP]
> You can freely rename the MP3 files (together with the .lrc files), because the YouTube video's id is embedded into the MP3 metadata and the metadata guessing is done on the YouTube video.

### Metadata

- Fetches the YouTube video's metadata and sets the MP3 file's title, artist and album cover metadata.
- Parses the description of Topic channel videos for more metadata
- Searches for the music on [MusicBrainz](https://musicbrainz.org/), and sets all possible MP3 metadata tags possible.

### Lyrics

- Searches for the lyrics on [LrcLib](https://lrclib.net/), and embeds it into the MP3 metadata, and also into a .lrc file.

### Duplicate detection

- Warns you if you have the same music in multiple different playlists.
- Warns you if you have very extremely suspiciously similar music files.

If you specify `--youtube-credentials`, you can also remove the duplicated music from the command line.

### SoundCloud sync

- **WIP** Synchronizes the YouTube playlists to SoundCloud

## Example Usage

```bash
ytsync -p https://music.youtube.com/playlist?list=PLCXNT9D5QsgZZrogN4KV__ImVNQWTmRjs -o ./Music
```

This will download the playlist into the `./Music/PLAYLIST_NAME/` directory, where `PLAYLIST_NAME` is the name of the specified playlist on YouTube

> [!TIP]
> You can also just pass the playlist id instead of the full url

## Arguments

- `-p|--playlist <id>` - The YouTube playlist id to download (you can specify more)
- `-o|--output <path>` - Output directory to sync the playlists to
- `--no-download` - Don't download any YouTube music videos
- `--no-metadata` - Don't fetch any song metadata
- `--no-lyrics` - Don't fetch song lyrics
- `--http-cache <path>` - Directory to use as an HTTP cache for the API requests (MusicBrainz, LrcLib, YouTube)
- `--ignore-meta-warnings` - Ignores all metadata warnings produced when it cannot parse the filename, or it cannot find the MusicBrainz record
- `--youtube-credentials <path>` - File to save the credentials when interacting with the YouTube API
- `--soundcloud-credentials <path>` - Credentials to use when interacting with the SoundCloud API
- `--cookies <path>` - [Netscape cookies file](https://everything.curl.dev/http/cookies/fileformat.html) to extract credentials from ([Get cookies.txt LOCALLY](https://github.com/kairi003/Get-cookies.txt-LOCALLY#from-webstore))
- `--check-redundancy` - Checks for similar meta tags across all music files, like artist names with different capitalization or a few letter differences
- `--check-duplicates` - Checks for duplicate music based on the meta tags. Music with the same YouTube id will always be checked for
- `--regenerate-audicous-playlists` - Regenerates your Audicous playlists (stored in `$HOME/.config/audacious/playlists`)
- `--sync-soundcloud-playlists` - **WIP** Synchronizes your SoundCloud playlists with your YouTube playlists (YouTube -> SoundCloud)
- `--sound-cloud-ignore <id/name>` - Don't sync this playlist to SoundCloud. You can specify by playlist id or name (you can specify more)
- `--ignore-soundcloud-match-warnings` - Ignores warnings related to SoundCloud track matching
- `--ytdlp <arg>` - Additional ytdlp argument (you can specify more)
- `--save-intermediate-tags` - Saves ID3v2 tags after they are being changed. Will skew up the final diff info, but the tags will be saved if you terminate the app
- `--use-cache` - For testing only
- `--reset-meta` - For testing only
- `--dry` - For testing only

> [!INFO]
> For the arguments where you can specify more, you have to write the argument name too:
> Instead of `--playlist meowmeow1 wiwiwi` you should write `--playlist meowmeow1 --playlist wiwiwi`

## Runtime Dependencies

- `ffmpeg`
- `yt-dlp`

## Build Dependencies

- [banszkyy/MusicBrainz](https://github.com/banszkyy/MusicBrainz)
- [banszkyy/Logger](https://github.com/banszkyy/Logger)
- banszkyy/HttpCache (not yet published)

## Would you use this?

This program fulfills my needs, so no additional crazy features are planned, but if for you it need some improvements, tell me!
