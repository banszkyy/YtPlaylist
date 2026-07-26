using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class Badges
{
    [JsonPropertyName("pro")] public bool? Pro { get; init; }
    [JsonPropertyName("creator_mid_tier")] public bool? CreatorMidTier { get; init; }
    [JsonPropertyName("pro_unlimited")] public bool? ProUnlimited { get; init; }
    [JsonPropertyName("verified")] public bool? Verified { get; init; }
}
