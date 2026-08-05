using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_RuntimeConfig
{
    [J("staticPrefix")] public string? StaticPrefix { get; init; }
    [J("basePath")] public string? BasePath { get; init; }
    [J("development")] public bool? Development { get; init; }
    [J("eventSenderClientId")] public string? EventSenderClientId { get; init; }
    [J("urlHosts")] public UrlHosts? UrlHosts { get; init; }
}
