using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_RequestedScopes
{
    [J("grouped")] public IReadOnlyList<NextData_Grouped>? Grouped { get; init; }
    [J("ungrouped")] public IReadOnlyList<string>? Ungrouped { get; init; }
}
