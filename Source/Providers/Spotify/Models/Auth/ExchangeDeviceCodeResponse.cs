using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class ExchangeDeviceCodeResponse
{
    [J("access_token")] public required string AccessToken { get; init; }
    [J("token_type")] public required string TokenType { get; init; }
    [J("expires_in")] public required long ExpiresIn { get; init; }
    [J("refresh_token")] public required string RefreshToken { get; init; }
    [J("scope")] public required string Scope { get; init; }
}
