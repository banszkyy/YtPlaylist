using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class ConsentManagementJwt
{
    [JsonPropertyName("userId")] public string? UserId { get; init; }
    [JsonPropertyName("jwt")] public string? Jwt { get; init; }
}
