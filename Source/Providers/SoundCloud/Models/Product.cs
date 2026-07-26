using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Product
{
    [JsonPropertyName("id")] public string? Id { get; init; }
}
