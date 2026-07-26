using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class MarketingIds
{
    [JsonPropertyName("gtm")] public string? Gtm { get; init; }
}
