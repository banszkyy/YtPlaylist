using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_AuthorizationInfo
{
    [J("clientInfo")] public NextData_ClientInfo? ClientInfo { get; set; }
    [J("requestedScopes")] public NextData_RequestedScopes? RequestedScopes { get; set; }
}
