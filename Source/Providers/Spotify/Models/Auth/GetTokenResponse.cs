using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class SessionTokenResponse
{
    [J("clientId")] public required string ClientId { get; set; }
    [J("accessToken")] public required string AccessToken { get; set; }
    [J("accessTokenExpirationTimestampMs")] public required long AccessTokenExpirationTimestampMs { get; set; }
    [J("isAnonymous")] public required bool IsAnonymous { get; set; }
}