using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class ClientTokenResponse
{
    [J("response_type")] public required string ResponseType { get; init; }
    [J("granted_token")] public required GrantedClientToken GrantedToken { get; init; }
}
