using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_ClientInfo
{
    [J("id")] public string? Id { get; init; }
    [J("name")] public string? Name { get; init; }
    [J("description")] public string? Description { get; init; }
    [J("requiredisTrusted")] public bool? IsTrusted { get; init; }
    [J("privacyPolicyUrl")] public string? PrivacyPolicyUrl { get; init; }
    [J("smallImageUrl")] public string? SmallImageUrl { get; init; }
    [J("largeImageUrl")] public string? LargeImageUrl { get; init; }
    [J("creationPoint")] public string? CreationPoint { get; init; }
    [J("referral")] public string? Referral { get; init; }

    public override string ToString() => $"{Name} <{Id}>";
}
