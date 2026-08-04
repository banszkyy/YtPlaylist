using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class DeviceAuthorizationResponse
{
    [J("device_code")] public required string DeviceCode { get; set; }
    [J("user_code")] public required string UserCode { get; set; }
    [J("verification_uri")] public required string VerificationUri { get; set; }
    [J("verification_uri_complete")] public required string VerificationUriComplete { get; set; }
    [J("expires_in")] public required long ExpiresIn { get; set; }
    [J("interval")] public required long Interval { get; set; }
}
