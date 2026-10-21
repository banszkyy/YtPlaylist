using System.Text.Json.Serialization;

namespace YtPlaylist.Spotify;

sealed class SpotifyCredentials
{
    [JsonPropertyName("userAgent")] public string? UserAgent { get; init; }
}
