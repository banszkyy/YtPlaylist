using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class AppVersion
{
    [JsonPropertyName("app")] public required string App { get; init; }
    [JsonPropertyName("serviceWorker")] public required string ServiceWorker { get; init; }
    [JsonPropertyName("serviceWorkerUnregistrationPatterns")] public required IReadOnlyList<object> ServiceWorkerUnregistrationPatterns { get; init; }
}
