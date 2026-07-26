using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class RSubscription
{
    [JsonPropertyName("product")] public Product? Product { get; init; }
}
