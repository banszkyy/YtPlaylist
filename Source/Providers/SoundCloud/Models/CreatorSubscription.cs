using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class CreatorSubscription
{
    [JsonPropertyName("product")] public Product? Product { get; init; }
}
