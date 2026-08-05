using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_Grouped
{
    [J("groupName")] public string? GroupName { get; init; }
    [J("scopes")] public IReadOnlyList<string>? Scopes { get; init; }
}
