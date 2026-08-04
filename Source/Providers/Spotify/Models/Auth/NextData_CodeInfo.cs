using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_CodeInfo
{
    [J("code")] public string? Code { get; set; }
    [J("authorizationInfo")] public NextData_AuthorizationInfo? AuthorizationInfo { get; set; }
}
