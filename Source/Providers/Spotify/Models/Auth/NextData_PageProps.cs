using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_PageProps
{
    [J("initialStep")] public NextData_InitialStep? InitialStep { get; init; }
    [J("method")] public string? Method { get; init; }
}
