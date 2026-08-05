using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_AuthorizationInfo
{
    [J("clientInfo")] public NextData_ClientInfo? ClientInfo { get; init; }
    [J("requestedScopes")] public NextData_RequestedScopes? RequestedScopes { get; init; }
}
