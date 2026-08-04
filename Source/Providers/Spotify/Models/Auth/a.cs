using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class ClientTokenResponse
{
    [J("response_type")] public required string ResponseType { get; set; }
    [J("granted_token")] public required GrantedClientToken GrantedToken { get; set; }
}

public class GrantedClientToken
{
    [J("token")] public required string Token { get; set; }
    [J("expires_after_seconds")] public required long ExpiresAfterSeconds { get; set; }
    [J("refresh_after_seconds")] public required long RefreshAfterSeconds { get; set; }
    [J("domains")] public required List<ClientTokenDomain> Domains { get; set; }
}

public class ClientTokenDomain
{
    [J("domain")] public required string Domain { get; set; }
}
