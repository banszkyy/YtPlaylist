using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class ClientTokenDomain
{
    [J("domain")] public required string Domain { get; init; }

    public override string ToString() => Domain;
}
