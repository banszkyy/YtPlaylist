using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_RequestedScopes
{
    [J("grouped")] public List<NextData_Grouped>? Grouped { get; set; }
    [J("ungrouped")] public List<string>? Ungrouped { get; set; }
}
