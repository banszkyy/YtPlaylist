using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class DeviceAuthorizationResponse
{
    [J("device_code")] public required string DeviceCode { get; init; }
    [J("user_code")] public required string UserCode { get; init; }
    [J("verification_uri")] public required string VerificationUri { get; init; }
    [J("verification_uri_complete")] public required string VerificationUriComplete { get; init; }
    [J("expires_in")] public required long ExpiresIn { get; init; }
    [J("interval")] public required long Interval { get; init; }
}
