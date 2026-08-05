using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class Error
{
    [J("status")] public long? Status { get; init; }
    [J("code")] public long? Code { get; init; }
    [J("message")] public required string Message { get; init; }
}
