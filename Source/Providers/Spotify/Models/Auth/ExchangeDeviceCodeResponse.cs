using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class ExchangeDeviceCodeResponse
{
    [J("access_token")] public required string AccessToken { get; set; }
    [J("token_type")] public required string TokenType { get; set; }
    [J("expires_in")] public required long ExpiresIn { get; set; }
    [J("refresh_token")] public required string RefreshToken { get; set; }
    [J("scope")] public required string Scope { get; set; }
}
