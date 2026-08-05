using System.Collections.Immutable;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class GrantedClientToken
{
    [J("token")] public required string Token { get; init; }
    [J("expires_after_seconds")] public required long ExpiresAfterSeconds { get; init; }
    [J("refresh_after_seconds")] public required long RefreshAfterSeconds { get; init; }
    [J("domains")] public required IReadOnlyList<ClientTokenDomain> Domains { get; init; }
}
