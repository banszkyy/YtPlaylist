using System.Text.Json.Serialization;

namespace YtPlaylist.Spotify;

sealed class SpotifyCredentials
{
    [JsonPropertyName("clientToken")] public string? ClientToken { get; init; }
    [JsonPropertyName("userAgent")] public string? UserAgent { get; init; }
}
