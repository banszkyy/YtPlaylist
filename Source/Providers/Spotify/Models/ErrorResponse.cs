using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class ErrorResponse
{
    [J("error")] public required Error Error { get; set; }
}
