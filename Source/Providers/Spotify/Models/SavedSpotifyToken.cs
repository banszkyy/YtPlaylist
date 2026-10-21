using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class SavedClientToken
{
    [J("sessionToken")] public required string SessionToken { get; set; }
    [J("clientId")] public required string ClientId { get; set; }
    [J("sessionTokenExpiresAt")] public required DateTimeOffset SessionTokenExpiresAt { get; set; }
    [J("clientToken")] public required string ClientToken { get; set; }
    [J("clientTokenExpiresAt")] public required DateTimeOffset ClientTokenExpiresAt { get; set; }
    [J("clientTokenRefreshAt")] public required DateTimeOffset ClientTokenRefreshAt { get; set; }
}
