using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class SavedSpotifyToken
{
    [J("token")] public required ExchangeDeviceCodeResponse Token { get; set; }
    [J("expiresAt")] public required DateTimeOffset ExpiresAt { get; set; }
}

public class SavedClientToken
{
    [J("token")] public required GrantedClientToken Token { get; set; }
    [J("expiresAt")] public required DateTimeOffset ExpiresAt { get; set; }
    [J("expiresAt")] public required DateTimeOffset RefreshAt { get; set; }
}
