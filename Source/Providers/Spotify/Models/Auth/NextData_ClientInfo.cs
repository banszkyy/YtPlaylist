using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_ClientInfo
{
    [J("id")] public string? Id { get; set; }
    [J("name")] public string? Name { get; set; }
    [J("description")] public string? Description { get; set; }
    [J(" requiredisTrusted")] public bool? IsTrusted { get; set; }
    [J("privacyPolicyUrl")] public string? PrivacyPolicyUrl { get; set; }
    [J("smallImageUrl")] public string? SmallImageUrl { get; set; }
    [J("largeImageUrl")] public string? LargeImageUrl { get; set; }
    [J("creationPoint")] public string? CreationPoint { get; set; }
    [J("referral")] public string? Referral { get; set; }
}
