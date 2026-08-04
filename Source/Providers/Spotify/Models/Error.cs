using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class Error
{
    [J("status")] public long? Status { get; set; }
    [J("code")] public long? Code { get; set; }
    [J("message")] public required string Message { get; set; }
}
