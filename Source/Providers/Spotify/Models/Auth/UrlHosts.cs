using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class UrlHosts
{
    [J("login")] public string? Login { get; set; }
    [J("signup")] public string? Signup { get; set; }
}