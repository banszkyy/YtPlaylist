using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_InitialStep
{
    [J("step")] public string? Step { get; set; }
    [J("codeInfo")] public NextData_CodeInfo? CodeInfo { get; set; }
}
