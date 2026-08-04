using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_RuntimeConfig
{
    [J("staticPrefix")] public string? StaticPrefix { get; set; }
    [J("basePath")] public string? BasePath { get; set; }
    [J("development")] public bool? Development { get; set; }
    [J("eventSenderClientId")] public string? EventSenderClientId { get; set; }
    [J("urlHosts")] public UrlHosts? UrlHosts { get; set; }
}
