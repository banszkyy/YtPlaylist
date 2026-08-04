using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_Grouped
{
    [J("groupName")] public string? GroupName { get; set; }
    [J("scopes")] public List<string>? Scopes { get; set; }
}
