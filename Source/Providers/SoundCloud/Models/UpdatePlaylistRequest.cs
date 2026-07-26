using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class UpdatePlaylistRequest
{
    [JsonPropertyName("playlist")] public required UpdatePlaylistContent Playlist { get; set; }
}
